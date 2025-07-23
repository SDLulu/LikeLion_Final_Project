using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using LMCore;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Inst => BaseManager<PlayerManager>.Inst;
    public static bool HasInstance => BaseManager<PlayerManager>.HasInstance;
    
    [Header("설정")]
    [SerializeField] private int minPlayersToStart = 2;                 // 게임 시작 최소 인원

    [Header("디버그용")]
    [SerializeField] private bool isInGame = false;
    [SerializeField] private bool isGameSceneLoading = false;
    [SerializeField] private bool isGameSceneLoaded = false;
    [SerializeField] public bool IsSpawned = false;

    [Networked, Capacity(4), UnitySerializeField]
    public NetworkDictionary<int, PlayerData> Players => default;

    public Dictionary<PlayerRef, AwaitableCompletionSource> playerFadingTCS = new();

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


    public NetworkDictionary<int, PlayerData> GetPlayers()
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
        if (Players.ContainsKey(player.AsIndex))
            return true;
        return false;
    }


    public override void Spawned()
    {
        IsSpawned = true;
        
        var uiController = FindAnyObjectByType<UI_Controller>();
        this.AddRenderingAction(uiController.UpdateData);
        DontDestroyOnLoad(this.gameObject);

        NetworkEventSystem.Inst.OnSceneLoadStartEvent += (runner, sceneName) =>  StartSceneLoad(sceneName);

        if (Runner.IsServer)
        {
            NetworkEventSystem.Inst.OnSceneLoadDoneEvent += (runner, sceneName) =>  MoveToGameScene(sceneName);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        IsSpawned = false;
        OnPlayerDataRendered = null;
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
                Players.Add(tempPlayer.Object.InputAuthority.AsIndex, tempPlayer);
                return;
            }
        }

        Debug.LogError("플레이어를 추가하는데 실패했습니다.");
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
            if (tempPlayer.Key == player.AsIndex)
            {
                Players.Remove(tempPlayer.Key);
                return (tempPlayer.Value.Object.InputAuthority, tempPlayer.Value.Object);
            }
        }

        Debug.LogError("플레이어를 제거하는데 실패했습니다.");
        return (PlayerRef.None, null);
    }

    // --- 데이터 렌더링 액션
    public Action<NetworkDictionary<int, PlayerData>> OnPlayerDataRendered;
    public void AddRenderingAction(Action<NetworkDictionary<int, PlayerData>> action)
    {
        OnPlayerDataRendered += action;
    }
    public void RemoveRenderingAction(Action<NetworkDictionary<int, PlayerData>> action)
    {
        OnPlayerDataRendered -= action;
    }


    /// <summary>
    /// 최소인원수 이상이면서 준비여부를 확인하는 함수 
    /// </summary>
    public bool AreAllPlayersReady()
    {
        int requiredPlayers = minPlayersToStart;
        if (Players.Count < requiredPlayers) 
            return false;
        
        foreach (var kvp in Players)
        {
            if (kvp.Value.IsReady == false) 
                return false;
        }
        
        return true;
    }

    public override void Render()
    {
        if (Players.Count >= 1)
        {
            OnPlayerDataRendered?.Invoke(GetPlayers());
        }
    }

    public override async void FixedUpdateNetwork()
    {
        int requiredPlayers = minPlayersToStart;
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




    private CinemachineCamera _cinemachineCamera;
    private CinemachineBrain _cinemachineBrain;
    /// <summary>
    /// Note - 씬로드 시작시 호출 / Both - Server, Client
    /// 로비의 카메라 참조 저장
    /// </summary>
    public void StartSceneLoad(string sceneName)
    {
        if (sceneName == GlobalSetting.Inst.LobbyScenePath)
        {
            _cinemachineCamera = this.FindObjectByTypeAtCurScene<CinemachineCamera>();
            _cinemachineBrain = this.FindObjectByTypeAtCurScene<CinemachineBrain>();
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
        // 게임씬 카메라 제거
        var gameSceneCamera = this.FindObjectsByTypeAtCurScene<Camera>().FirstOrDefault(x=> x.tag != "MainCamera");
        if (gameSceneCamera == null)
        {
            Debug.LogError("게임씬 카메라를 찾을 수 없습니다.");
            return;
        }
        else
        {
            GameObject.Destroy(gameSceneCamera.gameObject);
        }

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
            Runner.MoveGameObjectToSameScene(_cinemachineCamera.gameObject, gameSceneObj);
            Runner.MoveGameObjectToSameScene(_cinemachineBrain.gameObject, gameSceneObj);

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
    private PlayerData GetPlayerData(PlayerRef player)
    {
        if (Players.ContainsKey(player.AsIndex))
        {
            return Players[player.AsIndex];
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
        UI_Controller.Inst.DeactiveAllLobbyUI();
        _= Fader.Inst.FadeInAsync(Color.black, 1.0f);
    }
}
