using Fusion;
using UnityEngine;

// 🏃 플레이어 이동 컴포넌트
// 좌우 이동, 덕킹 담당 (순수 로직만)
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float duckMoveSpeed = 2.5f;
    public float NormalizedSpeed { get; private set; }
    
    // 🌐 네트워크 동기화 상태
    [Networked] public bool IsDucking { get; private set; }
    [Networked] public bool IsFacingLeft { get; private set; }
    
    // 참조 컴포넌트들
    private PlayerGroundCheck groundCheck;
    private Rigidbody2D rb;
    private PlayerClimbing climbing;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        groundCheck = GetComponentInChildren<PlayerGroundCheck>();
        climbing = GetComponent<PlayerClimbing>();
    }
    
    // 이동 관련 모든 처리를 통합한 메서드
    public void ProcessInput(SpelunkyPlayerData input)
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
    
    private void HandleDucking(SpelunkyPlayerData input)
    {
        // 웅크리기 (아래키 + 땅에 있을 때)
        IsDucking = input.VerticalInput < -0.5f && groundCheck.IsGrounded;
    }
    
    private void ProcessMovement(SpelunkyPlayerData input)
    {
        // 사다리 오르는 중에는 수평 이동 금지
        if (climbing != null && climbing.IsClimbing)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }
        // 웅크린 상태에 따라 속도 조절
        float currentMoveSpeed = IsDucking ? duckMoveSpeed : moveSpeed;
        float targetSpeed = input.HorizontalInput * currentMoveSpeed;
        
        rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
    }
    
    private void UpdateFacingDirection(SpelunkyPlayerData input)
    {
        // 네트워크 동기화되는 방향 상태 업데이트
        if (input.HorizontalInput != 0)
        {
            IsFacingLeft = input.HorizontalInput < 0;
        }
    }
} 