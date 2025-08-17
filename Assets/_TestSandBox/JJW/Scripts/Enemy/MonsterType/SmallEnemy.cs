using UnityEngine;
using Fusion;
using Fusion.Addons.FSM;
using Fusion.Addons.Physics;

public class SmallEnemy : EnemyBase
{
    protected override void UpdateChaseState()
    {
        //타겟이 벗어났다면
        if (TargetPlayer == null)
        {
            CurrentState = EnemyStateName.Idle;
            fsm.StateMachine.ForceActivateState<EnemyIdleState>();
            return;
        }

        // 타겟 방향으로 이동
        float directionToTarget = TargetPlayer.transform.position.x - transform.position.x;

        // 타겟 방향으로 몸을 돌림
        if (((directionToTarget >= 0 && !IsFacingRight) || (directionToTarget < 0 && IsFacingRight)) && FlipTimer.ExpiredOrNotRunning(Runner))
        {
            Flip();
        }

        SetVelocityX(Mathf.Sign(directionToTarget) * enemyData.moveSpeed * 2);
    }

    protected override void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(groundCheck.position, new Vector3(groundCheck.position.x, groundCheck.position.y - groundCheckDistance));
        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance, wallCheck.position.y));
        //Gizmos.color = Color.green;
        //Gizmos.DrawWireSphere(this.transform.position, enemyData.searchDistance); //탐색거리
        if (attackCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackCheck.position, attackCheckRadius);

        Gizmos.color = Color.cyan; // 눈에 잘 띄는 색으로 변경
        Vector3 boxCenter2 = transform.position + (Vector3)detectionBoxOffset;
        Gizmos.DrawWireCube(boxCenter2, detectionBoxSize);
    }
}
