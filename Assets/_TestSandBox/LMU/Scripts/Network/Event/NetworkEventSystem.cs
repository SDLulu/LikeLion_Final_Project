using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using LMCore;
using UnityEngine;

public class NetworkEventSystem : BaseManager<NetworkEventSystem>, INetworkRunnerCallbacks
{
    [Header("이벤트 핸들러들")]
    [SerializeField] private PlayerSpawnHandler spawnHandler;
    public event Action<NetworkRunner, PlayerRef> OnPlayerJoinedEvent;
    public event Action<NetworkRunner, PlayerRef> OnPlayerLeftEvent;
    public event Action<NetworkRunner, string> OnSceneLoadDoneEvent;

    private void Awake()
    {
        spawnHandler = this.GetOrAddComponent<PlayerSpawnHandler>();

        OnPlayerJoinedEvent += spawnHandler.HandlePlayerJoined;
        OnPlayerLeftEvent += spawnHandler.OnPlayerLeft;
    }

    private void OnDestroy()
    {
        OnPlayerJoinedEvent -= spawnHandler.HandlePlayerJoined;
        OnPlayerLeftEvent -= spawnHandler.OnPlayerLeft;
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        OnPlayerJoinedEvent?.Invoke(runner, player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        OnPlayerLeftEvent?.Invoke(runner, player);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {

    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        OnSceneLoadDoneEvent?.Invoke(runner, sceneName);
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }
}