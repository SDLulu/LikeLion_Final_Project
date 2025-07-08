using Fusion;
using UnityEngine;

public class PlayerManage : NetworkBehaviour, IPlayerJoined
{
    private const int MAX_PLAYER_COUNT = 4;

    [Header("설정")]
    [SerializeField] private float currentPlayerCount = 0;

    // [Networked, Capacity(4)]
    // public NetworkLinkedList<TempNetPlayer> Players { get; } = default;

    /// <summary>
    /// 호스트에의해 한번만 생성 및 호출
    /// </summary>
    public override void Spawned()
    {
        if(Runner.IsServer)
        {
            // 찰나의 순간 플레이어가 동시에 여러명 존재하는경우 호스트를 식별해야함.
            var players = FindObjectsByType<TempNetPlayer>(FindObjectsSortMode.None);
            foreach(var player in players)
            {
                if (player == null)
                    continue;

                var localPlayer = LobbyManager.Inst.NetRunner.LocalPlayer;
                if(player.Object.StateAuthority == localPlayer)
                {
                    // Players.Add(player);
                    break;
                }
            }
        }

        
        DontDestroyOnLoad(this.gameObject);
    }

    public void PlayerJoined(PlayerRef player)
    {
        if(Runner.IsServer)
        {
            var tempPlayers = FindObjectsByType<TempNetPlayer>(FindObjectsSortMode.None);
            foreach(var tempPlayer in tempPlayers)
            {
                if(tempPlayer == null)
                    continue;

                // 입장 PlayerRef이 동일하고, 아직 추가되지 않은 플레이어인 경우
                if(tempPlayer.Object.StateAuthority == player)
                {
                    // Players.Add(tempPlayer);
                    break;
                }
            }
        }
    }


    private void AddPlayer(TempNetPlayer player)
    {
        // Players.Add(player);
    }

    private void RemovePlayer(TempNetPlayer player)
    {
        // Players.Remove(player);
    }

}
