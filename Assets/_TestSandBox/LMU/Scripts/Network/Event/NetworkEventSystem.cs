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
    [Header("디버그용")]
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

    // --- 커스텀 이벤트
    public event Action<NetworkRunner, E_StateName, E_StateName> OnGameStateChangedEvent;  // (이전 상태, 현재 상태)
    public event Action<Stage.Data> OnStageLoadDoneEvent;                   // 스테이지 정보 - "3-1 or 5-4"
    public event Action<bool> OnCutSceneActiveEvent;                        // 컷씬 활성화 여부
    public event Action<PlayerRef, EnemyData> OnEnemyKilledEvent;           // 적 처치 (플레이어, 가중치)
    public event Action<PlayerRef, int> OnItemCollectedEvent;               // 아이템 획득 (플레이어, 가중치)
    public event Action<PlayerRef> OnScoreChangedEvent;                     // 점수 변경 알림 

    public void TriggerEnemyKilled(PlayerRef attacker, EnemyData enemyData)
    {
        if (IsServer() == false)
            return;
        OnEnemyKilledEvent?.Invoke(attacker, enemyData);
    }

    public void TriggerItemCollected(PlayerRef attacker, int scoreWeight = 1)
    {
        if (IsServer() == false)
            return;
        OnItemCollectedEvent?.Invoke(attacker, scoreWeight);
    }

    public void TriggerScoreChanged(PlayerRef attacker)
    {
        if (IsServer() == false)
            return;
        OnScoreChangedEvent?.Invoke(attacker);
    }

    public void TriggerGameStateChangedEvent(NetworkRunner runner, E_StateName previous, E_StateName current)
    {
        if (IsServer() == false)
            return;
        OnGameStateChangedEvent?.Invoke(runner, previous, current);
    }

    public void TriggerCutSceneActiveEvent(bool isActive)
    {
        if (IsServer() == false)
            return;
        OnCutSceneActiveEvent?.Invoke(isActive);
    }

    public void TriggerStageLoadDoneEvent(Stage.Data stageData)
    {
        if (IsServer() == false)
        {
            return;
        }
        var handler = OnStageLoadDoneEvent;
        if (handler == null)
        {
            return;
        }
        foreach (var del in handler.GetInvocationList())
        {
            var action = del as Action<Stage.Data>;
            if (action == null)
            {
                continue;
            }

            var method = action.Method;
            var target = action.Target;
            string targetInfo = "static";
            if (target is UnityEngine.MonoBehaviour mb)
            {
                // 파괴된 컴포넌트는 Unity의 null 비교에서 null로 동작함
                if (mb == null)
                {
                    targetInfo = "Destroyed(MonoBehaviour)";
                }
                else
                {
                    var go = mb.gameObject;
                    targetInfo = $"{mb.GetType().FullName} on GO='{go.name}' Scene='{go.scene.name}'";
                }
            }
            else if (target != null)
            {
                targetInfo = target.GetType().FullName;
            }

            try
            {
                action.Invoke(stageData);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning(
                    $"[StageLoadDoneEvent] 예외 발생 - {method.DeclaringType.FullName}.{method.Name} | Target={targetInfo}\n{ex}");
            }
        }
    }

#region NewtorkSpawn 시점에 따른 초기화 이벤트 처리
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
        foreach (var type in assembly.GetTypes().Where(t => t.IsDefined(typeof(NetworkSpawnDelayAttribute), false)))
        {
            _requiredManagerTypes.Add(type);
            Debug.Log($"[GameStateManager] 필수 매니저 발견: {type.Name}");
        }
    }

    public void RegisterNetDelay(NetworkBehaviour netDelay)
    {
        if (IsReady) 
            return;

        var managerType = netDelay.GetType();
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

    public void UnregisterNetDelay(NetworkBehaviour netDelay)
    {
        _registeredManagers.Remove(netDelay.GetType());
    }
#endregion

    private NetworkRunner _runner;
    private bool IsServer()
    {
        if (_runner == null)
        {
            return true;
        }
        return _runner.IsServer;
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
        OnConnectRequestEvent = null;
        OnPlayerJoinedEvent = null;
        OnPlayerLeftEvent = null;
        OnDisconnectedFromServerEvent = null;
        OnShutdownEvent = null;
        OnStageLoadDoneEvent = null;
        OnGameStateChangedEvent = null;
        OnEnemyKilledEvent = null;
        OnItemCollectedEvent = null;
        OnScoreChangedEvent = null;
        OnCutSceneActiveEvent = null;
        _spawnHandler = null;
        _connectionHandler = null;
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
        _runner = runner;
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
        _runner = runner;
        OnSceneLoadDoneEvent?.Invoke(runner, sceneName);
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        _runner = runner;
        OnSceneLoadStartEvent?.Invoke(runner, sceneName);
    }
}