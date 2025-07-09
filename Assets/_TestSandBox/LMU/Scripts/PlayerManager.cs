using System;
using Fusion;
using UnityEngine;

public class PlayerManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    private const int MAX_PLAYER_COUNT = 4;

    [Header("설정")]
    [SerializeField] private float currentPlayerCount = 0;

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


    /// <summary>
    /// 플레이어 등록 및 데이터 초기화
    /// </summary>
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

        // 추가실패
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

        // 제거실패
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

    public override void Render()
    {
        if (Players.Count >= 1)
        {
            OnPlayerDataRendered?.Invoke(Players);
        }
    }

}
