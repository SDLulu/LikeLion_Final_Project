using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Fusion;
using Fusion.Addons.FSM;
using Fusion.Addons.Physics;
using Unity.VisualScripting;
using UnityEditor.Tilemaps;
using UnityEngine;

public enum EnemyStateName
{
    Idle,
    Move,
    Attack,
    Chase,
    HitReact,
    Stun,
    Dead
}
public class EnemyBase : NetworkBehaviour //실제 행동은 EnemyBase(FUN)에서 처리하고, 렌더링 관련만 FSM에서 처리
{
    [Networked] public float CurrentHealth { get; private set; } //몬스터 Hp의 변경이 감지되면 OnHpChanged 호출, 현재 hp
    [Networked] public SpelunkyPlayerController TargetPlayer { get; set; }
    [Networked] public EnemyStateName CurrentState { get; set; } //현재 스테이트 (EnemyFsm과 동기화)
    // [Networked] public TickTimer StateTimer { get; set; } //상태 시간(랜덤)을 저장할 타이머
    [Networked] public TickTimer AttackCooldownTimer { get; set; } // 공격 쿨타임을 위한 타이머
    [Networked, OnChangedRender(nameof(OnDirectionChanged))] private NetworkBool IsFacingRight { get; set; } //몬스터가 바라보는 방향
    public EnemyData enemyData; //ScriptableObject를 사용, 드래그앤드롭으로 적 기본 스탯 설정
    public EnemyFSM fsm; //시각적 상태를 제어하는 fsm
    [SerializeField] private Transform groundCheck; // 절벽 감지를 위한 위치
    [SerializeField] private Transform wallCheck; // 벽 감지를 위한 위치
    [SerializeField] private Transform attackCheck; // 공격 감지를 위한 위치
    [SerializeField] private float wallCheckDistance = 0.5f; // 벽 감지 거리
    [SerializeField] private float groundCheckDistance = 0.2f; // 바닥 감지 거리
    [SerializeField] private float attackCheckRadius = 0.5f; // 공격 감지 반지름

    //컴포넌트들
    protected Collider2D coll;
    protected NetworkRigidbody2D nrb;
    [Networked] protected bool IsAlive { get; set; }

    public override void Spawned() //네트워크 객체가 생성될 때 호출
    {
        UnityEngine.Debug.Log($"{enemyData.enemyName} Monster Spawned");
        coll = GetComponent<Collider2D>();
        nrb = GetComponent<NetworkRigidbody2D>();
        IsAlive = true;

        if (Object.HasStateAuthority) //호스트(서버)에서 초기스탯 설정
        {
            CurrentHealth = enemyData.maxHp;
            IsFacingRight = true;
            CurrentState = EnemyStateName.Idle;
            fsm.StateMachine.ForceActivateState<EnemyIdleState>();
        }
    }

    public override void Render()
    {
        
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return; //호스트가 아니면, 실행 X

        if (!IsAlive)
        {
            UpdateDeadState();
            return;
        }


        if (TargetPlayer == null)
        {
            CheckForPlayer();
        }
        switch (CurrentState)
        {
            case EnemyStateName.Idle:
                UpdateIdleState();
                break;

            case EnemyStateName.Move:
                UpdateMoveState();
                break;

            case EnemyStateName.Attack:
                UpdateAttackState();
                break;

            case EnemyStateName.Chase:
                UpdateChaseState();
                break;

            case EnemyStateName.HitReact:
                UpdateHitReactState();
                break;

            case EnemyStateName.Stun:
                UpdateStunState();
                break;

            case EnemyStateName.Dead:
                UpdateDeadState();
                break;
        }
    }

    //데미지를 받는 함수
    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)] //서버에서만 이 함수를 호출할 수 있고, 서버에서만 이 함수가 실행되어야함
    public void Rpc_TakeDamage(float damage) //데미지를 받는 함수
    {
        //죽었다면 데미지 못받게 return
        if (!IsAlive) return;


        CurrentHealth -= damage;
        UnityEngine.Debug.Log($"몬스터 체력 : {CurrentHealth}");

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            IsAlive = false;
            CurrentState = EnemyStateName.Dead;
            fsm.StateMachine.ForceActivateState<EnemyDeadState>();
        }
        else
        {
            CurrentState = EnemyStateName.HitReact;
            fsm.StateMachine.ForceActivateState<EnemyHitReactState>();
        }
    }

    protected virtual void UpdateIdleState()
    {
        nrb.Rigidbody.linearVelocity = new Vector2(0, nrb.Rigidbody.linearVelocity.y);
    }
    protected virtual void UpdateMoveState()
    {
        // 플레이어가 감지 범위 안에 들어오면 Chase 상태로 변경
        if (TargetPlayer != null)
        {
            CurrentState = EnemyStateName.Chase;
            fsm.StateMachine.ForceActivateState<EnemyChaseState>();
            return;
        }

        // 정찰 로직
        float moveDirection = IsFacingRight ? 1f : -1f;
        nrb.Rigidbody.linearVelocity = new Vector2(moveDirection * enemyData.moveSpeed, nrb.Rigidbody.linearVelocity.y);

        //벽 또는 절벽 감지 시 방향 전환
        if (IsDetectingWall() || !IsDetectingGround())
        {
            Flip();
        }

    }
    protected virtual void UpdateChaseState()
    {
        if (TargetPlayer == null)
        {
            CurrentState = EnemyStateName.Idle;
            fsm.StateMachine.ForceActivateState<EnemyIdleState>();
            return;
        }


        // 타겟 방향으로 이동
        float directionToTarget = TargetPlayer.transform.position.x - transform.position.x;

        // 타겟 방향으로 몸을 돌림
        if ((directionToTarget > 0 && !IsFacingRight) || (directionToTarget < 0 && IsFacingRight))
        {
            Flip();
        }

        nrb.Rigidbody.linearVelocity = new Vector2(Mathf.Sign(directionToTarget) * enemyData.moveSpeed, nrb.Rigidbody.linearVelocity.y);
    }

    protected virtual void UpdateAttackState()
    {
        nrb.Rigidbody.linearVelocity = new Vector2(0, 0);
    }
    protected virtual void UpdateHitReactState()
    {
        nrb.Rigidbody.linearVelocity = new Vector2(0, nrb.Rigidbody.linearVelocity.y);
    }
    protected virtual void UpdateStunState()
    {
    }
    protected virtual void UpdateDeadState()
    {
        nrb.Rigidbody.linearVelocity = new Vector2(0, nrb.Rigidbody.linearVelocity.y);
        //coll.enabled = false; //다른 오브젝트와 충돌하지 않도록 비활성화
    }

    private bool IsDetectingWall()
    {
        Vector2 direction = IsFacingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Runner.GetPhysicsScene2D().Raycast(wallCheck.position, direction, wallCheckDistance, enemyData.PlatformLayer);
        return hit.collider != null;
    }

    private bool IsDetectingGround()
    {
        RaycastHit2D hit = Runner.GetPhysicsScene2D().Raycast(groundCheck.position, Vector2.down, groundCheckDistance, enemyData.PlatformLayer);
        return hit.collider != null;
    }

    protected void Flip()
    {
        IsFacingRight = !IsFacingRight;
    }

    public void OnDirectionChanged()
    {
        // IsFacingRight 값에 따라 로컬 스케일을 조정하여 뒤집습니다.
        // 이 코드는 모든 클라이언트에서 실행되므로 화면에 올바르게 렌더링됩니다.
        if (IsFacingRight)
        {
           transform.localScale =  new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);; // 오른쪽을 볼 때 (기본값)
        }
        else
        {
            transform.localScale =  new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);; // 왼쪽을 볼 때
        }
    }

    //매 틱마다 주변에 플레이어가 있는지 탐색
    private void CheckForPlayer()
    {
        Collider2D hitCollider = Runner.GetPhysicsScene2D().OverlapCircle(transform.position, enemyData.searchDistance, enemyData.PlayerLayer);

        if (hitCollider != null)
        {
            TargetPlayer = hitCollider.gameObject.GetComponent<SpelunkyPlayerController>();
        }
        else
        {
            TargetPlayer = null;
        }
    }
    public void DealDamage()
    {
        // 서버(제어 권한자)가 아니면 로직을 실행하지 않습니다.
        if (!Object.HasStateAuthority) return;

        // 공격 지점(attackPoint)을 중심으로 attackRadius 반경 내의 모든 콜라이더를 감지합니다.
        // 이때 PlayerLayer에 속한 콜라이더만 감지 대상으로 합니다.
         List<LagCompensatedHit> hits = new  List<LagCompensatedHit>(); // 최대 5개까지 감지
        int hitCount = Runner.LagCompensation.OverlapSphere(
            attackCheck.position, 
            attackCheckRadius, 
            Object.InputAuthority, 
            hits, 
            enemyData.PlayerHitBoxLayer
        );

        if (hitCount > 0)
        {
            UnityEngine.Debug.Log($"{hitCount}명의 플레이어를 공격했습니다!");
            for (int i = 0; i < hitCount; i++)
            {
                // 감지된 콜라이더에서 PlayerController 컴포넌트를 가져옵니다.
                PlayerInteractionBase player = hits[i].GameObject.GetComponentInParent<PlayerInteractionBase>();
                if (player != null) // 플레이어가 존재하고 살아있다면
                {
                    UnityEngine.Debug.Log($"플레이어에게 데미지!");
                    // 플레이어의 Rpc_TakeDamage 함수를 호출하여 데미지를 줍니다.
                    // 이 RPC는 플레이어의 스크립트에 구현되어 있어야 합니다.
                    player.TakeDamage((int)enemyData.attackDamage);
                }
            }
        }
    }

    protected virtual void OnDrawGizmos()
    {
        if (attackCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(groundCheck.position, new Vector3(groundCheck.position.x, groundCheck.position.y - groundCheckDistance));
        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance, wallCheck.position.y));
        Gizmos.DrawWireSphere(attackCheck.position, attackCheckRadius);
    }
}    
