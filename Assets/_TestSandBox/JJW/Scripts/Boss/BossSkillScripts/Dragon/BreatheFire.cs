using UnityEngine;
using Fusion;
public class BreatheFire : BossSkillAState, IAnimationTriggerReceiver
{
    private enum SubState
    {
        Jumping, // 점프 애니메이션 실행 중
        Attacking,
        Returning, // 원래 위치로 복귀 중
    }

    [Header("Animation Names")]
    [SerializeField] private string jumpAnimStateName = "Dragon Jump";
    [SerializeField] private string attackAnimStateName = "Dragon Attack";
    [SerializeField] private string flyingAnimStateName = "Dragon Flying";

    [Header("Movement")]
    [SerializeField] private Transform topBoundary; // 위쪽 비행 경계
    [SerializeField] private Transform bottomBoundary; // 아래쪽 비행 경계
    [SerializeField] private float flyingSpeed = 8f;


    //드래곤이 날아다니면서 불을 뿜는 스킬 페이즈의 스크립트
    [Header("Skill Settings")]
    [SerializeField] private int numberOfBursts = 3; // 총 몇 번 발사할 것인가
    [SerializeField] private int numberOfProjectiles = 6; //발사체 개수
    [SerializeField] private float totalAngle = 30f; // 발사체 사이의 각도

    [Header("Projectile")]
    [SerializeField] private NetworkPrefabRef FirePrefab = NetworkPrefabRef.Empty;    //불 프리팹
    [SerializeField] private Transform firePointPos;

    // --- Networked State ---
    // 이 상태의 내부 진행 상황을 동기화하기 위한 네트워크 변수들입니다.
    [Networked] private SubState currentSubState { get; set; } // 현재 하위 상태
    [Networked] private Vector2 startPosition { get; set; } // 스킬 시작 위치 (복귀 지점)
    [Networked] private Vector2 currentTargetPosition { get; set; } // 현재 목표 지점
    [Networked] private int burstsFired { get; set; }
    protected override void OnEnterState()
    {
        UnityEngine.Debug.Log("보스 몬스터가 Breathe Fire 상태에 진입했습니다!!!!!!!!!!!!!!!!!!!!");

        if (Object.HasStateAuthority)
        {
            startPosition = transform.position;
            currentSubState = SubState.Jumping; // 첫 단계는 'Jumping'
            burstsFired = 0;
            UnityEngine.Debug.Log("현재 보스몬스터 위치 : " + startPosition.ToString() );
        }
    }

    protected override void OnEnterStateRender()
    {
        anim.CrossFadeInFixedTime(jumpAnimStateName, animTransitionLength);
    }

     // 물리 프레임마다 지속적으로 실행
    protected override void OnFixedUpdate()
    {
        if (!Object.HasStateAuthority) return;

        // 하위 상태에 따라 지속적인 행동(주로 이동)을 처리
        switch (currentSubState)
        {
            case SubState.Attacking:
                HandleFlyingMovement();
                break;

            case SubState.Returning:
                HandleReturnMovement();
                break;
        }
    }

    // --- 로직 헬퍼 메서드 ---

    private void HandleFlyingMovement()
    {
        Vector2 direction = (currentTargetPosition - (Vector2)transform.position).normalized;
        boss.nrb.Rigidbody.linearVelocity = direction * flyingSpeed;

        if (Vector2.Distance(transform.position, currentTargetPosition) < 0.5f)
        {
            currentTargetPosition = (currentTargetPosition.y == topBoundary.position.y)
                ? bottomBoundary.position : topBoundary.position;
        }
    }

    private void HandleReturnMovement()
    {
        Vector2 direction = (startPosition - (Vector2)transform.position).normalized;
        boss.nrb.Rigidbody.linearVelocity = direction * flyingSpeed;

        if (Vector2.Distance(transform.position, startPosition) < 0.5f)
        {
            boss.nrb.transform.position = startPosition; // 시작 지점으로 이동
            boss.nrb.Rigidbody.linearVelocity = Vector2.zero;
            fsmRef.BossNetworkBehaviour.CurrentState = BossStateName.Idle;
        }
    }
    

    private void FireOneBurst()
    {

        float angleStep = totalAngle / (numberOfProjectiles - 1);
        float startAngle = -totalAngle / 2f;

        // 보스가 바라보는 방향을 기준으로 발사 각도를 계산합니다.
        Vector2 baseDirection = boss.transform.right;
        if (boss.transform.localScale.x < 0) // 왼쪽을 보고 있다면 방향 뒤집기
        {
            baseDirection = -boss.transform.right;
        }

        for (int i = 0; i < numberOfProjectiles; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = Quaternion.Euler(0, 0, currentAngle);
            Vector3 fireDirection = rotation * baseDirection;
            Quaternion finalRotation = Quaternion.LookRotation(Vector3.forward, fireDirection);

            // 네트워크 객체 생성
            Runner.Spawn(FirePrefab, firePointPos.position, finalRotation, Object.InputAuthority);
        }
    }
    
    public void OnAnimationEvent(string eventName)
    {
        if (!Object.HasStateAuthority) return;

        switch (eventName)
        {
            case "JumpEnd":
                currentSubState = SubState.Attacking;
                currentTargetPosition = topBoundary.position;
                // 공격 애니메이션으로 전환
                anim.CrossFadeInFixedTime(attackAnimStateName, animTransitionLength);
                break;

            case "Fire": // 발사 프레임일 때
                if (burstsFired < numberOfBursts)
                {
                    FireOneBurst();
                }
                break;
            case "CycleEnd": // 애니메이션 한 사이클이 끝났을 때
                burstsFired++;
                if (burstsFired >= numberOfBursts)
                {
                    currentSubState = SubState.Returning;
                    currentTargetPosition = startPosition;
                    // 복귀 애니메이션으로 전환
                    anim.CrossFadeInFixedTime(flyingAnimStateName, animTransitionLength);
                }
                break;
        }
    }
}
