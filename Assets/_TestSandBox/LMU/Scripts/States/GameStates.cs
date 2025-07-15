using System.Collections.Generic;
using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public class GameStates : NetworkBehaviour, IStateMachineOwner
{
    public static GameStates Inst => BaseManager<GameStates>.Inst;

    private void OnValidate()
    {
        if(lobbyState == null)
            lobbyState = GetComponentInChildren<LobbyState>();
        if(waitingState == null)
            waitingState = GetComponentInChildren<GameStageWaitingState>();
        if(playingState == null)
            playingState = GetComponentInChildren<GameStagePlayingState>();
        if(completedState == null)
            completedState = GetComponentInChildren<GameStageCompletedState>();
        if(transitionState == null)
            transitionState = GetComponentInChildren<GameStageTransitionState>();
        if(failedState == null)
            failedState = GetComponentInChildren<GameStageFailedState>();
    }
    
    [Header("인스펙터 참조")]
    [SerializeField] private LobbyState lobbyState;
    [SerializeField] private GameStageWaitingState waitingState;
    [SerializeField] private GameStagePlayingState playingState;
    [SerializeField] private GameStageCompletedState completedState;
    [SerializeField] private GameStageTransitionState transitionState;
    [SerializeField] private GameStageFailedState failedState;

    [Header("디버그용")]
    [SerializeField] private UI_Controller uiController = null;
    [SerializeField] private Fader fader = null;

    public UI_Controller UIController => uiController ?? UI_Controller.Inst;
    public Fader Fader => fader ?? Fader.Inst;

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        uiController = null;
        waitingState.UIController = null;
        lobbyState.UIController = null;
        playingState.UIController = null;
        completedState.UIController = null;
        transitionState.UIController = null;
        failedState.UIController = null;
        fader = null;
        waitingState.Fader = null;
        lobbyState.Fader = null;
        playingState.Fader = null;
        completedState.Fader = null;
        transitionState.Fader = null;
        failedState.Fader = null;
        base.Despawned(runner, hasState);
    }

    public int GetStateID<TState>() where TState : StateBehaviour
    {
        var state = StateMachine.GetState<TState>();
        var stateID = state.StateId;
        return stateID;
    }

    [field: SerializeField] public StateMachine<StateBehaviour> StateMachine {get; private set;}
    
    public void CollectStateMachines(List<IStateMachine> stateMachines)
    {
        StateMachine = new StateMachine<StateBehaviour>("GameState",
                        lobbyState, waitingState, playingState,
                        completedState, transitionState, failedState);

        stateMachines.Add(StateMachine);

        lobbyState.UIController = UIController;
        waitingState.UIController = UIController;
        playingState.UIController = UIController;
        completedState.UIController = UIController;
        transitionState.UIController = UIController;
        failedState.UIController = UIController;

        lobbyState.Fader = Fader;
        waitingState.Fader = Fader;
        playingState.Fader = Fader;
        completedState.Fader = Fader;
        transitionState.Fader = Fader;
        failedState.Fader = Fader;
    }


    public void ForceActiveState<TState>() where TState : StateBehaviour
    {
        var stateID = GetStateID<TState>();
        StateMachine.ForceActivateState(stateID);
    }

    private System.Tuple<int, StateBehaviour> delayedState;
    
    /// <summary>
    /// 네트워크신호(FixedUpdateNwtork)에 맞춰서 상태를 활성화
    /// </summary>
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
}
