using Fusion;
using UnityEngine;

// 🦘 플레이어 점프 컴포넌트
// 점프, 중력, 속도 제한 담당
public class PlayerJump : NetworkBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float maxFallSpeed = 15f;
    
    // 참조 컴포넌트들
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private Rigidbody2D rb;
    
    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        groundCheck = GetComponent<PlayerGroundCheck>();
        movement = GetComponent<PlayerMovement>();
    }
    
    public void HandleJump(SpelunkyPlayerData input)
    {
        // 웅크린 상태에서는 점프 불가
        if (movement.IsDucking) return;
        
        // 간단한 점프 (땅에 있을 때만)
        if (input.NetworkButtons.IsSet(SpelunkyInputButtons.Jump) && groundCheck.IsGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }
    
    public void ApplyGravity()
    {
        if (!groundCheck.IsGrounded)
        {
            rb.linearVelocity += Vector2.down * gravity * Runner.DeltaTime;
        }
    }
    
    public void ClampVelocity()
    {
        // 최대 낙하 속도 제한
        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }
    
    // 다른 컴포넌트에서 참조할 수 있는 속성들
    public float VelocityY => rb.linearVelocity.y;
    public Vector2 Velocity => rb.linearVelocity;
} 