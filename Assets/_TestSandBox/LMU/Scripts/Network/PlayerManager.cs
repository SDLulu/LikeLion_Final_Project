using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using LMCore;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Inst => BaseManager<PlayerManager>.Inst;
    public static bool HasInstance => BaseManager<PlayerManager>.HasInstance;
    
    [Header("설정")]
    [SerializeField] private int minPlayersToStart = 2;                 // 게임 시작 최소 인원
    [SerializeField] private bool testMode = false;                     // 테스트 모드 활성화
    [SerializeField] private int minPlayersForTest = 1;                 // 테스트 모드 최소 인원

    [Networked, Capacity(4), UnitySerializeField]
    public NetworkDictionary<int, PlayerData> Players => default;

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

    [SerializeField] public bool IsSpawned = false;
    public async Awaitable<bool> IsPollingSpawned()
    {
        while (IsSpawned == false)
        {
            await Awaitable.NextFrameAsync();
        }
        return true;
    }

    public override void Spawned()
    {
        IsSpawned = true;
        
        var uiController = FindAnyObjectByType<UI_Controller>();
        this.AddRenderingAction(uiController.UpdateData);
        
        DontDestroyOnLoad(this.gameObject);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        IsSpawned = false;
        OnPlayerDataRendered = null;
    }

    // public void PlayerJoined(PlayerRef player)
    // {
    //     if(Runner.IsServer)
    //     {
    //         AddPlayer(player);
            
    //         // Late Join 처리: 게임이 이미 진행 중이면 바로 게임 씬으로 이동
    //         if (isInGame && isGameSceneLoaded)
    //         {
    //             Debug.Log($"Late Join 플레이어 {player}를 게임 씬으로 이동시킵니다.");
                
    //             // 새로 입장한 플레이어만 게임 씬으로 이동
    //             var playerData = GetPlayerData(player);
    //             if (playerData != null)
    //             {
    //                 RPC_MovePlayerToGameScene(player);
    //                 RPC_SetLobbyUI(false);
    //             }
    //         }
    //     }
    // }

    // -- 플레이어 관리
    public void AddPlayer(PlayerRef player)
    {
        if (Runner.IsServer == false)
            return;

        if (IsSpawned == false)
        {
            Debug.LogError("플레이어 매니저가 스폰되지 않았습니다.");
            return;
        }

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
        int requiredPlayers = testMode ? minPlayersForTest : minPlayersToStart;
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
        int requiredPlayers = testMode ? minPlayersForTest : minPlayersToStart;
        if (Runner.IsServer && Players.Count >= requiredPlayers && isGameSceneLoading == false && isInGame == false)
        {
            var result = await TryStartGameAsync(isStart: AreAllPlayersReady());
            if (result)
            {
                // 게임 상태 StageWating 변경
                GameStates.Inst.DelayForceActiveState<GameStageWaitingState>();
            }
        }
    }

    [SerializeField] private bool isInGame = false;
    [SerializeField] private bool isGameSceneLoading = false;
    [SerializeField] private bool isGameSceneLoaded = false;
    public async Awaitable<bool> TryStartGameAsync(bool isStart = true)
    {
        if (Runner.GameMode == GameMode.Client)
        {
            Debug.Log("클라이언트 접속완료");
            isGameSceneLoading = false;
            isInGame = true;
            isGameSceneLoaded = true;   
            RPC_MoveToGameScene();
            return false;
        }

        if (isStart == false)
        {
            //Debug.Log("아직 준비되지 않은 플레이어가 있습니다.");
            await Awaitable.NextFrameAsync();
            return false;
        }

        try
        {
            isGameSceneLoading = true;
            isGameSceneLoaded = false;
            isInGame = false;

            Debug.Log("모든 플레이어가 준비되었습니다!");

            var gameScenePath = GlobalSetting.Inst.GameScenePath;
            await LevelManager.LoadSceneAsync(
                gameScenePath, 
                UnityEngine.SceneManagement.LoadSceneMode.Additive, 
                onLoadComplete: () =>
                {
                    Debug.Log("게임 씬 로드 완료");
                    isGameSceneLoading = false;
                    isInGame = true;
                    isGameSceneLoaded = true;   
                    RPC_MoveToGameScene();
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
    /// 게임오브젝트를 특정씬으로 이동시키는 함수
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
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
            var teleporter = obj.GetComponent<PlayerStageController>();
            teleporter.SetPosition(new Vector2(0, 0));
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

    /// <summary>
    /// 테스트 모드 활성화/비활성화
    /// </summary>
    public void SetTestMode(bool enabled)
    {
        if (Runner.IsServer)
        {
            testMode = enabled;
            Debug.Log($"테스트 모드 {(enabled ? "활성화" : "비활성화")} - 최소 인원: {(enabled ? minPlayersForTest : minPlayersToStart)}");
        }
    }

    /// <summary>
    /// 로비 UI 제어 RPC
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SetLobbyUI(bool active)
    {
        try
        {
            if (UI_Controller.Inst != null && UI_Controller.Inst.UILobby != null)
            {
                UI_Controller.Inst.UILobby.gameObject.SetActive(active);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"RPC_SetLobbyUI 중 오류: {e.Message}");
        }
    }
}
