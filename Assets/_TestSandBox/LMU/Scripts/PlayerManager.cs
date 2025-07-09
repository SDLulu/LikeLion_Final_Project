using System;
using System.Linq;
using Fusion;
using UnityEngine;

public class PlayerManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    private const int MAX_PLAYER_COUNT = 4;

    [Header("설정")]
    [SerializeField] private float currentPlayerCount = 0;
    [SerializeField] private int minPlayersToStart = 2;                 // 게임 시작 최소 인원
    [SerializeField] private float toGameSceneLoadingDelay = 3.0f;      // 게임 씬 로드 최소 딜레이

    [Networked, Capacity(4), UnitySerializeField]
    public NetworkDictionary<int, PlayerData> Players => default;

    public override void Spawned()
    {
        if(Runner.IsServer)
        {
            TryAddPlayer(LobbyManager.Inst.NetRunner.LocalPlayer);
        }
        
        var uiController = FindAnyObjectByType<UI_Controller>();
        if (uiController != null)
        {
            this.AddRenderingAction(uiController.UpdateData);
        }
        
        DontDestroyOnLoad(this.gameObject);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        OnPlayerDataRendered = null;
    }

    public void PlayerJoined(PlayerRef player)
    {
        if(Runner.IsServer)
        {
            TryAddPlayer(player);
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (Runner.IsServer)
        {
            TryRemovePlayer(player);    
        }
    }

    // -- 플레이어 관리
    private void TryAddPlayer(PlayerRef player)
    {
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

    private void TryRemovePlayer(PlayerRef player)
    {
        if (Players.Count <= 0)
        {
            Debug.LogError("플레이어를 제거하는데 실패했습니다. 등록된 플레이어가 존재하지 않습니다.");
            return;
        }

        foreach (var tempPlayer in Players)
        {
            if (tempPlayer.Key == player.AsIndex)
            {
                Players.Remove(tempPlayer.Key);
                return;
            }
        }

        Debug.LogError("플레이어를 제거하는데 실패했습니다.");
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
        if (Players.Count < minPlayersToStart) 
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
            OnPlayerDataRendered?.Invoke(Players);
        }
    }

    private TickTimer loadingDelayTimer = TickTimer.None;
    public override void FixedUpdateNetwork()
    {
        if (Runner.IsServer && Players.Count >= 2 && isGameSceneLoading == false && isInGame == false)
        {
            var ret = TryStartGameAsync(isStart: AreAllPlayersReady());

            // 게임씬으로 이동시 최소딜레이시간 타이머 설정
            if(ret.GetAwaiter().GetResult())
            {
                loadingDelayTimer = TickTimer.None;
            }
            else
            {
                loadingDelayTimer = TickTimer.CreateFromSeconds(Runner, toGameSceneLoadingDelay);
            }
        }

        if (Runner.IsServer && isGameSceneLoaded && loadingDelayTimer.Expired(Runner))
        {
            var gameStates = FindAnyObjectByType<GameStates>();
            gameStates.ForceActiveState<GameStagePlayingState>();
        }
        else if (Runner.IsServer && isGameSceneLoaded && loadingDelayTimer.Expired(Runner) == false)
        {
            Debug.Log($"게임씬로드 중 딜레이 중입니다. 남은 시간: {loadingDelayTimer.RemainingTime(Runner)}");
        }
    }

    [SerializeField] private bool isInGame = false;
    [SerializeField] private bool isGameSceneLoading = false;
    [SerializeField] private bool isGameSceneLoaded = false;
    public async Awaitable<bool> TryStartGameAsync(bool isStart = true)
    {
        if (isStart)
        {

            var gameStates = FindAnyObjectByType<GameStates>();
            gameStates.ForceActiveState<GameStageWaitingState>();

            isGameSceneLoading = true;
            isGameSceneLoaded = false;

            Debug.Log("모든 플레이어가 준비되었습니다!");

            await LevelManager.LoadSceneAsync(
                "DevGame", 
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
        else
        {
            Debug.Log("아직 준비되지 않은 플레이어가 있습니다.");
            return false;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_MoveToGameScene()
    {
        foreach (var obj in this.Players.ToList().Select(x => x.Value.gameObject))
        {
            if (obj == null)
                continue;
            Runner.MoveGameObjectToSameScene(obj, GameObject.Find("GameScene"));
        }
    }
}
