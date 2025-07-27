using Fusion;
using UnityEngine;

// 🎭 플레이어 애니메이션 컴포넌트
// 애니메이션과 시각적 요소(스프라이트 뒤집기 등) 관리
// Visual 하위 오브젝트에 위치
public class PlayerAnimation : NetworkBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    // 참조 컴포넌트들 (Player 오브젝트에서 찾기)
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerClimbing climbing;
    private SpelunkyPlayerController playerController;
    
    public override void Spawned()
    {
        // 모든 컴포넌트 참조를 한 번에 설정
        if (animator == null)
            animator = GetComponent<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        // 부모 오브젝트에서 컴포넌트들 찾기 (규칙에 맞게 수정)
        groundCheck = GetComponentInParent<PlayerGroundCheck>();
        movement = GetComponentInParent<PlayerMovement>();
        jump = GetComponentInParent<PlayerJump>();
        climbing = GetComponentInParent<PlayerClimbing>();
        playerController = GetComponentInParent<SpelunkyPlayerController>();
        
        // 필수 컴포넌트 검증
        if (animator == null)
            Debug.LogError($"[{name}] Animator 컴포넌트를 찾을 수 없습니다!");
        if (spriteRenderer == null)
            Debug.LogError($"[{name}] SpriteRenderer 컴포넌트를 찾을 수 없습니다!");
        if (groundCheck == null)
            Debug.LogError($"[{name}] PlayerGroundCheck 컴포넌트를 찾을 수 없습니다!");
        if (movement == null)
            Debug.LogError($"[{name}] PlayerMovement 컴포넌트를 찾을 수 없습니다!");
        if (jump == null)
            Debug.LogError($"[{name}] PlayerJump 컴포넌트를 찾을 수 없습니다!");
        if (climbing == null)
            Debug.LogError($"[{name}] PlayerClimbing 컴포넌트를 찾을 수 없습니다!");
        if (playerController == null)
            Debug.LogError($"[{name}] SpelunkyPlayerController 컴포넌트를 찾을 수 없습니다!");
    }
    
    // 애니메이션 동기화
    public override void Render()
    {
        if (playerController == null || animator == null) return;
        
        // 🎭 상태별 애니메이션 처리
        PlayerState currentState = playerController.CurrentState;
        
        // 💀 사망 상태
        if (currentState == PlayerState.Dead)
        {
            animator.SetBool("IsDead", true);
            return; // 사망 상태면 다른 애니메이션 갱신 불필요
        }
        
        // 🛑 스턴 상태
        if (currentState == PlayerState.Stunned)
        {
            animator.SetBool("IsStunned", true);
            return; // 스턴 상태면 다른 애니메이션 갱신 불필요
        }
        
        // 🎮 정상 상태 애니메이션
        animator.SetBool("IsDead", false);
        animator.SetBool("IsStunned", false);
        UpdateAnimations();
        UpdateSpriteDirection();
    }
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        // 🎭 애니메이션 파라미터 설정 (null 체크 제거 - Spawned에서 검증됨)
        float speed = movement.NormalizedSpeed;
        float velocityY = jump.VelocityY;
        bool isGrounded = groundCheck.IsGrounded;
        bool isDucking = movement.IsDucking;
        bool isClimbing = climbing.IsClimbing;
        
        animator.SetFloat("Speed", speed);
        animator.SetFloat("VelocityY", velocityY);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsDucking", isDucking);
        animator.SetBool("IsClimbing", isClimbing);
    }
    
    private void UpdateSpriteDirection()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = movement.IsFacingLeft;
        }
    }
} 