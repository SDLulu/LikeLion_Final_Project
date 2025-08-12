using System.Collections.Generic;
using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;
using System.Reflection;


[NetworkSpawnManager(typeof(GameStates))]
public class GameStates : NetworkBehaviour, IStateMachineOwner
{
    public static GameStates Inst => BaseManager<GameStates>.Inst;
    
    public void Collect()
    {
        if (_allStates == null || _allStates.Length <= 0)
            _allStates = GetComponentsInChildren<StateBehaviour>();
    }

    private void OnValidate() => Collect();
    private void Reset() => Collect();


    [Header("디버그용")]
    [SerializeField] private LobbyUI_Manager _uiController = null;
    [SerializeField] private Fader _fader = null;
    [SerializeField] private CutSceneController _cutSceneController = null;
    [SerializeField] private PlayerManager _playerManager = null;
    [SerializeField] private NetworkEventSystem _networkEventSystem = null;
    [SerializeField] private StateBehaviour[] _allStates;
    [field: SerializeField] public StateMachine<StateBehaviour> StateMachine { get; private set; }
    public bool IsSpawned { get; set; }

    private E_StateName _previousStateName = E_StateName.GameStageWaitingState;
    public override void Spawned()
    {
        IsSpawned = true;
        base.Spawned();
        DontDestroyOnLoad(this);
        NetworkEventSystem.Inst.RegisterManager(this);
        NetworkEventSystem.Inst.OnSceneLoadDoneEvent += (runner, sceneName) => OnSceneLoadDone(sceneName);
        ApplyInject();
    }
    
    public void OnSceneLoadDone(string sceneName)
    {   
        // CutSceneController가 GameScene에 존재해서 씬변경시 한번더 주입
        if (sceneName == GlobalSetting.Inst.GameScenePath)
            ApplyInject();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        delayedState = null;
        ClearInject();
        base.Despawned(runner, hasState);
    }

    public void CollectStateMachines(List<IStateMachine> stateMachines)
    {
        Collect();
        StateMachine = new StateMachine<StateBehaviour>("GameState", _allStates);
        stateMachines.Add(StateMachine);
    }

    public int GetStateID<TState>() where TState : StateBehaviour
    {
        var state = StateMachine.GetState<TState>();
        return state.StateId;
    }

    public E_StateName GetActiveStateName()
    {
        var state = StateMachine.ActiveState as BaseStateBehaviour;
        if (state == null)
            return E_StateName.GameStageWaitingState;

        return state.StateName;
    }

    public void ForceActiveState<TState>() where TState : StateBehaviour
    {
        var stateID = GetStateID<TState>();
        StateMachine.ForceActivateState(stateID);
    }


    // --- 딜레이 상태
    private System.Tuple<int, StateBehaviour> delayedState;

    public void DelayForceActiveState<TState>() where TState : StateBehaviour
    {
        if (delayedState != null)
        {
            Debug.LogWarning("딜레이 상태가 존재할때 또 호출되었습니다.");
            Debug.LogWarning($"이전 딜레이 상태는 무시됩니다. - {delayedState.Item2.Name}");
            delayedState = null;
        }
        delayedState = new(GetStateID<TState>(), StateMachine.GetState<TState>());
    }

    public override void FixedUpdateNetwork()
    {
        if (delayedState != null)
        {
            StateMachine.ForceActivateState(delayedState.Item1);
            delayedState = null;
        }
        
        if (Runner.IsServer)
        {
            CheckStateChange();
        }
    }
    
    /// <summary>
    /// 상태 변경 감지 및 이벤트 발생
    /// </summary>
    private void CheckStateChange()
    {
        var currentStateName = GetActiveStateName();
        
        if (_previousStateName != currentStateName)
        {
            NetworkEventSystem.Inst?.TriggerGameStateChangedEvent(_previousStateName, currentStateName);
            _previousStateName = currentStateName;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public static async void RPC_FadeOutUI(NetworkRunner runner, float duration = 1.0f)
    {
        while (Fader.Inst.IsFading)
        {
            await Awaitable.NextFrameAsync();
        }
        _ = Fader.Inst.FadeOutAsync(Color.black, duration);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public static async void RPC_FadeInUI(NetworkRunner runner)
    {
        while (Fader.Inst.IsFading)
        {
            await Awaitable.NextFrameAsync();
        }

        UIEventSystem.Inst.TriggerGameUIActive(true);
        LobbyUI_Manager.Inst.DeactiveAllLobbyUI();
        _ = Fader.Inst.FadeInAsync(Color.black, 1.0f);
    }


    // --- 인젝트
    private Dictionary<System.Type, object> refs;

    private void ApplyInject()
    {
        // Note - CutSceneController는 게임씬에 존재, Manager아님
        refs = new Dictionary<System.Type, object>
        {
            { typeof(LobbyUI_Manager), _uiController != null ? _uiController : LobbyUI_Manager.Inst },
            { typeof(Fader), _fader != null ? _fader : Fader.Inst },
            { typeof(PlayerManager), _playerManager != null ? _playerManager : PlayerManager.Inst },
            { typeof(CutSceneController), _cutSceneController != null ? _cutSceneController : this.FindObjectByTypeAtCurScene<CutSceneController>() },
            { typeof(NetworkEventSystem), _networkEventSystem != null ? _networkEventSystem : NetworkEventSystem.Inst }
        };

        foreach (var state in _allStates)
        {
            if (state == null) 
            {
                Debug.LogError($"Inject 할수 없어요 .{state.GetType().Name}");
                continue;
            }

            var stateType = state.GetType();
            var properties = stateType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                if (property.CanWrite && refs.ContainsKey(property.PropertyType))
                {
                    var dependency = refs[property.PropertyType];
                    property.SetValue(state, dependency);
                }
            }
        }
    }

    private void ClearInject()
    {
        if (refs == null || refs.Count <= 0) 
            return;

        foreach (var state in _allStates)
        {
            if (state == null) 
                continue;

            var stateType = state.GetType();
            var properties = stateType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                if (property.CanWrite && refs.ContainsKey(property.PropertyType))
                    property.SetValue(state, null);
            }
        }

        refs.Clear();
    }

    public async Awaitable<bool> IsPollingSpawned()
    {
        while (IsSpawned == false)
        {
            await Awaitable.NextFrameAsync();
        }
        return true;
    }


} 