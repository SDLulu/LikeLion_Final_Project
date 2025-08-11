using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusion;
using Fusion.Sockets;
using LMCore;
using UnityEngine;

public class NetworkEventSystem : BaseManager<NetworkEventSystem>, INetworkRunnerCallbacks
{
    private HashSet<Type> _requiredManagerTypes = new HashSet<Type>();
    private HashSet<Type> _registeredManagers = new HashSet<Type>();

    public bool IsReady { get; private set; } = false;
    public event Action OnAllManagersReady;

    /// <summary>
    /// 어셈블리를 스캔해 필수 매니저 목록 초기화
    /// </summary>
    private void DiscoverRequiredManagers()
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var type in assembly.GetTypes().Where(t => t.IsDefined(typeof(RequiredManagerAttribute), false)))
        {
            _requiredManagerTypes.Add(type);
            Debug.Log($"[GameStateManager] 필수 매니저 발견: {type.Name}");
        }
    }

    public void RegisterManager(NetworkBehaviour manager)
    {
        if (IsReady) 
            return;

        var managerType = manager.GetType();
        _registeredManagers.Add(managerType);
        CheckIfReady();
    }

    private void CheckIfReady()
    {
        if (_requiredManagerTypes.IsSubsetOf(_registeredManagers))
        {
            IsReady = true;
            OnAllManagersReady?.Invoke();
        }
    }

    public void UnregisterManager(NetworkBehaviour manager)
    {
        _registeredManagers.Remove(manager.GetType());
    }

    [Header("이벤트 핸들러")]
    [SerializeField] private PlayerSpawnHandler _spawnHandler;
    [SerializeField] private ConnectionHandler _connectionHandler;

    // -- 퓨전2 이벤트
    public event Action<NetworkRunner, PlayerRef> OnPlayerJoinedEvent;
    public event Action<NetworkRunner, PlayerRef> OnPlayerLeftEvent;
    public event Action<NetworkRunner, string> OnSceneLoadDoneEvent;
    public event Action<NetworkRunner, string> OnSceneLoadStartEvent;
    public event Action<NetworkRunner, NetworkRunnerCallbackArgs.ConnectRequest, byte[]> OnConnectRequestEvent;
    public event Action<NetworkRunner, NetDisconnectReason> OnDisconnectedFromServerEvent;
    public event Action<NetworkRunner, ShutdownReason> OnShutdownEvent;
    public event Action<NetworkRunner, PlayerRef> OnPlayerSpawnedEvent;
    public event Action<Stage.Data> OnStageLoadDoneEvent;

    // 게임 상태 변경 이벤트
    public event Action<E_StateName, E_StateName> OnGameStateChangedEvent;  // (이전 상태, 현재 상태)

    public void TriggerStageLoadDoneEvent(Stage.Data stageInfo)    // 스테이지 정보 - "3-1 or 5-4"
    {
        OnStageLoadDoneEvent?.Invoke(stageInfo);
    }
    public void TriggerGameStateChangedEvent(E_StateName previousState, E_StateName currentState)
    {
        OnGameStateChangedEvent?.Invoke(previousState, currentState);
        Debug.Log($"게임 상태 변경: {previousState} → {currentState}");
    }

    protected override void Awake()
    {
        base.Awake();
        DiscoverRequiredManagers();
        _spawnHandler = this.GetOrAddComponent<PlayerSpawnHandler>();
        _connectionHandler = this.GetOrAddComponent<ConnectionHandler>();

        OnConnectRequestEvent += _connectionHandler.OnConnectRequest;
        OnPlayerJoinedEvent += _spawnHandler.OnPlayerJoined;
        OnPlayerLeftEvent += _spawnHandler.OnPlayerLeft;
        OnDisconnectedFromServerEvent += _connectionHandler.OnDisconnectedFromServer;
        OnShutdownEvent += _connectionHandler.OnShutdown;
    }

    private void OnDestroy()
    {
        OnConnectRequestEvent -= _connectionHandler.OnConnectRequest;
        OnPlayerJoinedEvent -= _spawnHandler.OnPlayerJoined;
        OnPlayerLeftEvent -= _spawnHandler.OnPlayerLeft;
        OnDisconnectedFromServerEvent -= _connectionHandler.OnDisconnectedFromServer;
        OnShutdownEvent -= _connectionHandler.OnShutdown;

        OnStageLoadDoneEvent = null;
        OnGameStateChangedEvent = null;
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
        OnConnectRequestEvent?.Invoke(runner, request, token);
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.Log($"OnConnectFailed 연결실패 - {remoteAddress} - {reason}");
        // 클라이언트 측 UX: 접속 실패 시 타이틀로 복귀
        try
        {
            if (runner != null && runner.IsRunning == false)
            {
                LobbyUI_Manager.Inst.ActiveTitlePanel();
            }
        }
        catch (System.Exception)
        {
            // UI 싱글톤이 아직 준비되지 않은 경우 무시
        }
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