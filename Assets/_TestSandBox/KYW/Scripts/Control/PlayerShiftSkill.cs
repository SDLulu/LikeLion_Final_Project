using Fusion;
using UnityEngine;

// 🎯 플레이어 쉬프트 스킬 컴포넌트
// 쉬프트키를 눌러서 특수 스킬을 사용하는 시스템
public class PlayerShiftSkill : NetworkBehaviour, ISoftReset
{
    [Header("Skill Settings")]
    [SerializeField] private float skillCooldown = 1f;    // 스킬 쿨다운 시간
    [SerializeField] private float skillDuration = 0.5f;  // 스킬 지속 시간
    
    // 🌐 네트워크 동기화 상태
    [Networked] private TickTimer skillCooldownTimer { get; set; }
    [Networked] private TickTimer skillDurationTimer { get; set; }
    [Networked] public bool IsSkillActive { get; private set; }
    
    // 참조 컴포넌트들
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private PlayerJump jump;
    private Rigidbody2D rb;
    private Animator animator;

    public override void Spawned()
    {
        SetupReferences();
    }
    
    private void SetupReferences()
    {
        rb = GetComponent<Rigidbody2D>();
        groundCheck = GetComponentInChildren<PlayerGroundCheck>();
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
        animator = GetComponentInChildren<Animator>();
    }

    public void ProcessInput(SpelunkyPlayerInputData input)
    {
        HandleSkillInput(input);
        UpdateSkillState();
    }

    private void HandleSkillInput(SpelunkyPlayerInputData input)
    {
        // InputAuthority에서만 스킬 입력 처리
        if (!Object.HasInputAuthority) return;
        
        // 쉬프트키 입력 감지
        if (input.NetworkButtons.IsSet(SpelunkyInputButtons.Skill))
        {
            TryActivateSkill();
        }
    }

    private void TryActivateSkill()
    {
        // 쿨다운 체크
        if (!skillCooldownTimer.ExpiredOrNotRunning(Runner)) return;
        
        // 스킬 활성화
        ActivateSkill();
    }

    private void ActivateSkill()
    {
        // StateAuthority에서만 스킬 실행
        if (!Object.HasStateAuthority) return;
        
        IsSkillActive = true;
        skillDurationTimer = TickTimer.CreateFromSeconds(Runner, skillDuration);
        
        // 스킬 효과 적용
        ApplySkillEffect();
        
        Debug.Log("🎯 스킬 활성화!");
    }

    private void DeactivateSkill()
    {
        if (!Object.HasStateAuthority) return;
        
        IsSkillActive = false;
        skillCooldownTimer = TickTimer.CreateFromSeconds(Runner, skillCooldown);
        
        // 스킬 효과 해제
        RemoveSkillEffect();
        
        Debug.Log("🎯 스킬 비활성화!");
    }

    private void UpdateSkillState()
    {
        // StateAuthority에서만 상태 업데이트
        if (!Object.HasStateAuthority) return;
        
        // 스킬 지속 시간 체크
        if (IsSkillActive && skillDurationTimer.Expired(Runner))
        {
            DeactivateSkill();
        }
    }

    // 🎯 스킬 효과 적용 (구체적인 스킬 로직은 여기에 구현)
    private void ApplySkillEffect()
    {
        // 애니메이션 파라미터 설정
        if (animator != null)
        {
            animator.SetBool("SkillActive", true);
        }
        
        // 여기에 구체적인 스킬 효과 구현
        // 예: 대시, 점프 강화, 무적 등
    }

    // 🎯 스킬 효과 해제
    private void RemoveSkillEffect()
    {
        // 애니메이션 파라미터 설정
        if (animator != null)
        {
            animator.SetBool("SkillActive", false);
        }
        
        // 여기에 스킬 효과 해제 로직 구현
    }

    // 🎯 스킬 상태 확인 (다른 컴포넌트에서 사용)
    public bool CanUseSkill()
    {
        return skillCooldownTimer.ExpiredOrNotRunning(Runner) && !IsSkillActive;
    }

    public float GetSkillCooldownProgress()
    {
        if (skillCooldownTimer.IsRunning)
        {
            return 1f - (skillCooldownTimer.RemainingTime(Runner) ?? 0f) / skillCooldown;
        }
        return 1f; // 쿨다운 완료
    }

    /// <summary>
    /// ISoftReset 구현: 스킬 진행/쿨다운 및 상태 초기화
    /// </summary>
    public void SoftReset()
    {
        if (!Object.HasStateAuthority) return;
        IsSkillActive = false;
        skillCooldownTimer = TickTimer.None;
        skillDurationTimer = TickTimer.None;
        if (animator != null)
        {
            animator.SetBool("SkillActive", false);
        }
    }
} 