using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.FSM;
using UnityEngine;

public class EnemyHitReactState : EnemyStateBase
{
    public override EnemyStateName StateName => EnemyStateName.HitReact;
    [SerializeField] private float HitReactDuration = 0.5f; //idle 지속시간

    protected override void OnEnterState()
    {
        fsmRef.EnemyNetworkBehaviour.StateTimer = TickTimer.CreateFromSeconds(Runner, HitReactDuration);
    }

    protected override void OnEnterStateRender()
    {
        anim.CrossFadeInFixedTime(animState, animTransitionLength, 0, 0f);
    }

    protected override void OnFixedUpdate()
    {
        // Debug.Log("Machine.StateTime : " + Machine.StateTime);

        // if (Machine.StateTime > HitReactDuration)
        // {
        //     fsmRef.EnemyNetworkBehaviour.CurrentState = EnemyStateName.Move;
        //     fsmRef.StateMachine.ForceActivateState<EnemyMoveState>();
        // }
    }
}
