using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using LMCore;
using UnityEngine;

[RequiredManager(typeof(PlayerManager))]
public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Inst => BaseManager<PlayerManager>.Inst;
    public static bool HasInstance => BaseManager<PlayerManager>.HasInstance;

    [Header("디버그용")]
    [SerializeField] private int _minPlayersToStart = 2;
    [SerializeField] private bool _isInGame = false;
    [SerializeField] private bool _isGameSceneLoading = false;
    [SerializeField] private bool _isGameSceneLoaded = false;
    public int MinPlayersToStart => _minPlayersToStart = (LobbyManager.Inst.IsSoloPlay ? 1 : 2);
    public bool IsInGame => _isInGame;
    public bool IsGameSceneLoading => _isGameSceneLoading;
    public bool IsGameSceneLoaded => _isGameSceneLoaded;

    // -- 서버 전용 필드
    private Dictionary<PlayerRef, AwaitableCompletionSource> _fadingTCS = new();
    private List<NetworkObject> _alivePlayers = new();
    private Dictionary<PlayerRef, ChangeDetector> _changeDetectors = new();

    // -- 네트워크 필드
    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, NetworkObject> Players => default;

    public override void Spawned()
    {
        DontDestroyOnLoad(this.gameObject);
        NetworkEventSystem.Inst.RegisterManager(this);

        // Note - 기획변경으로 더이상 사용하지않음
        // var uiController = FindAnyObjectByType<LobbyUI_Manager>();
        // this.AddPlayerDataAction(uiController.UpdateData);
        // OnChangedPlayers();

        if (Runner.IsServer)
        {
            NetworkEventSystem.Inst.OnSceneLoadDoneEvent += (runner, sceneName) => MoveToGameScene(sceneName);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _fadingTCS.Clear();
        Players.Clear();
        _alivePlayers.Clear();
        _changeDetectors.Clear();
    }

    public override async void FixedUpdateNetwork()
    {
        // 조건을 만족하면 게임 시작 - 씬변경후 게임의 상태를 PlayingState로 변경
        bool startCondition = Runner.IsServer &&
                                Players.Count >= MinPlayersToStart &&
                                _isGameSceneLoading == false &&
                                _isInGame == false;

        if (startCondition == false)
            return;

        var result = await TryStartGameAsync(isStart: AreAllPlayersReady());
        if (result)
            GameStates.Inst.DelayForceActiveState<GameStageWaitingState>();
    }

    public override void Render()
    {
        if (CheckPlayerDataChanged())
        {
            OnChangedData();
        }
    }

    #region 유틸리티
    public void AddPlayer(PlayerRef player)
    {
        if (Runner.IsServer == false)
            return;

        if (Players.ContainsKey(player))
        {
            Debug.LogError($"이미 존재하는 플레이어 입니다 {player}");
            return;
        }

        var playerObj = Runner.GetPlayerObject(player);
        if (playerObj == null)
        {
            foreach (var activePlayer in Runner.ActivePlayers)
            {
                if (Players.ContainsKey(activePlayer))
                    continue;

                playerObj = Runner.TryGetPlayerObject(activePlayer, out var obj) ? obj : null;
                if (playerObj == null)
                    continue;

                Players.Add(activePlayer, playerObj);
                if (_changeDetectors.ContainsKey(activePlayer) == false)
                {
                    _changeDetectors[activePlayer] = playerObj.GetComponent<PlayerData>().GetChangeDetector(ChangeDetector.Source.SimulationState);
                }
                return;
            }
        }

        Players.Add(player, playerObj);
        if (_changeDetectors.ContainsKey(player) == false)
        {
            var playerData = playerObj.GetComponent<PlayerData>();
            if (playerData != null)
            {
                _changeDetectors[player] = playerData.GetChangeDetector(ChangeDetector.Source.SimulationState);
            }
        }

        // 플레이어 추가시 데이터 변경 이벤트 발생
        OnChangedData();
    }

    public List<(PlayerRef, NetworkObject)> TryRemoveAllPlayer()
    {
        var list = new List<(PlayerRef, NetworkObject)>();
        foreach (var player in Players)
        {
            list.Add(TryRemovePlayer(player.Key));
        }
        return list;
    }

    public (PlayerRef, NetworkObject) TryRemovePlayer(PlayerRef player)
    {
        if (Players.Count <= 0)
        {
            Debug.LogError("플레이어를 제거하는데 실패했습니다. 등록된 플레이어가 존재하지 않습니다.");
            return (PlayerRef.None, null);
        }

        foreach (var tempPlayer in Players)
        {
            if (tempPlayer.Key == player)
            {
                Players.Remove(tempPlayer.Key);
                if (_changeDetectors.ContainsKey(tempPlayer.Key))
                {
                    _changeDetectors.Remove(tempPlayer.Key);
                }
                return (tempPlayer.Key, tempPlayer.Value);
            }
        }

        Debug.LogError("플레이어를 제거하는데 실패했습니다.");
        return (PlayerRef.None, null);
    }

    /// <summary>
    /// Note - 중간에 플레이어가 나가는 경우에 대한 예외처리를 하지않음
    /// </summary>
    public async Awaitable<bool> WaitForAllPlayerFading()
    {
        foreach (var player in _fadingTCS)
        {
            await player.Value.Awaitable;
        }
        return true;
    }

    public PlayerData GetPlayerData(PlayerRef player)
    {
        if (Players.ContainsKey(player))
        {
            return Players[player].GetComponent<PlayerData>();
        }
        return null;
    }

    public Dictionary<PlayerRef, PlayerData> GetPlayerDatas()
    {
        var dict = new Dictionary<PlayerRef, PlayerData>();
        foreach (var player in Players)
        {
            dict.Add(player.Key, player.Value.GetComponent<PlayerData>());
        }
        return dict;
    }

    public NetworkDictionary<PlayerRef, NetworkObject> GetPlayers()
    {
        foreach (var player in Players)
        {
            if (player.Value == null)
            {
                Players.Remove(player.Key);
            }
        }
        return Players;
    }

    public List<InputBlocker> GetPlayerInputBlockers()
    {
        var list = new List<InputBlocker>();
        foreach (var player in Players)
        {
            list.Add(player.Value.GetComponent<InputBlocker>());
        }
        return list;
    }

    public List<NetworkObject> GetAlivePlayers()
    {
        _alivePlayers.Clear();
        foreach (var player in Players)
        {
            var p = player.Value.GetComponent<PlayerStageController>();
            if (p.IsAlive())
                _alivePlayers.Add(player.Value);
        }
        return _alivePlayers;
    }

    public bool IsValidPlayer(PlayerRef player)
    {
        if (Players.ContainsKey(player))
            return true;
        return false;
    }
    #endregion

    #region 데이터 변경 알림
    public Action<Dictionary<PlayerRef, PlayerData>> OnPlayerDataChanged;
    public void AddPlayerDataAction(Action<Dictionary<PlayerRef, PlayerData>> action)
    {
        OnPlayerDataChanged += action;
    }
    public void RemovePlayerDataAction(Action<Dictionary<PlayerRef, PlayerData>> action)
    {
        OnPlayerDataChanged -= action;
    }
    private void OnChangedData()
    {
        OnPlayerDataChanged?.Invoke(GetPlayerDatas());
    }

    /// <summary>
    /// 데이터 변경을 감지하고 결과값을 반환하는 함수
    /// </summary>
    private bool CheckPlayerDataChanged()
    {
        // 최초 생성 시 초기 동기화를 위해 갱신 신호 반환
        bool isCreatedFrame = false;

        foreach (var kvp in Players)
        {
            NetworkObject netObj = kvp.Value;
            if (netObj == null)
                continue;

            PlayerData pData = netObj.GetComponent<PlayerData>();
            if (pData == null)
                continue;

            if (_changeDetectors.ContainsKey(kvp.Key) == false)
            {
                _changeDetectors[kvp.Key] = pData.GetChangeDetector(ChangeDetector.Source.SimulationState);
                isCreatedFrame = true; 
                continue;
            }

            var detector = _changeDetectors[kvp.Key];
            foreach (var _ in detector.DetectChanges(pData))
                return true;
        }

        if (isCreatedFrame)
            return true;

        return false;
    }
    #endregion

    #region 게임 시작
    /// <summary>
    /// 최소인원수 이상이면서 준비여부를 확인하는 함수 
    /// </summary>
    public bool AreAllPlayersReady()
    {
        int requiredPlayers = MinPlayersToStart;
        if (Players.Count < requiredPlayers)
            return false;

        foreach (var kvp in Players)
        {
            NetworkObject netObj = kvp.Value;
            if (netObj == null)
                continue;

            PlayerData pData = netObj.GetComponent<PlayerData>();
            if (pData == null)
                continue;

            if (pData.IsReady == false)
                return false;
        }

        return true;
    }


    public async Awaitable<bool> TryStartGameAsync(bool isStart = true)
    {
        // 준비 되지 않은경우
        if (isStart == false)
        {
            //Debug.Log("아직 준비되지 않은 플레이어가 있습니다.");
            return false;
        }

        // 클라이언트인 경우
        if (Runner.GameMode == GameMode.Client)
        {
            Debug.Log("클라이언트 접속완료");
            _isGameSceneLoading = false;
            _isInGame = true;
            _isGameSceneLoaded = true;
            RPC_MoveToGameScene();
            return false;
        }

        // 서버인 경우
        try
        {
            _isGameSceneLoading = true;
            _isGameSceneLoaded = false;
            _isInGame = false;

            _fadingTCS.Clear();
            foreach (var player in Players)
            {
                var playerRef = player.Key;
                _fadingTCS.Add(playerRef, new());
            }

            Debug.Log("모든 플레이어가 준비되었습니다!");

            // FadeOut 신호를 보내고 완료될때까지 서버는 대기
            // 씬로드 완료후 FadeIn은 GameStates(GameStagePlayingState) 에서 관리
            RPC_FadeOutUI();
            await WaitForAllPlayerFading();

            var gameScenePath = GlobalSetting.Inst.GameScenePath;
            await LevelManager.LoadSceneAsync(
                gameScenePath,
                UnityEngine.SceneManagement.LoadSceneMode.Additive,
                onLoadComplete: () =>
                {
                    Debug.Log("게임 씬 로드 완료");
                    _isGameSceneLoading = false;
                    _isInGame = true;
                    _isGameSceneLoaded = true;

                    // 서버가 세션을 '게임 중'으로 표시하여 랜덤 매치 대상에서 제외
                    var sessionProperties = new Dictionary<string, SessionProperty>();
                    sessionProperties["InGame"] = true;
                    Runner.SessionInfo.UpdateCustomProperties(sessionProperties);
                });

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"게임 시작 중 오류 발생: {e.Message}");
            return false;
        }
    }
    #endregion

    #region 씬이동 및 RPC
    /// <summary>
    /// Note - 게임씬 로드완료시 호출 / Only Server
    /// </summary>
    public void MoveToGameScene(string sceneName)
    {
        if (sceneName == GlobalSetting.Inst.FocusScenePath)
        {
            RPC_MoveToGameScene();
        }
    }

    /// <summary>
    /// 게임오브젝트를 특정씬으로 이동시키는 함수
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_MoveToGameScene()
    {
        foreach (var obj in this.Players.ToList().Select(x => x.Value.gameObject))
        {
            if (obj == null)
                continue;

            GameObject gameSceneObj = GameObject.Find("GameScene");
            if (gameSceneObj == null)
            {
                Debug.LogError("게임 씬 오브젝트를 찾을 수 없습니다.");
                continue;
            }
            Runner.MoveGameObjectToSameScene(obj, gameSceneObj);

            if (Runner.IsServer)
            {
                var teleporter = obj.GetComponent<PlayerStageController>();
                teleporter.SetPosition(new Vector2(0, 0));
            }
        }
    }

    /// <summary>
    /// 특정 플레이어를 게임 씬으로 이동
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_MovePlayerToGameScene(PlayerRef player)
    {
        var playerData = GetPlayerData(player);
        if (playerData != null)
        {
            var gameSceneObj = GameObject.Find("GameScene");
            if (gameSceneObj != null)
            {
                Runner.MoveGameObjectToSameScene(playerData.gameObject, gameSceneObj);
                var teleporter = playerData.GetComponent<PlayerStageController>();
                if (teleporter != null)
                {
                    teleporter.SetPosition(new Vector2(0, 0));
                }
            }
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public async void RPC_FadeOutUI()
    {
        await Fader.Inst.FadeOutAsync(Color.black, 1.0f);
        RPC_FadeOutCompleted(Runner.LocalPlayer);
    }

    /// <summary>
    /// 클라이언트에서 서버에게 FadeOut 완료 알림
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_FadeOutCompleted(PlayerRef player)
    {
        _fadingTCS[player].SetResult();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_FadeInUI()
    {
        UIEventSystem.Inst.TriggerGameUIActive(true);
        LobbyUI_Manager.Inst.DeactiveAllLobbyUI();
        _ = Fader.Inst.FadeInAsync(Color.black, 1.0f);
    }

    public void PlayerJoined(PlayerRef player)
    {
        throw new NotImplementedException();
    }


    #endregion
}
