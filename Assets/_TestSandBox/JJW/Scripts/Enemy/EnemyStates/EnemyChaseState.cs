using System.Collections;
using System.Collections.Generic;
using Fusion.Addons.FSM;
using UnityEngine;

public class EnemyChaseState : EnemyStateBase
{
    public override EnemyStateName StateName => EnemyStateName.Chase;

    protected override void OnEnterState()
    {

    }

    protected override void OnEnterStateRender()
    {
        anim.CrossFadeInFixedTime(animState, animTransitionLength, 0, 0f);
    }

    protected override void OnFixedUpdate()
    {
        //타겟이 벗어났다면
        // if (fsmRef.EnemyNetworkBehaviour.TargetPlayer == null)
        // {
        //     fsmRef.EnemyNetworkBehaviour.CurrentState = EnemyStateName.Idle;
        //     fsmRef.StateMachine.ForceActivateState<EnemyIdleState>();
        //     return;
        // }

        //타겟이 공격사정거리 안에 들어왔다면
        // if (Vector2.Distance(fsmRef.EnemyNetworkBehaviour.transform.position, fsmRef.EnemyNetworkBehaviour.TargetPlayer.transform.position) < fsmRef.EnemyNetworkBehaviour.enemyData.attackRange
        // && fsmRef.EnemyNetworkBehaviour.AttackCooldownTimer.ExpiredOrNotRunning(Runner))
        // {
        //     fsmRef.EnemyNetworkBehaviour.CurrentState = EnemyStateName.Attack;
        //     fsmRef.StateMachine.ForceActivateState<EnemyAttackState>();
        // }
    }
}
