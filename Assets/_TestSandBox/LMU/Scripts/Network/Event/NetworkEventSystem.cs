using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using LMCore;
using UnityEngine;

public class NetworkEventSystem : BaseManager<NetworkEventSystem>, INetworkRunnerCallbacks
{
    [Header("이벤트 핸들러")]
    [SerializeField] private PlayerSpawnHandler spawnHandler;
    [SerializeField] private HostDisconnectHandler hostDisconnectHandler;

    // -- 퓨전2 이벤트
    public event Action<NetworkRunner, PlayerRef> OnPlayerJoinedEvent;
    public event Action<NetworkRunner, PlayerRef> OnPlayerLeftEvent;
    public event Action<NetworkRunner, string> OnSceneLoadDoneEvent;
    public event Action<NetworkRunner, string> OnSceneLoadStartEvent;
    public event Action<NetworkRunner, NetDisconnectReason> OnDisconnectedFromServerEvent;
    public event Action<NetworkRunner, ShutdownReason> OnShutdownEvent;

    public event Action<string> OnStageLoadDoneEvent;

    public void TriggerStageLoadDoneEvent(string stageInfo)    // 스테이지 정보 - "3-1 or 5-4"
    {
        OnStageLoadDoneEvent?.Invoke(stageInfo);
    }


    private void Awake()
    {
        spawnHandler = this.GetOrAddComponent<PlayerSpawnHandler>();
        hostDisconnectHandler = this.GetOrAddComponent<HostDisconnectHandler>();

        OnPlayerJoinedEvent += spawnHandler.OnPlayerJoined;
        OnPlayerLeftEvent += spawnHandler.OnPlayerLeft;
        OnDisconnectedFromServerEvent += hostDisconnectHandler.OnDisconnectedFromServer;
        OnShutdownEvent += hostDisconnectHandler.OnShutdown;
    }

    private void OnDestroy()
    {
        OnPlayerJoinedEvent -= spawnHandler.OnPlayerJoined;
        OnPlayerLeftEvent -= spawnHandler.OnPlayerLeft;
        OnDisconnectedFromServerEvent -= hostDisconnectHandler.OnDisconnectedFromServer;
        OnShutdownEvent -= hostDisconnectHandler.OnShutdown;

        OnStageLoadDoneEvent = null;
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
        OnShutdownEvent?.Invoke(runner, shutdownReason);
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        OnDisconnectedFromServerEvent?.Invoke(runner, reason);
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        Debug.Log($"OnConnectRequest 연결요청 - {request.RemoteAddress}");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.Log($"OnConnectFailed 연결실패 - {remoteAddress} - {reason}");
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
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        OnSceneLoadStartEvent?.Invoke(runner, sceneName);
    }
}