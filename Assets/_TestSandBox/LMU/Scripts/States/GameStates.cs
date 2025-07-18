using System.Collections.Generic;
using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;
using System.Reflection;


public class GameStates : NetworkBehaviour, IStateMachineOwner
{
    public static GameStates Inst => BaseManager<GameStates>.Inst;

    public void Collect()
    {
        if (allStates == null || allStates.Length <= 0)
            allStates = GetComponentsInChildren<StateBehaviour>();
    }

    private void OnValidate() => Collect();
    private void Reset() => Collect();


    [Header("디버그용")]
    [SerializeField] private UI_Controller uiController = null;
    [SerializeField] private Fader fader = null;
    [SerializeField] private CutSceneController cutSceneController = null;
    [SerializeField] private PlayerManager playerManager = null;
    [SerializeField] private StateBehaviour[] allStates;
    [field: SerializeField] public StateMachine<StateBehaviour> StateMachine { get; private set; }
    public UI_Controller UIController => uiController ?? (uiController = UI_Controller.Inst);
    public Fader Fader => fader ?? (fader = Fader.Inst);
    public PlayerManager PlayerManager => playerManager ?? (playerManager = PlayerManager.Inst);
    public CutSceneController CutSceneController => cutSceneController ?? (cutSceneController = this.FindObjectByTypeAtCurScene<CutSceneController>());

    public override void Spawned()
    {
        base.Spawned();
        NetworkEventSystem.Inst.OnSceneLoadDoneEvent += (runner, sceneName) => OnSceneLoadDone();
    }

    // Note - CutSceneController가 GameScene에 존재해서 게임씬 로드시점까지 대기후 Inject 처리
    public void OnSceneLoadDone()
    {
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
        StateMachine = new StateMachine<StateBehaviour>("GameState", allStates);
        stateMachines.Add(StateMachine);
    }

    public int GetStateID<TState>() where TState : StateBehaviour
    {
        var state = StateMachine.GetState<TState>();
        return state.StateId;
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
    }


    // --- 인젝트
    private Dictionary<System.Type, object> refs;

    private void ApplyInject()
    {
        refs = new Dictionary<System.Type, object>
        {
            { typeof(UI_Controller), UIController },
            { typeof(Fader), Fader },
            { typeof(PlayerManager), PlayerManager },
            { typeof(CutSceneController), CutSceneController }
        };

        foreach (var state in allStates)
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

        foreach (var state in allStates)
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
} 