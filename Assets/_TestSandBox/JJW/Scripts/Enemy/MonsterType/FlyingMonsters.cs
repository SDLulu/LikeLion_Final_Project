using UnityEngine;
using Fusion;
using Fusion.Addons.FSM;
using Fusion.Addons.Physics;

public class FlyingMonsters : EnemyBase
{
    [Header("Flying Monster Settings")]
    [SerializeField] private float hoverSpeed = 2f;    // 위아래로 움직이는 속도
    [SerializeField] private float hoverAmplitude = 0.5f; // 위아래로 움직이는 폭
    [SerializeField] private float patrolRadius = 3f;    // 무한대 패턴의 크기
    [SerializeField] private float patrolSpeed = 1f;     // 무한대 패턴을 도는 속도

    [Networked] protected Vector2 PatrolAnchorPosition { get; set; } // 대기/정찰 상태의 기준 위치
    [Networked] protected float PatrolAngle { get; set; } // 정찰 패턴의 진행 각도
    protected override void UpdateIdleState()
    {
        // 플레이어와 동일 정책: 조작이 불가한 4상태에서는 인위적 속도 제어 차단
        if (IsDead || IsStunned || IsHeld || IsThrown)
        {
            return;
        }
        // 타겟이 감지되면 즉시 Chase 상태로 전환
        if (TargetPlayer != null)
        {
            CurrentState = EnemyStateName.Chase;
            fsm.StateMachine.ForceActivateState<EnemyChaseState>();
            return;
        }

        // 리사주 곡선을 이용한 무한대 패턴
        PatrolAngle += Runner.DeltaTime * patrolSpeed;
        float xOffset = Mathf.Sin(PatrolAngle) * patrolRadius;
        float yOffset = Mathf.Sin(2 * PatrolAngle) * patrolRadius / 2; // y축 움직임은 x축의 절반으로

        Vector2 nextPosition = PatrolAnchorPosition + new Vector2(xOffset, yOffset);
        nrb.Rigidbody.MovePosition(nextPosition);

        // 이동 방향에 따라 뒤집기
        if (nrb.Rigidbody.linearVelocity.x > 0.01f && !IsFacingRight) Flip();
        else if (nrb.Rigidbody.linearVelocity.x < -0.01f && IsFacingRight) Flip();
    }

    protected override void UpdateChaseState()
    {
        // 플레이어와 동일 정책: 조작이 불가한 4상태에서는 인위적 속도 제어 차단
        if (IsDead || IsStunned || IsHeld || IsThrown)
        {
            return;
        }
        // 타겟이 사라지면 Idle 상태로 전환
        if (TargetPlayer == null)
        {
            PatrolAnchorPosition = transform.position;
            PatrolAngle = 0f;
            IsFacingRight = true;
            CurrentState = EnemyStateName.Idle;
            fsm.StateMachine.ForceActivateState<EnemyIdleState>();
            return;
        }

        // 공격 상태가 없으므로, 공격 범위 체크 로직은 제거합니다.

        // 타겟을 향한 방향 벡터 계산
        Vector2 directionToTarget = (TargetPlayer.transform.position - transform.position).normalized;

        // 해당 방향으로 이동
        nrb.Rigidbody.linearVelocity = directionToTarget * enemyData.moveSpeed * 2;

        // 타겟 방향으로 몸을 돌림
        if (nrb.Rigidbody.linearVelocity.x > 0.01f && !IsFacingRight) Flip();
        else if (nrb.Rigidbody.linearVelocity.x < -0.01f && IsFacingRight) Flip();
    }

    protected override void UpdateHitReactState()
    {
        // 플레이어와 동일 정책: 조작이 불가한 4상태에서는 인위적 속도 제어 차단
        if (IsDead || IsStunned || IsHeld || IsThrown)
        {
            return;
        }
        nrb.Rigidbody.linearVelocity = new Vector2(0, 0);

        if (StateTimer.ExpiredOrNotRunning(Runner) && StunTimer.ExpiredOrNotRunning(Runner))
        {
            CurrentState = EnemyStateName.Idle;
            fsm.StateMachine.ForceActivateState<EnemyIdleState>();
        }
    }
    
    protected override void UpdateDeadState()
    {
        nrb.Rigidbody.linearVelocity = new Vector2(0, 0);
        //coll.enabled = false; //다른 오브젝트와 충돌하지 않도록 비활성화
    }
}
