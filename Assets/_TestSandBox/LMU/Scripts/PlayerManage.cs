using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerManage : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    private const int MAX_PLAYER_COUNT = 4;

    [Header("설정")]
    [SerializeField] private float currentPlayerCount = 0;

    [Networked, Capacity(4), UnitySerializeField]
    public NetworkDictionary<int, TempNetPlayer> Players => default;

    private UI_Controller uiController;
    public UI_Controller UIController
    {
        get
        {
            uiController ??= FindAnyObjectByType<UI_Controller>();
            return uiController;
        }
    }   

    /// <summary>
    /// 호스트에의해 한번만 생성 및 호출
    /// </summary>
    public override void Spawned()
    {
        if(Runner.IsServer)
        {
            TryAddPlayer(LobbyManager.Inst.NetRunner.LocalPlayer);
        }
        
        DontDestroyOnLoad(this.gameObject);
    }


    public override void Render()
    {
        // UI 업데이트
        if (Players.Count >= 1 && UIController != null)
        {
            UIController.UpdateData(Players);
        }
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


}
