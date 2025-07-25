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
            
        // Player 오브젝트 찾기
        Transform playerObject = transform.parent;
        if (playerObject != null)
        {
            groundCheck = playerObject.GetComponentInChildren<PlayerGroundCheck>();
            movement = playerObject.GetComponent<PlayerMovement>();
            jump = playerObject.GetComponent<PlayerJump>();
            climbing = playerObject.GetComponent<PlayerClimbing>();
            playerController = playerObject.GetComponent<SpelunkyPlayerController>();
            
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
        else
        {
            Debug.LogError($"[{name}] PlayerAnimation이 Player 오브젝트의 하위에 없습니다. " +
                          "Player/Visual 하위에 배치해주세요.");
        }
    }
    
    // 애니메이션 동기화
    public override void Render()
    {
        // Dead 상태면 IsDead 파라미터만 true로, 아니면 false로
        if (playerController != null && animator != null)
        {
            bool isDead = playerController.CurrentState == PlayerState.Dead;
            animator.SetBool("IsDead", isDead);
            if (isDead) return; // 사망 상태면 다른 애니메이션 갱신 불필요
        }
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