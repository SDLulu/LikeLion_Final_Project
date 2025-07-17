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
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;
    
    // 참조 컴포넌트들 (Player 오브젝트에서 찾기)
    private SpelunkyPlayerController playerController;
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerClimbing climbing;
    
    public override void Spawned()
    {
        // 필수 컴포넌트 찾기
        if (animator == null)
            animator = GetComponent<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        // Player 오브젝트 찾기
        Transform playerObject = transform.parent;
        if (playerObject != null)
        {
            playerController = playerObject.GetComponent<SpelunkyPlayerController>();
            groundCheck = playerObject.GetComponentInChildren<PlayerGroundCheck>();
            movement = playerObject.GetComponent<PlayerMovement>();
            jump = playerObject.GetComponent<PlayerJump>();
            climbing = playerObject.GetComponent<PlayerClimbing>();
        }
        else
        {
            Debug.LogWarning($"[{name}] PlayerAnimation이 Player 오브젝트의 하위에 없습니다. " +
                           "Player/Visual 하위에 배치해주세요.");
        }
    }
    
    // 다른 컴포넌트들과 일관성을 위해 ProcessInput 추가
    public void ProcessInput(SpelunkyPlayerData input)
    {
        UpdateAnimations();
        UpdateSpriteDirection();
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        // 🎭 애니메이션 파라미터 설정
        float speed = movement?.NormalizedSpeed ?? 0f;
        float velocityY = jump?.VelocityY ?? 0f;
        bool isGrounded = groundCheck?.IsGrounded ?? false;
        bool isDucking = movement?.IsDucking ?? false;
        bool isClimbing = climbing?.IsClimbing ?? false;
        
        animator.SetFloat("Speed", speed);
        animator.SetFloat("VelocityY", velocityY);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsDucking", isDucking);
        animator.SetBool("IsClimbing", isClimbing);
    }
    
    private void UpdateSpriteDirection()
    {
        if (spriteRenderer != null && movement != null)
        {
            spriteRenderer.flipX = movement.IsFacingLeft;
        }
    }
    
    // 🔍 디버그 정보 표시 (일관된 위치에 표시)
    private void OnGUI()
    {
        if (!showDebugLog || !Object.HasInputAuthority) return;
        
        GUILayout.BeginArea(new Rect(10, 700, 300, 100));
        GUILayout.Box("🎭 애니메이션 상태");
        GUILayout.Label($"속도: {movement?.NormalizedSpeed:F2}");
        GUILayout.Label($"수직속도: {jump?.VelocityY:F2}");
        GUILayout.Label($"상태: {(groundCheck?.IsGrounded == true ? "지상" : "공중")}");
        GUILayout.EndArea();
    }
} 