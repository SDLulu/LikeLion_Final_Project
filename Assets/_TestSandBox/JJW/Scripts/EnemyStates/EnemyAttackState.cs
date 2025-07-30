using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Addons.FSM;

public class EnemyAttackState : EnemyStateBase
{
    public override EnemyStateName StateName => EnemyStateName.Attack;
    [SerializeField] private float attackDuration = 1f; // 공격 애니메이션/판정 시간

    protected override void OnEnterState()
    {
        fsmRef.EnemyNetworkBehaviour.AttackCooldownTimer = TickTimer.CreateFromSeconds(Runner, fsmRef.EnemyNetworkBehaviour.enemyData.attackCooldown);
    }

    protected override void OnEnterStateRender()
    {
        anim.CrossFadeInFixedTime(animState, animTransitionLength, 0, 0f);
    }

    protected override void OnFixedUpdate()
    {
        if (!fsmRef.Object.HasStateAuthority) return;

        // 공격 모션 시간이 끝나면 다시 Chase 상태로 돌아감
        if (Machine.StateTime > attackDuration)
        {
            fsmRef.EnemyNetworkBehaviour.CurrentState = EnemyStateName.Chase;
            fsmRef.StateMachine.ForceActivateState<EnemyChaseState>();
        }
    }
}
