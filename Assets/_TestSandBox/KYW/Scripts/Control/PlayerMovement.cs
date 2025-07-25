using Fusion;
using UnityEngine;

// 🏃 플레이어 이동 컴포넌트
// 좌우 이동, 덕킹 담당 (순수 로직만)
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float duckMoveSpeed = 2.5f;
    [Networked] public float NormalizedSpeed { get; private set; }
    
    // 🌐 네트워크 동기화 상태
    [Networked] public bool IsDucking { get; private set; }
    [Networked] public bool IsFacingLeft { get; private set; }
    
    // 참조 컴포넌트들
    private PlayerGroundCheck groundCheck;
    private Rigidbody2D rb; // ⚠️ 순간이동 문제의 핵심 원인! 일반 Rigidbody2D 사용 중
    private PlayerClimbing climbing;
    private SpelunkyPlayerController playerController;

    public override void Spawned()
    {
        // 모든 컴포넌트 참조를 한 번에 설정
        rb = GetComponent<Rigidbody2D>();
        groundCheck = GetComponentInChildren<PlayerGroundCheck>();
        climbing = GetComponent<PlayerClimbing>();
        playerController = GetComponent<SpelunkyPlayerController>();
        
        // 필수 컴포넌트 검증
        if (rb == null)
            Debug.LogError($"[{name}] Rigidbody2D 컴포넌트를 찾을 수 없습니다!");
        if (groundCheck == null)
            Debug.LogError($"[{name}] PlayerGroundCheck 컴포넌트를 찾을 수 없습니다!");
        if (climbing == null)
            Debug.LogError($"[{name}] PlayerClimbing 컴포넌트를 찾을 수 없습니다!");
        if (playerController == null)
            Debug.LogError($"[{name}] SpelunkyPlayerController 컴포넌트를 찾을 수 없습니다!");
    }
    
    // 이동 관련 모든 처리를 통합한 메서드
    public void ProcessInput(SpelunkyPlayerInputData input)
    {
        // 덕킹 처리
        HandleDucking(input);
        
        // 이동 처리
        ProcessMovement(input);
        
        // 스프라이트 방향 전환 (네트워크 상태만)
        UpdateFacingDirection(input);

        // 정규화된 속도 업데이트
        NormalizedSpeed = Mathf.Abs(rb.linearVelocity.x) / moveSpeed;
    }
    
    private void HandleDucking(SpelunkyPlayerInputData input)
    {
        // 🎮 상태 기반 웅크리기 가능 여부 확인 (순환 의존성 방지)
        // 현재 상태가 Ducking이 아닐 때만 CanDuck 체크
        if (playerController.CurrentState != PlayerState.Ducking && !PlayerStateHelper.CanDuck(playerController.CurrentState)) 
        {
            IsDucking = false;
            return;
        }
        
        // 웅크리기 (아래키 + 땅에 있을 때)
        bool shouldDuck = input.VerticalInput < 0f && groundCheck.IsGrounded;
        
        // 상태가 변경될 때만 업데이트 (깜빡임 방지)
        if (IsDucking != shouldDuck)
        {
            IsDucking = shouldDuck;
        }
    }
    
    private void ProcessMovement(SpelunkyPlayerInputData input)
    {
        // 🎮 상태 기반 이동 가능 여부 확인
        if (!PlayerStateHelper.CanMove(playerController.CurrentState))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }
        
        // 사다리 오르는 중에는 플레이어 입력에 의한 수평 이동만 금지 (중앙 정렬은 허용)
        if (climbing.IsClimbing)
        {
            // 🎯 사다리 중앙 정렬을 위해 기존 X 속도는 유지 (PlayerClimbing에서 조절)
            // 플레이어 입력에 의한 수평 이동만 막음
            return;
        }
        
        // 웅크린 상태에 따라 속도 조절
        float currentMoveSpeed = IsDucking ? duckMoveSpeed : moveSpeed;
        float targetSpeed = input.HorizontalInput * currentMoveSpeed;
        
        if (input.HorizontalInput != 0)
        {
            rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }
    
    private void UpdateFacingDirection(SpelunkyPlayerInputData input)
    {
        // 네트워크 동기화되는 방향 상태 업데이트
        if (input.HorizontalInput != 0)
        {
            IsFacingLeft = input.HorizontalInput < 0;
        }
    }
} 