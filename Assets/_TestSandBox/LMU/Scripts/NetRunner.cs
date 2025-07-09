using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class NetRunner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("디버그용")]
    [SerializeField] private GameMode localGameMode;
    [SerializeField] private PlayerManager hostPlayerManage;

    private void OnDestroy()
    {
        hostPlayerManage = null;
    }


    public PlayerRef LocalPlayer {get; private set;}

    
    /// <summary>
    /// 로비 입장
    /// </summary>
    public async void JoinOrCreateLobby(GameMode mode = GameMode.AutoHostOrClient,
                                        string roomName = "TestRoom",
                                        Action OnEnterLobby = default)
    {
        localGameMode = mode;
        var netRunner = GetComponent<NetworkRunner>();

        if (netRunner == null)
        {
            Debug.LogError("네트워크 러너가 존재하지 않습니다.");
            return;
        }

        netRunner.AddCallbacks(this);
        netRunner.ProvideInput = true;

       var startGameArgs = new StartGameArgs()
       {
           GameMode = mode,                   
           SessionName = roomName,       
           PlayerCount = 4,      
           SceneManager = LevelManager.Inst,      
       };

       var startGameTask = netRunner.StartGame(startGameArgs);
       await startGameTask;
       OnEnterLobby?.Invoke();
       
       Debug.Log($"방에 입장함 {roomName}");
    }

    /// <summary>
    /// 네트워크 접속해제
    /// </summary>
    public void Shutdown(Action OnShutdown = default)
    {
        var netRunner = GetComponent<NetworkRunner>();
        netRunner.Shutdown();
        OnShutdown?.Invoke();
    }

    [SerializeField] private GameObject playerMPrefab;
    [SerializeField] private GameObject tempNetPlayerPrefab;

    /// <summary>
    /// 플레이어 입장 및 생성
    /// </summary>
    public async void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"플레이어 {player} 입장!");
        if (runner.IsServer)
        {
            LocalPlayer = player;

            // 호스트인경우, 객체 생성후 네트워크 등록까지 대기
            if (localGameMode == GameMode.Host && hostPlayerManage == null)
            {
                await runner.SpawnAsync(tempNetPlayerPrefab, Vector3.zero, Quaternion.identity, player,
                onCompleted: (NetworkSpawnOp obj) =>
                {
                    var gameManagerObj = runner.Spawn(playerMPrefab, Vector3.zero, Quaternion.identity, player);
                    hostPlayerManage = gameManagerObj.GetComponent<PlayerManager>();
                });
            }
            // 클라이언트인 경우
            else
            {
                runner.Spawn(tempNetPlayerPrefab, Vector3.zero, Quaternion.identity, player);
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }









    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }


    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }
}
