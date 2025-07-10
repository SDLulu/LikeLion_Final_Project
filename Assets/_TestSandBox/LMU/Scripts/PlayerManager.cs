using System;
using System.Linq;
using Fusion;
using UnityEngine;

public class PlayerManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Header("설정")]
    [SerializeField] private int minPlayersToStart = 2;                 // 게임 시작 최소 인원

    [Networked, Capacity(4), UnitySerializeField]
    public NetworkDictionary<int, PlayerData> Players => default;

    public override void Spawned()
    {
        if(Runner.IsServer)
        {
            TryAddPlayer(LobbyManager.Inst.NetRunner.LocalPlayer);
        }
        
        var uiController = FindAnyObjectByType<UI_Controller>();
        this.AddRenderingAction(uiController.UpdateData);
        
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

    public override async void FixedUpdateNetwork()
    {
        if (Runner.IsServer && Players.Count >= 2 && isGameSceneLoading == false && isInGame == false)
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
        if (isStart == false)
        {
            Debug.Log("아직 준비되지 않은 플레이어가 있습니다.");
            await Awaitable.NextFrameAsync();
            return false;
        }

        try
        {
            isGameSceneLoading = true;
            isGameSceneLoaded = false;
            isInGame = false;

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
            Runner.MoveGameObjectToSameScene(obj, GameObject.Find("GameScene"));
            var teleporter = obj.GetComponent<PlayerStageController>();
            teleporter.SetPosition(new Vector2(0, 0));
        }
    }
}
