using Fusion;
using UnityEngine;

// 🎭 플레이어 애니메이션 컴포넌트
// 애니메이션 파라미터 관리만 담당
// Visual 하위 오브젝트에 위치하며, 부모의 로직 컴포넌트들을 참조
public class PlayerAnimation : NetworkBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;
    
    // 참조 컴포넌트들 (부모 오브젝트에서 찾기)
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerClimbing climbing;
    
    public override void Spawned()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
            
        // 부모 오브젝트에서 로직 컴포넌트들 찾기
        Transform parent = transform.parent;
        if (parent != null)
        {
            groundCheck = parent.GetComponentInChildren<PlayerGroundCheck>();
            movement = parent.GetComponent<PlayerMovement>();
            jump = parent.GetComponent<PlayerJump>();
            climbing = parent.GetComponent<PlayerClimbing>();
        }
        else
        {
            Debug.LogWarning($"[{name}] PlayerAnimation이 부모 오브젝트 없이 있습니다. " +
                           "Player 오브젝트의 하위에 배치해주세요.");
        }
    }
    
    public void UpdateAnimations()
    {
        if (animator == null) return;
        
        // 🎭 애니메이션 파라미터 설정
        float speed = movement?.NormalizedSpeed ?? 0f;
        float velocityY = jump?.VelocityY ?? 0f;
        bool isGrounded = groundCheck?.IsGrounded ?? false;
        bool isDucking = movement?.IsDucking ?? false;
        bool isClimbing = climbing?.IsCurrentlyClimbing ?? false;
        
        animator.SetFloat("Speed", speed);
        animator.SetFloat("VelocityY", velocityY);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsDucking", isDucking);
        animator.SetBool("IsClimbing", isClimbing);
        
        // 🐛 디버그 로그 (점프 상태일 때만)
        if (showDebugLog && !isGrounded)
        {
            Debug.Log($"[점프 애니메이션] Speed: {speed:F2}, VelocityY: {velocityY:F2}, IsGrounded: {isGrounded}, IsDucking: {isDucking}");
        }
    }
} 