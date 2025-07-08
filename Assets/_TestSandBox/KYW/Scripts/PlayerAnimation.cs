using Fusion;
using UnityEngine;

// 🎭 플레이어 애니메이션 컴포넌트
// 애니메이션 파라미터 관리만 담당
public class PlayerAnimation : NetworkBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;
    
    // 참조 컴포넌트들
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private PlayerJump jump;
    
    public override void Spawned()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
            
        groundCheck = GetComponent<PlayerGroundCheck>();
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
    }
    
    public void UpdateAnimations()
    {
        if (animator == null) return;
        
        // 🎭 애니메이션 파라미터 설정
        float speed = movement.NormalizedSpeed;
        float velocityY = jump.VelocityY;
        bool isGrounded = groundCheck.IsGrounded;
        bool isDucking = movement.IsDucking;
        
        animator.SetFloat("Speed", speed);
        animator.SetFloat("VelocityY", velocityY);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsDucking", isDucking);
        
        // 🐛 디버그 로그 (점프 상태일 때만)
        if (showDebugLog && !isGrounded)
        {
            Debug.Log($"[점프 애니메이션] Speed: {speed:F2}, VelocityY: {velocityY:F2}, IsGrounded: {isGrounded}, IsDucking: {isDucking}");
        }
    }
} 