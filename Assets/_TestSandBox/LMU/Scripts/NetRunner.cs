using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class NetRunner : MonoBehaviour, INetworkRunnerCallbacks
{
    /// <summary>
    /// 로비 입장
    /// </summary>
    public void JoinOrCreateLobby(GameMode mode = GameMode.AutoHostOrClient, string roomName = "TestRoom", string sceneName = "_TestSandBox/LMU/Scenes/DevLobby")
    {
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
           SceneManager = netRunner.GetComponent<INetworkSceneManager>(), 
           ObjectProvider = netRunner.GetComponent<ObjectPoolingManager>() 
       };

       LM_SceneManager.Inst.LoadScene(sceneName);
       netRunner.StartGame(startGameArgs);
       Debug.Log($"방에 입장함{roomName}");
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

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
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
