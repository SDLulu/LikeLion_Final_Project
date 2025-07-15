using Fusion;
using UnityEngine;

// 🪜 플레이어 사다리 감지 컴포넌트 참조
// (PlayerLadderCheck.cs가 같은 네임스페이스에 있으므로 별도 using 불필요)

// 🪜 플레이어 사다리 오르기 컴포넌트
// 사다리 감지, 사다리 상태 관리, 사다리 오르기 처리 담당
public class PlayerClimbing : NetworkBehaviour
{
    [Header("Climbing Settings")]
    [SerializeField] private float climbingSpeed = 3f;      // 사다리 오르기 속도
    [SerializeField] private bool showDebugInfo = true;     // 디버그 정보 표시
    [SerializeField] private float climbRegrabCooldownTime = 0.2f; // 사다리 점프 후 재매달림 쿨타임(초)
    private float climbRegrabCooldown = 0f;

    [Header("Climbing Physics")]
    [SerializeField] private float climbingGravityScale = 0f; // 사다리 중 중력 (0 = 무중력)
    [SerializeField] private float normalGravityScale = 1f;   // 일반 상태 중력

    // 🪜 사다리 상태 추적 (네트워크 동기화)
    [Networked] public bool IsClimbing { get; private set; }
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }

    // 📦 컴포넌트 참조들
    private PlayerLadderCheck ladderCheck;
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private PlayerJump jump;
    private Rigidbody2D rb;

    private bool climbRequested = false;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        groundCheck = GetComponentInChildren<PlayerGroundCheck>();
        ladderCheck = GetComponentInChildren<PlayerLadderCheck>();
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
        Debug.Log($"🪜 PlayerClimbing 생성 - HasInputAuthority: {Object.HasInputAuthority}");
    }

    // 🪜 사다리 관련 모든 처리를 통합한 메서드
    public void ProcessInput(SpelunkyPlayerData input)
    {
        HandleClimbing(input);
        UpdateClimbingPhysics();
    }

    private void HandleClimbing(SpelunkyPlayerData input)
    {
        bool nearLadder = ladderCheck != null && ladderCheck.IsNearLadder;
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
            if (jump != null)
            {
                Debug.Log("[Climbing] 사다리에서 점프! 강제 점프 실행");
                jump.SetJumpFromClimb();
            }
            return;
        }

        // 2. 사다리 근처에서 위키를 한 번이라도 누르면 climbRequested = true (쿨타임 중엔 무시)
        if (nearLadder && input.VerticalInput > 0.5f && climbRegrabCooldown <= 0f)
        {
            climbRequested = true;
        }

        // 3. 사다리에서 벗어나면 climbRequested 해제
        if (!nearLadder)
        {
            climbRequested = false;
            if (IsClimbing)
                StopClimbing();
        }

        // 4. climbRequested && nearLadder일 때만 매달림
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

        // 5. 사다리 상태에서만 위/아래키로 오르내림, 좌우키 무시
        if (IsClimbing)
        {
            rb.linearVelocity = new Vector2(0, input.VerticalInput * climbingSpeed);
        }

        // 버튼 상태 갱신
        ButtonsPrevious = input.NetworkButtons;
    }

    private void StartClimbing()
    {
        IsClimbing = true;
        rb.linearVelocity = Vector2.zero;
        Debug.Log("🪜 사다리 오르기 시작!");
    }

    private void StopClimbing()
    {
        IsClimbing = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        Debug.Log("🪜 사다리 오르기 종료!");
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

    // 외부에서 사다리 상태 확인용 프로퍼티
    public bool IsCurrentlyClimbing => IsClimbing;

    // 🔍 디버그 정보 표시
    private void OnGUI()
    {
        if (!showDebugInfo || !Object.HasInputAuthority) return;
        GUILayout.BeginArea(new Rect(10, 630, 300, 120));
        GUILayout.Box("🪜 사다리 오르기 시스템");
        GUILayout.Label($"사다리 근처: {ladderCheck?.IsNearLadder}");
        GUILayout.Label($"오르기 중: {IsClimbing}");
        GUILayout.Label($"오르기 속도: {climbingSpeed}");
        GUILayout.Label("");
        GUILayout.Label("조작법: 사다리 근처에서 위키(W) 유지, 점프키로 탈출");
        GUILayout.EndArea();
    }
} 