using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.FSM;
using UnityEngine;

public class EnemyMoveState : EnemyStateBase
{
    public override EnemyStateName StateName => EnemyStateName.Move;
    [SerializeField] protected float MoveDuration; //Move 지속시간


    protected override void OnEnterState()
    {
        Debug.Log("몬스터 무브 스테이트");
        float randomIdleDuration = Random.Range(1f, 5f);
        // fsmRef.EnemyNetworkBehaviour.StateTimer = TickTimer.CreateFromSeconds(Runner, randomIdleDuration);
        MoveDuration = randomIdleDuration;
    }

    protected override void OnEnterStateRender()
    {
        anim.CrossFadeInFixedTime(animState, animTransitionLength, 0, 0f);
    }

    protected override void OnFixedUpdate()
    {
        if (!fsmRef.Object.HasStateAuthority) return;

        // 타겟(플레이어)이 감지되면 Chase 상태로 전환합니다.
        if (fsmRef.EnemyNetworkBehaviour.TargetPlayer != null)
        {
            fsmRef.EnemyNetworkBehaviour.CurrentState = EnemyStateName.Chase;
            fsmRef.StateMachine.ForceActivateState<EnemyChaseState>();
            return;
        }

        if (Machine.StateTime > MoveDuration)
        {
            fsmRef.EnemyNetworkBehaviour.CurrentState = EnemyStateName.Idle;
            fsmRef.StateMachine.ForceActivateState<EnemyIdleState>();

        }
    }
}
