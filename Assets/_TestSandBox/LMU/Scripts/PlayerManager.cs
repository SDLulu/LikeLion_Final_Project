using System;
using System.Linq;
using Fusion;
using UnityEngine;

public class PlayerManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    private const int MAX_PLAYER_COUNT = 4;

    [Header("설정")]
    [SerializeField] private float currentPlayerCount = 0;
    [SerializeField] private int minPlayersToStart = 2; // 게임 시작 최소 인원

    [Networked, Capacity(4), UnitySerializeField]
    public NetworkDictionary<int, TempNetPlayer> Players => default;

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
        var tempPlayers = FindObjectsByType<TempNetPlayer>(FindObjectsSortMode.None);
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
    public Action<NetworkDictionary<int, TempNetPlayer>> OnPlayerDataRendered;
    public void AddRenderingAction(Action<NetworkDictionary<int, TempNetPlayer>> action)
    {
        OnPlayerDataRendered += action;
    }
    public void RemoveRenderingAction(Action<NetworkDictionary<int, TempNetPlayer>> action)
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
        
        if (Players.Count >= 2 && Runner.IsServer && isGameSceneLoading == false && isInGame == false)
        {
            TryStartGameAsync(isStart: AreAllPlayersReady());
        }
    }

    private bool isInGame = false;
    private bool isGameSceneLoading = false;
    public async void TryStartGameAsync(bool isStart = true)
    {
        if (isStart)
        {
            isGameSceneLoading = true;
            Debug.Log("모든 플레이어가 준비되었습니다!");

            await LevelManager.LoadSceneAsync(
                "DevGame", 
                UnityEngine.SceneManagement.LoadSceneMode.Additive, 
                onLoadComplete: () =>
                {
                    isGameSceneLoading = false;
                    isInGame = true;
                    Debug.Log("게임 씬 로드 완료");
                    RPC_MoveToGameScene();
                });
        }
        else
        {
            Debug.Log("아직 준비되지 않은 플레이어가 있습니다.");
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
