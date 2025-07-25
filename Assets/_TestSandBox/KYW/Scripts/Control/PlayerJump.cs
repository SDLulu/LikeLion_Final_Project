using Fusion;
using UnityEngine;

// 🦘 플레이어 점프 컴포넌트
// 점프, 중력, 속도 제한 담당
public class PlayerJump : NetworkBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpSpeed = 10f;         // 점프 상승 속도 (일정)
    [SerializeField] private float maxJumpTime = 0.3f;      // 최대 점프 지속 시간
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float maxFallSpeed = 15f;
    [SerializeField] private bool showDebugInfo = true;     // 디버그 정보 표시
    public float VelocityY { get; private set; }
    
    // 🦘 점프 상태 추적
    [Networked] public bool IsJumping { get; private set; }
    [Networked] public float JumpTime { get; private set; }
    
    // Fusion 2 공식 패턴: 이전 버튼 상태 추적 (GetPressed 사용)
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    
    // 참조 컴포넌트들
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private Rigidbody2D rb;
    private SpelunkyPlayerController playerController;
    
    public override void Spawned()
    {
        // 모든 컴포넌트 참조를 한 번에 설정
        rb = GetComponent<Rigidbody2D>();
        groundCheck = GetComponentInChildren<PlayerGroundCheck>();
        movement = GetComponent<PlayerMovement>();
        playerController = GetComponent<SpelunkyPlayerController>();
        
        // 필수 컴포넌트 검증
        if (rb == null)
            Debug.LogError($"[{name}] Rigidbody2D 컴포넌트를 찾을 수 없습니다!");
        if (groundCheck == null)
            Debug.LogError($"[{name}] PlayerGroundCheck 컴포넌트를 찾을 수 없습니다!");
        if (movement == null)
            Debug.LogError($"[{name}] PlayerMovement 컴포넌트를 찾을 수 없습니다!");
        if (playerController == null)
            Debug.LogError($"[{name}] SpelunkyPlayerController 컴포넌트를 찾을 수 없습니다!");
    }
    
    // 점프 관련 모든 처리를 통합한 메서드
    public void ProcessInput(SpelunkyPlayerInputData input)
    {
        HandleJump(input);
        ApplyGravity();
        ClampVelocity();
        
        // 수직 속도 업데이트
        VelocityY = rb.linearVelocity.y;
    }

    // 사다리에서 강제 점프 진입용 (Climbing에서 호출)
    public void SetJumpFromClimb()
    {
        if (!IsJumping)
        {
            IsJumping = true;
            JumpTime = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
            Debug.Log("🦘 사다리에서 점프!");
        }
    }
    
    private void HandleJump(SpelunkyPlayerInputData input)
    {
        // Fusion 2 공식 패턴: GetPressed로 점프 버튼 눌림 감지
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        bool jumpHeld = input.NetworkButtons.IsSet(SpelunkyInputButtons.Jump);
        
        // 이전 상태 업데이트 (공식 패턴)
        ButtonsPrevious = input.NetworkButtons;
        
        // 🎮 점프 시작 (땅에 있을 때만, 한 번만 감지) - 상태 체크는 시작 시에만
        if (pressed.IsSet(SpelunkyInputButtons.Jump) && groundCheck.IsGrounded)
        {
            // 점프 시작 시에만 상태 기반 점프 가능 여부 확인
            if (!PlayerStateHelper.CanJump(playerController.CurrentState)) return;
            
            // 점프 상태 시작
            IsJumping = true;
            JumpTime = 0f;
            Debug.Log("🦘 점프 시작!");
        }
        
        // 점프 중일 때 처리
        if (IsJumping)
        {
            // 점프 시간 업데이트
            JumpTime += Runner.DeltaTime;
            
            // 점프키를 누르고 있고, 최대 시간을 넘지 않았으면 일정한 속도로 상승
            if (jumpHeld && JumpTime < maxJumpTime)
            {
                // 일정한 속도로 상승 (가속 없음)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
            }
            else
            {
                // 점프키를 떼거나 최대 시간 도달시 점프 종료
                IsJumping = false;
                Debug.Log($"🦘 점프 종료 - 시간: {JumpTime:F2}s, 높이: {JumpTime * jumpSpeed:F1}");
            }
        }
        
        // 땅에 닿으면 점프 상태 초기화
        if (groundCheck.IsGrounded && rb.linearVelocity.y <= 0)
        {
            IsJumping = false;
            JumpTime = 0f;
        }
    }
    
    private void ApplyGravity()
    {
        if (!groundCheck.IsGrounded)
        {
            rb.linearVelocity += Vector2.down * gravity * Runner.DeltaTime;
        }
    }
    
    private void ClampVelocity()
    {
        // 최대 낙하 속도 제한
        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }
    
    // 🔍 디버그 정보 표시
    private void OnGUI()
    {
        if (!showDebugInfo || !Object.HasInputAuthority) return;
        
        GUILayout.BeginArea(new Rect(10, 510, 300, 100));
        GUILayout.Box("🦘 점프 상태");
        GUILayout.Label($"땅에 있음: {groundCheck?.IsGrounded}");
        GUILayout.Label($"점프 중: {IsJumping}");
        GUILayout.Label($"점프 시간: {JumpTime:F2}s / {maxJumpTime:F2}s");
        GUILayout.Label($"세로 속도: {rb.linearVelocity.y:F1}");
        GUILayout.EndArea();
    }
} 