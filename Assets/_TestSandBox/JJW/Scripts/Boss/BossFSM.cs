using System.Collections.Generic;
using Fusion;
using Fusion.Addons.FSM;
using UnityEngine;

public class BossFSM : NetworkBehaviour, IStateMachineOwner
{
    private StateMachine<StateBehaviour> stateMachine;

    public StateMachine<StateBehaviour> StateMachine => stateMachine;

    public BossBase BossNetworkBehaviour;
    public void CollectStateMachines(List<IStateMachine> stateMachines)
    {
        // Collects all of the state machines as children.
        var childStates = GetComponentsInChildren<StateBehaviour>();
        stateMachine = new StateMachine<StateBehaviour>("Boss FSM", childStates);
        stateMachines.Add(stateMachine);
    }
}
