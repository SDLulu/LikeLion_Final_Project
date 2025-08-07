using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using LMCore;
using UnityEngine;


public class PlayerManager : NetworkBehaviour, IsPollingSpawnable
{
    public static PlayerManager Inst => BaseManager<PlayerManager>.Inst;
    public static bool HasInstance => BaseManager<PlayerManager>.HasInstance;
    
    [Header("디버그용")]
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private bool isInGame = false;
    [SerializeField] private bool isGameSceneLoading = false;
    [SerializeField] private bool isGameSceneLoaded = false;

    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, PlayerData> Players => default;

    public Dictionary<PlayerRef, Action> OnPlayerDataChangedActions = new();
    public Dictionary<PlayerRef, AwaitableCompletionSource> playerFadingTCS = new();
    public int MinPlayersToStart => minPlayersToStart = (LobbyManager.Inst.IsSoloPlay ? 1 : 2);
    public bool IsSpawned {get; set;}
    public async Awaitable<bool> IsPollingSpawned()
    {
        while (IsSpawned == false)
        {
            await Awaitable.NextFrameAsync();
        }
        return true;
    }

    /// <summary>
    /// Note - 중간에 플레이어가 나가는 경우에 대한 예외처리를 하지않음
    /// </summary>
    public async Awaitable<bool> WaitForAllPlayerFading()
    {
        foreach (var player in playerFadingTCS)
        {
            await player.Value.Awaitable;
        }
        return true;
    }


    public NetworkDictionary<PlayerRef, PlayerData> GetPlayers()
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

    private List<PlayerData> _alivePlayers = new();
    public List<PlayerData> GetAlivePlayers()
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


    public override void Spawned()
    {
        IsSpawned = true;
        
        // Note - 기획변경으로 더이상 사용하지않음
        // var uiController = FindAnyObjectByType<LobbyUI_Manager>();
        // this.AddPlayerDataAction(uiController.UpdateData);
        // OnChangedPlayers();

        DontDestroyOnLoad(this.gameObject);

        if (Runner.IsServer)
        {
            NetworkEventSystem.Inst.OnSceneLoadDoneEvent += (runner, sceneName) =>  MoveToGameScene(sceneName);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        IsSpawned = false;
        OnPlayerDataChanged = null;
        playerFadingTCS.Clear();    
        Players.Clear();
        _alivePlayers.Clear();
    }

    // -- 플레이어 관리
    public void AddPlayer(PlayerRef player)
    {
        if (IsSpawned == false)
        {
            Debug.LogError("플레이어 매니저가 스폰되지 않았습니다.");
            return;
        }

        if (Runner.IsServer == false)
            return;

        var tempPlayers = FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
        if (tempPlayers == null || tempPlayers.Length <= 0)
        {
            Debug.LogError("플레이어를 추가하는데 실패했습니다. 플레이어가 존재하지 않습니다.");
            return;
        }
        
        foreach (var tempPlayer in tempPlayers)
        {
            
            if (tempPlayer.Object.InputAuthority == player)
            {
                Players.Add(player, tempPlayer);
                return;
            }
        }

        Debug.LogError($"플레이어를 추가하는데 실패했습니다. 요청된 PlayerRef {player}와 일치하는 InputAuthority를 찾을 수 없습니다.");
    }

    public List<(PlayerRef, NetworkObject)> TryRemoveAllPlayer()
    {
        var list = new List<(PlayerRef, NetworkObject)>();
        foreach (var player in Players)
        {
            list.Add(TryRemovePlayer(player.Value.Object.InputAuthority));
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
                return (tempPlayer.Value.Object.InputAuthority, tempPlayer.Value.Object);
            }
        }

        Debug.LogError("플레이어를 제거하는데 실패했습니다.");
        return (PlayerRef.None, null);
    }

    // --- 데이터 렌더링 액션
    public Action<NetworkDictionary<PlayerRef, PlayerData>> OnPlayerDataChanged;
    public void AddPlayerDataAction(Action<NetworkDictionary<PlayerRef, PlayerData>> action)
    {
        OnPlayerDataChanged += action;
    }
    public void RemovePlayerDataAction(Action<NetworkDictionary<PlayerRef, PlayerData>> action)
    {
        OnPlayerDataChanged -= action;
    }
    private void OnChangedPlayers()
    {
        OnPlayerDataChanged?.Invoke(GetPlayers());
    }

    public override void Render()
    {
        OnChangedPlayers();
    }

    /// <summary>
    /// 최소인원수 이상이면서 준비여부를 확인하는 함수 
    /// </summary>
    public bool AreAllPlayersReady()
    {
        int requiredPlayers = (LobbyManager.Inst.IsSoloPlay ? 1 : 2);
        if (Players.Count < requiredPlayers) 
            return false;
        
        foreach (var kvp in Players)
        {
            if (kvp.Value.IsReady == false) 
                return false;
        }
        
        return true;
    }

    public override async void FixedUpdateNetwork()
    {
        int requiredPlayers = MinPlayersToStart;
        if (Runner.IsServer && Players.Count >= requiredPlayers && isGameSceneLoading == false && isInGame == false)
        {
            var result = await TryStartGameAsync(isStart: AreAllPlayersReady());
            if (result)
            {
                // 게임 상태 StagePlaying 변경
                GameStates.Inst.DelayForceActiveState<GameStagePlayingState>();
            }
        }
    }


    public async Awaitable<bool> TryStartGameAsync(bool isStart = true)
    {
        // 준비 되지 않은경우
        if (isStart == false)
        {
            //Debug.Log("아직 준비되지 않은 플레이어가 있습니다.");
            await Awaitable.NextFrameAsync();
            return false;
        }

        // 클라이언트인 경우
        if (Runner.GameMode == GameMode.Client)
        {
            Debug.Log("클라이언트 접속완료");
            isGameSceneLoading = false;
            isInGame = true;
            isGameSceneLoaded = true;   
            RPC_MoveToGameScene();
            return false;
        }

        // 서버인 경우
        try
        {
            isGameSceneLoading = true;
            isGameSceneLoaded = false;
            isInGame = false;

            playerFadingTCS.Clear();
            foreach (var player in Players)
            {
                var playerRef = player.Value.Object.InputAuthority;
                playerFadingTCS.Add(playerRef, new());
            }

            Debug.Log("모든 플레이어가 준비되었습니다!");

            // FadeOut 신호를 보내고 완료될때까지 서버는 대기
            // 씬로드 완료후 FadeIn은 GameStates(GameStagePlayingState) 에서 관리
            RPC_FadeOutUI();
            await WaitForAllPlayerFading();

            var gameScenePath = GlobalSetting.Inst.FocusScenePath;
            await LevelManager.LoadSceneAsync(
                gameScenePath, 
                UnityEngine.SceneManagement.LoadSceneMode.Additive, 
                onLoadComplete: () =>
                {
                    Debug.Log("게임 씬 로드 완료");
                    isGameSceneLoading = false;
                    isInGame = true;
                    isGameSceneLoaded = true;   
                });

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"게임 시작 중 오류 발생: {e.Message}");
            return false;
        }
    }





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
    
    /// <summary>
    /// 플레이어 데이터 가져오기
    /// </summary>
    public PlayerData GetPlayerData(PlayerRef player)
    {
        if (Players.ContainsKey(player))
        {
            return Players[player];
        }
        return null;
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
        playerFadingTCS[player].SetResult();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_FadeInUI()
    {
        UIEventSystem.Inst.TriggerGameUIActive(true);
        LobbyUI_Manager.Inst.DeactiveAllLobbyUI();
        _= Fader.Inst.FadeInAsync(Color.black, 1.0f);
    }
}
