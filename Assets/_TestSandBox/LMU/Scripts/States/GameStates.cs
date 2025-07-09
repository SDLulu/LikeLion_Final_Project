using System.Collections.Generic;
using Fusion;
using Fusion.Addons.FSM;
using UnityEngine;

public class GameStates : NetworkBehaviour, IStateMachineOwner
{
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
    }


    public void ForceActiveState<TState>() where TState : StateBehaviour
    {
        var stateID = GetStateID<TState>();
        StateMachine.ForceActivateState(stateID);
    }
}
