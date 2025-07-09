using Fusion;
using UnityEngine;

// 🏃 플레이어 이동 컴포넌트
// 좌우 이동, 덕킹, 스프라이트 방향 전환 담당
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float duckMoveSpeed = 2.5f;
    
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    // 🌐 네트워크 동기화
    [Networked] public bool IsDucking { get; private set; }
    [Networked] public bool IsFacingLeft { get; private set; }
    
    // 참조 컴포넌트들
    private PlayerGroundCheck groundCheck;
    private Rigidbody2D rb;
    
    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        groundCheck = GetComponent<PlayerGroundCheck>();
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    public void HandleMovement(SpelunkyPlayerData input)
    {
        // 덕킹 처리
        HandleDucking(input);
        
        // 이동 처리
        ProcessMovement(input);
        
        // 스프라이트 방향 전환
        UpdateSpriteDirection(input);
    }
    
    private void HandleDucking(SpelunkyPlayerData input)
    {
        // 웅크리기 (아래키 + 땅에 있을 때)
        IsDucking = input.VerticalInput < -0.5f && groundCheck.IsGrounded;
    }
    
    private void ProcessMovement(SpelunkyPlayerData input)
    {
        // 웅크린 상태에 따라 속도 조절
        float currentMoveSpeed = IsDucking ? duckMoveSpeed : moveSpeed;
        float targetSpeed = input.HorizontalInput * currentMoveSpeed;
        
        rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
    }
    
    private void UpdateSpriteDirection(SpelunkyPlayerData input)
    {
        // 네트워크 동기화되는 방향 상태 업데이트
        if (input.HorizontalInput != 0)
        {
            IsFacingLeft = input.HorizontalInput < 0;
        }
    }
    
    // 실제 스프라이트 렌더링 업데이트 (매 프레임 호출)
    public void UpdateSpriteRendering()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = IsFacingLeft;
        }
    }
    
    // 다른 컴포넌트에서 참조할 수 있는 속성들
    public float CurrentSpeed => Mathf.Abs(rb.linearVelocity.x);
    public float NormalizedSpeed => CurrentSpeed / moveSpeed;
    public bool FacingLeft => IsFacingLeft;
} 