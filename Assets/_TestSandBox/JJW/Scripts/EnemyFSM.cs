using System.Collections;
using System.Collections.Generic;
using Fusion.Addons.FSM;
using Fusion;
using UnityEngine;

public class EnemyFSM : NetworkBehaviour, IStateMachineOwner
{
    private StateMachine<StateBehaviour> stateMachine;

    public StateMachine<StateBehaviour> StateMachine => stateMachine;

    public EnemyBase EnemyNetworkBehaviour;

    public void CollectStateMachines(List<IStateMachine> stateMachines)
    {
        // Collects all of the state machines as children.
        var childStates = GetComponentsInChildren<StateBehaviour>();
        stateMachine = new StateMachine<StateBehaviour>("Enemy FSM", childStates);
        stateMachines.Add(stateMachine);
    }
}
