using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.FSM;
using Fusion.Addons.Physics;
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
//실제 행동 및 상태전환은 EnemyBase(FUN)에서 처리하고, 렌더링 관련만 FSM에서 처리
public class EnemyBase : NetworkBehaviour, IPlayerInteraction
{
    [Networked] public SpelunkyPlayerController TargetPlayer { get; set; }

    // 벽, 절벽, 공격 판정 체크 위치
    [SerializeField] private Transform groundCheck; // 절벽 감지를 위한 위치
    [SerializeField] private Transform wallCheck; // 벽 감지를 위한 위치
    [SerializeField] private Transform attackCheck; // 공격 판정 위치
    [SerializeField] private float wallCheckDistance = 0.5f; // 벽 감지 거리
    [SerializeField] private float groundCheckDistance = 0.2f; // 바닥 감지 거리
    [SerializeField] private float attackCheckRadius = 0.5f; // 공격 판정 반지름

    //컴포넌트들
    public EnemyData enemyData; //ScriptableObject를 사용, 드래그앤드롭으로 적 기본 스탯 설정
    public EnemyFSM fsm; //시각적 상태를 제어하는 fsm
    protected Collider2D coll;
    protected NetworkRigidbody2D nrb;

    // 타이머들 
    [Networked] public TickTimer StateTimer { get; set; } //상태 시간(랜덤)을 저장할 타이머
    [Networked] public TickTimer AttackCooldownTimer { get; set; } // 공격 쿨타임을 위한 타이머
    [Networked] private TickTimer FlipTimer { get; set; } // 빠르게 플립되는 현상을 방지하기 위한 타이머
    [Networked] private TickTimer InvincibleTimer { get; set; }
    [Networked] private TickTimer ThrownTimer { get; set; }
    // 상태 관련 네트워크 프로퍼티들
    [Networked] public int CurrentHealth { get; private set; } //몬스터 Hp의 변경이 감지되면 OnHpChanged 호출, 현재 hp
    [Networked] public EnemyStateName CurrentState { get; set; } //현재 스테이트 (EnemyFsm과 동기화)
    [Networked, OnChangedRender(nameof(OnDirectionChanged))] private NetworkBool IsFacingRight { get; set; } //몬스터가 바라보는 방향
    [Networked] protected bool IsDead { get; set; }
    [Networked] public bool IsStunned { get; private set; }
    [Networked] public bool IsInvincible { get; private set; }
    [Networked] public bool IsHeld { get; private set; } // 들림 상태 추가
    [Networked] public bool IsThrown { get; private set; } // 던진 상태 추가

    public override void Spawned() //네트워크 객체가 생성될 때 호출
    {
        UnityEngine.Debug.Log($"{enemyData.enemyName} Monster Spawned");
        coll = GetComponent<Collider2D>();
        nrb = GetComponent<NetworkRigidbody2D>();
        IsDead = false;
        IsStunned = false;
        IsInvincible = false;

        if (Object.HasStateAuthority) //호스트(서버)에서 초기스탯 설정
        {
            CurrentHealth = enemyData.maxHp;
            IsFacingRight = true;
            CurrentState = EnemyStateName.Idle;
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return; //호스트가 아니면, 실행 X

        if (IsDead)
        {
            UpdateDeadState();
            return;
        }

        // 무적 타이머 만료 시 무적 해제
        if (IsInvincible && InvincibleTimer.Expired(Runner))
        {
            IsInvincible = false;
        }

        // 던진 타이머 만료 시 던진 상태 해제
        if (IsThrown && ThrownTimer.Expired(Runner))
        {
            IsThrown = false;
        }

        UpdateTarget();

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

    protected virtual void UpdateIdleState()
    {
        nrb.Rigidbody.linearVelocity = new Vector2(0, nrb.Rigidbody.linearVelocity.y);

        //타겟이 감지되면 Chase 상태로 전환
        if (TargetPlayer != null)
        {
            CurrentState = EnemyStateName.Chase; //행동은 EnemyNetwworkBehaviour의 FUN 에서 처리
            fsm.StateMachine.ForceActivateState<EnemyChaseState>(); //렌더링은 FSM에서 처리
            return; //상태가 변경되었으므로 즉시 함수 종료
        }
        if (StateTimer.ExpiredOrNotRunning(Runner))
        {
            CurrentState = EnemyStateName.Move;
            fsm.StateMachine.ForceActivateState<EnemyMoveState>();
        }
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

        if (StateTimer.ExpiredOrNotRunning(Runner))
        {
            CurrentState = EnemyStateName.Idle;
            fsm.StateMachine.ForceActivateState<EnemyIdleState>();
        }

        // 정찰 로직
        float moveDirection = IsFacingRight ? 1f : -1f;
        nrb.Rigidbody.linearVelocity = new Vector2(moveDirection * enemyData.moveSpeed, nrb.Rigidbody.linearVelocity.y);

        //벽 또는 절벽 감지 시 방향 전환
        if ((IsDetectingWall() || !IsDetectingGround()) && FlipTimer.ExpiredOrNotRunning(Runner))
        {
            Flip();
        }

    }
    protected virtual void UpdateChaseState()
    {
        //타겟이 벗어났다면
        if (TargetPlayer == null)
        {
            CurrentState = EnemyStateName.Idle;
            fsm.StateMachine.ForceActivateState<EnemyIdleState>();
            return;
        }

        //타겟이 공격사정거리 안에 들어왔다면
        if (Vector2.Distance(transform.position, TargetPlayer.transform.position) < enemyData.attackRange
            && AttackCooldownTimer.ExpiredOrNotRunning(Runner))
        {
            CurrentState = EnemyStateName.Attack;
            fsm.StateMachine.ForceActivateState<EnemyAttackState>();
        }

        // 타겟 방향으로 이동
        float directionToTarget = TargetPlayer.transform.position.x - transform.position.x;

        // 타겟 방향으로 몸을 돌림
        if (((directionToTarget >= 0 && !IsFacingRight) || (directionToTarget < 0 && IsFacingRight)) && FlipTimer.ExpiredOrNotRunning(Runner))
        {
            Flip();
        }

        nrb.Rigidbody.linearVelocity = new Vector2(Mathf.Sign(directionToTarget) * enemyData.moveSpeed, nrb.Rigidbody.linearVelocity.y);
    }

    protected virtual void UpdateAttackState()
    {
        nrb.Rigidbody.linearVelocity = new Vector2(0, nrb.Rigidbody.linearVelocity.y);

        if (StateTimer.ExpiredOrNotRunning(Runner))
        {
            CurrentState = EnemyStateName.Chase;
            fsm.StateMachine.ForceActivateState<EnemyChaseState>();
        }
    }
    protected virtual void UpdateHitReactState()
    {
        nrb.Rigidbody.linearVelocity = new Vector2(0, nrb.Rigidbody.linearVelocity.y);

        if (StateTimer.ExpiredOrNotRunning(Runner))
        {
            CurrentState = EnemyStateName.Move;
            fsm.StateMachine.ForceActivateState<EnemyMoveState>();
        }
    }
    protected virtual void UpdateDeadState()
    {
        nrb.Rigidbody.linearVelocity = new Vector2(0, nrb.Rigidbody.linearVelocity.y);
        //coll.enabled = false; //다른 오브젝트와 충돌하지 않도록 비활성화
    }

    protected virtual void UpdateStunState()
    {
        nrb.Rigidbody.linearVelocity = new Vector2(0, nrb.Rigidbody.linearVelocity.y);

        if (IsStunned && StateTimer.ExpiredOrNotRunning(Runner))
        {
            IsStunned = false;
            //상태 전환
            CurrentState = EnemyStateName.Chase;
            fsm.StateMachine.ForceActivateState<EnemyChaseState>();
        }
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
        FlipTimer = TickTimer.CreateFromSeconds(Runner, 0.5f); //빠른 플립을 방지하기 위한 플립타이머 설정
        IsFacingRight = !IsFacingRight;
    }

    public void OnDirectionChanged()
    {
        // IsFacingRight 값에 따라 로컬 스케일을 조정하여 뒤집습니다.
        // 이 코드는 모든 클라이언트에서 실행되므로 화면에 올바르게 렌더링됩니다.
        if (IsFacingRight)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z); ; // 오른쪽을 볼 때 (기본값)
        }
        else
        {
            transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z); ; // 왼쪽을 볼 때
        }
    }

    //매 틱마다 주변에 플레이어가 있는지 탐색
    private void UpdateTarget()
    {
        Collider2D[] hitColliders = new Collider2D[5];
        int hitCounts = Runner.GetPhysicsScene2D().OverlapCircle(transform.position, enemyData.searchDistance, hitColliders, enemyData.PlayerLayer);

        SpelunkyPlayerController closestPlayer = null;
        float closestDistanceSqr = float.MaxValue;
        if (hitCounts > 0)
        {
            // 감지된 모든 플레이어에 대해 반복
            for (int i = 0; i < hitCounts; i++)
            {
                SpelunkyPlayerController player = hitColliders[i].GetComponent<SpelunkyPlayerController>();
                if (player != null)
                {
                    // 몬스터와 플레이어 사이의 거리 제곱을 계산
                    float distanceSqr = (player.transform.position - transform.position).sqrMagnitude;

                    // 더 가까운 플레이어를 찾으면, closestPlayer를 업데이트
                    if (distanceSqr < closestDistanceSqr)
                    {
                        closestDistanceSqr = distanceSqr;
                        closestPlayer = player;
                    }
                }
            }
        }

        // 가장 가까운 플레이어를 최종 타겟으로 설정합니다.
        TargetPlayer = closestPlayer;
    }
    public virtual void DealDamage()
    {
        // 서버(제어 권한자)가 아니면 로직을 실행하지 않습니다.
        if (!Object.HasStateAuthority) return;

        // 공격 지점(attackPoint)을 중심으로 attackRadius 반경 내의 모든 콜라이더를 감지합니다.
        // 이때 PlayerLayer에 속한 콜라이더만 감지 대상으로 합니다.
        List<LagCompensatedHit> hits = new List<LagCompensatedHit>(); // 최대 5개까지 감지
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
        Gizmos.color = Color.red;
        Gizmos.DrawLine(groundCheck.position, new Vector3(groundCheck.position.x, groundCheck.position.y - groundCheckDistance));
        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance, wallCheck.position.y));
        if (attackCheck == null) return;
        Gizmos.DrawWireSphere(attackCheck.position, attackCheckRadius);
    }


    public virtual void ApplyKnockback(Vector2 force, float stunDuration = 0)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;

        // 무적 상태에서는 넉백 불가
        if (IsInvincible == true) return;

        // 기본 스턴 지속시간 0.5초로 설정 (stunDuration이 0이면)
        if (stunDuration <= 0f) stunDuration = 0.5f;

        if (nrb != null)
        {
            nrb.Rigidbody.AddForce(force, ForceMode2D.Impulse);
        }
        else
        {
            Debug.LogWarning($"[{name}] Rigidbody2D 컴포넌트를 찾을 수 없어 넉백을 적용할 수 없습니다!");
        }

        ApplyStun(stunDuration);
    }

    //데미지를 받는 함수
    // [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)] //서버에서만 이 함수를 호출할 수 있고, 서버에서만 이 함수가 실행되어야함
    public virtual void TakeDamage(int damage) //데미지를 받는 함수
    {
        //죽었다면 데미지 못받게 return
        if (IsDead) return;

        //무적상태라면 데미지 못받게 return
        if (IsInvincible) return;

        CurrentHealth -= damage;
        SetInvincible(true, 0.2f);
        UnityEngine.Debug.Log($"몬스터 체력 : {CurrentHealth}");

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            IsDead = true;
            CurrentState = EnemyStateName.Dead;
            fsm.StateMachine.ForceActivateState<EnemyDeadState>();
        }
        else
        {
            CurrentState = EnemyStateName.HitReact;
            fsm.StateMachine.ForceActivateState<EnemyHitReactState>();
        }
    }

    public virtual void ApplyStun(float duration)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;

        // 무적 상태에서는 스턴 불가
        if (IsInvincible == true) return;

        // 사망 상태에서는 스턴 불가
        if (IsDead) return;

        IsStunned = true;
        StateTimer = TickTimer.CreateFromSeconds(Runner, duration);

        //StunState로 전환
        CurrentState = EnemyStateName.Stun;
        fsm.StateMachine.ForceActivateState<EnemyStunState>();
    }

    public virtual void SetInvincible(bool value, float duration = 0)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;

        // 사망 상태에서는 무적 설정 불가
        if (IsDead) return;

        IsInvincible = value;

        if (value && duration > 0f)
        {
            InvincibleTimer = TickTimer.CreateFromSeconds(Runner, duration);
        }
        else if (!value)
        {
            InvincibleTimer = TickTimer.None;
        }
    }
    public virtual void OnPickedUp()
    {
        if (!HasStateAuthority) return;
        IsHeld = true;
    }

    public virtual void OnReleased()
    {
        if (!HasStateAuthority) return;
        IsHeld = false;
    }
    
    // 🚀 던진 상태 설정 (duration초 동안)
    public virtual void SetThrown(float duration = 1.5f)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 사망 상태에서는 던진 상태 설정 불가
        if (IsDead) return;
        
        // 무적 상태에서는 던진 상태 설정 불가
        if (IsInvincible) return;
        
        IsThrown = true;
        ThrownTimer = TickTimer.CreateFromSeconds(Runner, duration);
    }
}    
