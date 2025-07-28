using Fusion;
using UnityEngine;
using UnityEngine.UI;

// 🎭 플레이어 애니메이션 컴포넌트
// 애니메이션과 시각적 요소(스프라이트 뒤집기 등) 관리
// Visual 하위 오브젝트에 위치
public class PlayerAnimation : NetworkBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("Invincible Effect")]
    [SerializeField] private Material invincibleMaterial; // 무적용 메테리얼
    [SerializeField, Tooltip("각 상태 지속 시간 (초 단위). 0.5 = 0.5초씩 원본/무적 메테리얼 교체")]
    private float invincibleBlinkDuration = 0.5f; // 각 상태 지속 시간 (초 단위)
    
    [Header("Stun UI Effect")]
    [SerializeField] private GameObject stunUIEffect; // 스턴용 UI 이펙트 (미리 배치된 오브젝트)
    
    // 참조 컴포넌트들 (Player 오브젝트에서 찾기)
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerClimbing climbing;
    private SpelunkyPlayerController playerController;
    
    // 🛡️ 무적 이펙트 관련 변수들
    private Material originalMaterial;
    private bool wasInvincible = false;
    private float invincibleEffectTimer = 0f;
    private bool isBlinkingOn = false; // 현재 깜박임 상태 (true = 무적 메테리얼, false = 원본 메테리얼)
    
    // 🛑 스턴 UI 이펙트 관련 변수들
    private bool wasStunned = false;
    
    public override void Spawned()
    {
        // 모든 컴포넌트 참조를 한 번에 설정
        if (animator == null)
            animator = GetComponent<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        // 부모 Player 오브젝트에서 컴포넌트들 찾기
        groundCheck = transform.parent.GetComponentInChildren<PlayerGroundCheck>();
        movement = GetComponentInParent<PlayerMovement>();
        jump = GetComponentInParent<PlayerJump>();
        climbing = GetComponentInParent<PlayerClimbing>();
        playerController = GetComponentInParent<SpelunkyPlayerController>();
        
        // 원본 메테리얼 저장
        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
        }
        
        // 필수 컴포넌트 검증
        if (animator == null)
            Debug.LogError($"[{name}] Animator 컴포넌트를 찾을 수 없습니다!");
        if (spriteRenderer == null)
            Debug.LogError($"[{name}] SpriteRenderer 컴포넌트를 찾을 수 없습니다!");
        if (groundCheck == null)
        {
            Debug.LogError($"[{name}] PlayerGroundCheck 컴포넌트를 찾을 수 없습니다!");
            Debug.LogError($"[{name}] Player Root: {transform.parent?.name ?? "null"}");
        }
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
        
        // 💀 사망 상태
        if (playerController.IsDead)
        {
            animator.SetBool("IsDead", true);
            // 사망 상태에서는 모든 다른 이펙트 해제
            ResetInvincibleEffect();
            ResetStunUIEffect();
            return; // 사망 상태면 다른 애니메이션 갱신 불필요
        }
        
        // 🛑 스턴 상태
        if (playerController.IsStunned)
        {
            animator.SetBool("IsStunned", true);
            UpdateStunUIEffect();
            return; // 스턴 상태면 다른 애니메이션 갱신 불필요
        }
        
        // 🎮 정상 상태 애니메이션
        animator.SetBool("IsDead", false);
        animator.SetBool("IsStunned", false);
        UpdateAnimations();
        UpdateSpriteDirection();
        
        // 🛡️ 무적 이펙트 처리
        UpdateInvincibleEffect();
        
        // 무적 이펙트 해제
        if (wasInvincible && !playerController.IsInvincible)
        {
            ResetInvincibleEffect();
        }
        
        // 스턴 UI 이펙트 해제
        if (wasStunned)
        {
            ResetStunUIEffect();
        }
    }
    
    private void UpdateAnimations()
    {
        if (animator == null || movement == null || jump == null || groundCheck == null || climbing == null) return;
        
        // 🎭 애니메이션 파라미터 설정
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
        if (spriteRenderer != null && movement != null)
        {
            spriteRenderer.flipX = movement.IsFacingLeft;
        }
    }
    
    // 🛡️ 무적 이펙트 처리
    private void UpdateInvincibleEffect()
    {
        if (spriteRenderer == null || invincibleMaterial == null || playerController == null) return;
        
        // 무적 상태가 시작되었는지 확인
        if (playerController.IsInvincible && !wasInvincible)
        {
            wasInvincible = true;
            invincibleEffectTimer = 0f;
            isBlinkingOn = true; // 무적 상태 시작 시 깜박임 시작
        }
        
        // 무적 상태일 때만 이펙트 적용
        if (playerController.IsInvincible)
        {
            invincibleEffectTimer += Time.deltaTime; // 각 상태 지속 시간 사용
            
            // 깜박임 상태 전환
            if (invincibleEffectTimer >= invincibleBlinkDuration && isBlinkingOn)
            {
                isBlinkingOn = false;
                invincibleEffectTimer = 0f; // 새로운 상태 시작 시 타이머 초기화
            }
            else if (invincibleEffectTimer >= invincibleBlinkDuration && !isBlinkingOn)
            {
                isBlinkingOn = true;
                invincibleEffectTimer = 0f; // 새로운 상태 시작 시 타이머 초기화
            }

            // 메테리얼 변경 (깜빡이는 효과)
            if (isBlinkingOn)
            {
                spriteRenderer.material = invincibleMaterial;
            }
            else
            {
                spriteRenderer.material = originalMaterial;
            }
        }
    }
    
    // 🛡️ 무적 이펙트 리셋
    private void ResetInvincibleEffect()
    {
        if (spriteRenderer != null && originalMaterial != null)
        {
            spriteRenderer.material = originalMaterial;
        }
        
        wasInvincible = false;
        invincibleEffectTimer = 0f;
        isBlinkingOn = false; // 무적 상태 종료 시 깜박임 중지
    }
    
    // 🛑 스턴 UI 이펙트 처리
    private void UpdateStunUIEffect()
    {
        // 스턴 상태가 시작되었는지 확인
        if (!wasStunned)
        {
            wasStunned = true;
            ShowStunUIEffect();
        }
    }
    
    // 🛑 스턴 UI 이펙트 표시
    private void ShowStunUIEffect()
    {
        if (stunUIEffect != null)
        {
            stunUIEffect.SetActive(true);
            Debug.Log($"[{name}] 스턴 UI 이펙트 표시!");
        }
    }
    
    // 🛑 스턴 UI 이펙트 리셋
    private void ResetStunUIEffect()
    {
        if (stunUIEffect != null)
        {
            stunUIEffect.SetActive(false);
        }
        
        wasStunned = false;
    }
} 