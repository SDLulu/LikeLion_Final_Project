using Fusion;
using UnityEngine;

// 🪜 플레이어 사다리 오르기 컴포넌트
// 사다리 감지, 사다리 상태 관리, 사다리 오르기 처리 담당
public class PlayerClimbing : NetworkBehaviour
{
    [Header("Climbing Settings")]
    [SerializeField] private float climbingSpeed = 3f;      // 사다리 오르기 속도
    [SerializeField] private float climbRegrabCooldownTime = 0.2f; // 사다리 점프 후 재매달림 쿨타임(초)
    [SerializeField] private float climbingGravityScale = 0f; // 사다리 중 중력 (0 = 무중력)
    [SerializeField] private float normalGravityScale = 1f;   // 일반 상태 중력
    [SerializeField] private float centerSnapSpeed = 30f; // 사다리 중심 흡입 속도
    
    // 🌐 네트워크 동기화 상태
    [Networked] public bool IsClimbing { get; private set; }
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    
    // 내부 상태
    private float climbRegrabCooldown = 0f;
    private bool climbRequested = false;

    // 참조 컴포넌트들
    private PlayerLadderCheck ladderCheck;
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private PlayerJump jump;
    private Rigidbody2D rb;

    public override void Spawned()
    {
        // 모든 컴포넌트 참조를 한 번에 설정
        rb = GetComponent<Rigidbody2D>();
        groundCheck = GetComponentInChildren<PlayerGroundCheck>();
        ladderCheck = GetComponentInChildren<PlayerLadderCheck>();
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
        
        // 필수 컴포넌트 검증
        if (rb == null)
            Debug.LogError($"[{name}] Rigidbody2D 컴포넌트를 찾을 수 없습니다!");
        if (groundCheck == null)
            Debug.LogError($"[{name}] PlayerGroundCheck 컴포넌트를 찾을 수 없습니다!");
        if (ladderCheck == null)
            Debug.LogError($"[{name}] PlayerLadderCheck 컴포넌트를 찾을 수 없습니다!");
        if (movement == null)
            Debug.LogError($"[{name}] PlayerMovement 컴포넌트를 찾을 수 없습니다!");
        if (jump == null)
            Debug.LogError($"[{name}] PlayerJump 컴포넌트를 찾을 수 없습니다!");
    }

    public void ProcessInput(SpelunkyPlayerData input)
    {
        HandleClimbing(input);
        UpdateClimbingPhysics();
    }

    private void HandleClimbing(SpelunkyPlayerData input)
    {
        bool nearLadder = ladderCheck.IsNearLadder;
        bool isGrounded = groundCheck.IsGrounded;
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);

        // 쿨타임 감소
        if (climbRegrabCooldown > 0f)
            climbRegrabCooldown -= Runner.DeltaTime;

        // 1. 사다리 상태에서 점프키 누르면 해제 + 점프 (최우선)
        if (IsClimbing && pressed.IsSet(SpelunkyInputButtons.Jump))
        {
            StopClimbing();
            climbRequested = false;
            climbRegrabCooldown = climbRegrabCooldownTime; // 쿨타임 시작
            jump.SetJumpFromClimb();
            return;
        }

        // 2. 사다리 상태에서 땅에 닿으면 Climbing 해제
        if (IsClimbing && isGrounded)
        {
            StopClimbing();
            climbRequested = false;
            return;
        }

        // 3. 사다리 근처에서 위키를 한 번이라도 누르면 climbRequested = true (쿨타임 중엔 무시)
        if (nearLadder && input.VerticalInput > 0.5f && climbRegrabCooldown <= 0f)
        {
            climbRequested = true;
        }

        // 4. 사다리에서 벗어나면 climbRequested 해제
        if (!nearLadder)
        {
            climbRequested = false;
            if (IsClimbing)
                StopClimbing();
        }

        // 5. climbRequested && nearLadder일 때만 매달림
        if (climbRequested && nearLadder)
        {
            if (!IsClimbing)
                StartClimbing();
        }
        else
        {
            if (IsClimbing)
                StopClimbing();
        }

        // 6. 사다리 상태에서만 위/아래키로 오르내림, 좌우키 무시
        if (IsClimbing)
        {
            float xVelocity = 0f;
            // 사다리 중앙으로 X축 속도 보정 (자연스럽게 붙도록)
            if (ladderCheck.CurrentLadderCenter.HasValue)
            {
                float centerX = ladderCheck.CurrentLadderCenter.Value.x;
                float diff = centerX - rb.position.x;
                xVelocity = diff * centerSnapSpeed; // 중심 흡입 속도 적용
            }
            float yVelocity = input.VerticalInput * climbingSpeed;
            rb.linearVelocity = new Vector2(xVelocity, yVelocity);
        }

        // 버튼 상태 갱신
        ButtonsPrevious = input.NetworkButtons;
    }

    private void StartClimbing()
    {
        IsClimbing = true;
        rb.linearVelocity = Vector2.zero;
    }

    private void StopClimbing()
    {
        IsClimbing = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
    }

    private void UpdateClimbingPhysics()
    {
        if (IsClimbing)
        {
            rb.gravityScale = climbingGravityScale;
        }
        else
        {
            rb.gravityScale = normalGravityScale;
        }
    }
} 