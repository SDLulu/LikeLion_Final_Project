using System.Collections.Generic;
using Fusion;
using UnityEngine;
public class SuperJump : BossSkillAState
{
     // 스킬의 세부 단계를 관리하기 위한 하위 상태
    private enum SubState
    {
        JumpingUp, // 위로 상승 중
        FallingDown // 아래로 하강 중
    }

    [Header("Animation Names")]
    [SerializeField] private string superJumpAnimName = "Dragon SuperJump";
    [SerializeField] private string landingAnimName = "Dragon Landing";

    [Header("Skill Settings")]
    [SerializeField] private float jumpHeight = 10f; // 상승할 높이 (n)
    [SerializeField] private float jumpSpeed = 12f; // 상승 및 하강 속도
    [SerializeField] private NetworkPrefabRef landingEffectPrefab; // 착지 시 생성될 이펙트 프리팹
    [SerializeField] private Transform EffectPos;

    [Header("Damage Settings")]
    [SerializeField] private BoxCollider2D damageArea;
    [SerializeField] private int damageAmount = 1; // 피해량

    // --- 네트워크 동기화 변수 ---
    [Networked] private SubState currentSubState { get; set; }
    [Networked] private Vector2 startPosition { get; set; }
    [Networked] private Vector2 peakPosition { get; set; }

    // 상태에 처음 진입
    protected override void OnEnterState()
    {
        // 이 스킬은 stateDuration을 사용하지 않으므로 base.OnEnterState()를 호출하지 않습니다.
        if (Object.HasStateAuthority)
        {
            startPosition = transform.position;
            peakPosition = new Vector2(startPosition.x, startPosition.y + jumpHeight);
            currentSubState = SubState.JumpingUp;
        }
    }

    // 렌더링 측면에서 첫 애니메이션 실행
    protected override void OnEnterStateRender()
    {
        anim.CrossFadeInFixedTime(superJumpAnimName, animTransitionLength);
    }

    // 물리 프레임마다 호출
    protected override void OnFixedUpdate()
    {
        if (!Object.HasStateAuthority) return;

        switch (currentSubState)
        {
            case SubState.JumpingUp:
                HandleJumpingUpMovement();
                break;
            case SubState.FallingDown:
                HandleFallingDownMovement();
                break;
        }
    }

    // --- 로직 헬퍼 메서드 ---

    private void HandleJumpingUpMovement()
    {
        // 최고점을 향해 위로 이동
        Vector2 direction = (peakPosition - (Vector2)transform.position).normalized;
        boss.nrb.Rigidbody.linearVelocity = direction * jumpSpeed;

        // 최고점에 도달했다면, 하강 상태로 전환
        if (Vector2.Distance(transform.position, peakPosition) < 0.5f)
        {
            currentSubState = SubState.FallingDown;
            anim.CrossFadeInFixedTime(landingAnimName, animTransitionLength);
        }
    }

    private void HandleFallingDownMovement()
    {
        // 시작 위치를 향해 아래로 이동
        Vector2 direction = (startPosition - (Vector2)transform.position).normalized;
        boss.nrb.Rigidbody.linearVelocity = direction * jumpSpeed;

        // 시작 위치에 도달했다면, 착지 처리
        if (Vector2.Distance(transform.position, startPosition) < 0.5f)
        {
            HandleLanding();
        }
    }

    private void HandleLanding()
    {
        boss.nrb.Rigidbody.linearVelocity = Vector2.zero; // 움직임 정지
        boss.nrb.transform.position = startPosition; // 시작 위치로 이동

        DealLagCompensatedDamage();
        // 착지 이펙트 생성 (모든 클라이언트에서 보이도록)
        if (landingEffectPrefab.IsValid)
        {
            Runner.Spawn(landingEffectPrefab, EffectPos.position, Quaternion.identity);
        }
        // Idle 상태로 최종 전환
        fsmRef.BossNetworkBehaviour.CurrentState = BossStateName.Idle;
        //fsmRef.StateMachine.ForceActivateState<BossIdleState>();
    }
     private void DealLagCompensatedDamage()
    {
        // LagCompensation.OverlapBox를 사용하여 과거 시점의 플레이어를 찾습니다.
        // 이는 Runner.Tick을 기준으로 하므로, 현재 프레임의 물리 상태가 아닌
        // 클라이언트들이 보았던 과거의 정확한 상태에서 판정을 수행합니다.
        if (damageArea == null)
        {
            Debug.LogError("Damage Area (BoxCollider2D)가 할당되지 않았습니다!");
            return;
        }
        // BoxCollider2D의 월드 좌표와 크기를 가져와 사용합니다.
        Vector2 boxCenter = (Vector2)damageArea.transform.position + damageArea.offset;
        Vector2 boxSize = damageArea.size;
        
        List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

        int hitCount = Runner.LagCompensation.OverlapBox(
            boxCenter,
            boxSize,
            damageArea.transform.rotation,        // 회전 없음
            Object.InputAuthority,      // 이 공격을 실행한 플레이어 (여기서는 보스)
            hits,                       // 결과를 저장할 배열
            boss.bossData.PlayerHitBoxLayer   // 감지할 레이어
        );
        UnityEngine.Debug.Log($"{hitCount}명의 플레이어를 탐지했습니다!");
        
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
                    player.TakeDamage(damageAmount);
                }
            }
        }
    }
}
