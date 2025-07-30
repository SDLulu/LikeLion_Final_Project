using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.FSM;
using UnityEngine;

public class EnemyIdleState : EnemyStateBase
{
    public override EnemyStateName StateName => EnemyStateName.Idle;
    [SerializeField] private float idleDuration; //idle 지속시간


    protected override void OnEnterState()
    {
        Debug.Log("몬스터 아이들 스테이트");
        float randomIdleDuration = Random.Range(1f, 3f);
        // fsmRef.EnemyNetworkBehaviour.StateTimer = TickTimer.CreateFromSeconds(Runner, randomIdleDuration);
        idleDuration = randomIdleDuration;
    }

    protected override void OnEnterStateRender()
    {
        anim.CrossFadeInFixedTime(animState, animTransitionLength, 0, 0);
    }

    protected override void OnFixedUpdate()
    {
        // 제어 권한이 있는 서버(호스트)에서만 상태 전환 로직을 실행합니다.
        if (!fsmRef.Object.HasStateAuthority) return;

        //타겟이 감지되면 Chase 상태로 전환
        if (fsmRef.EnemyNetworkBehaviour.TargetPlayer != null)
        {
            fsmRef.EnemyNetworkBehaviour.CurrentState = EnemyStateName.Chase; //행동은 EnemyNetwworkBehaviour의 FUN 에서 처리
            fsmRef.StateMachine.ForceActivateState<EnemyChaseState>(); //렌더링은 FSM에서 처리
            return; //상태가 변경되었으므로 즉시 함수 종료
        }
        if (Machine.StateTime > idleDuration)
        {
            fsmRef.EnemyNetworkBehaviour.CurrentState = EnemyStateName.Move;
            fsmRef.StateMachine.ForceActivateState<EnemyMoveState>();
        }
    }
}
