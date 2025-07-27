using Fusion;
using UnityEngine;

// 🎮 플레이어 상태 관리자
// 플레이어의 현재 상태와 액션을 관리하고 네트워크 동기화 담당
public class PlayerStateManager : NetworkBehaviour
{
    [Header("Player State")]
    [Networked] public PlayerState CurrentState { get; private set; } = PlayerState.Normal;
    [Networked] public PlayerAction CurrentActions { get; private set; } = PlayerAction.None;
    
    // 📦 상태 결정에 필요한 컴포넌트들
    private PlayerStunInvincibleDie stunInvincible;
    private PlayerShiftSkill shiftSkill;
    
    // 🎮 입력 상태 (BeforeUpdate에서 설정됨)
    private bool useItemHeld;
    private bool skillPressed;
    
    public override void Spawned()
    {
        // 필요한 컴포넌트들 캐싱 (부모 오브젝트에서 찾기)
        stunInvincible = GetComponentInParent<PlayerStunInvincibleDie>();
        shiftSkill = GetComponentInParent<PlayerShiftSkill>();
        
        Debug.Log($"🎮 PlayerStateManager 초기화 완료!");
    }
    
    // 🎮 입력 상태 업데이트 (SpelunkyPlayerController에서 호출)
    public void UpdateInputState(bool useItemHeld, bool skillPressed)
    {
        this.useItemHeld = useItemHeld;
        this.skillPressed = skillPressed;
    }
    
    // 🎮 상태 업데이트 (FixedUpdateNetwork에서 호출)
    public void UpdateStates()
    {
        UpdateCurrentState();
        UpdateCurrentActions();
    }
    
    // 🎮 현재 상태 업데이트
    private void UpdateCurrentState()
    {
        // StateAuthority에서만 상태 변경
        if (!Object.HasStateAuthority) return;
        
        PlayerState newState = DetermineCurrentState();
        
        // 상태가 변경된 경우에만 업데이트
        if (CurrentState != newState)
        {
            CurrentState = newState;
            Debug.Log($"🎮 상태 변경: {CurrentState}");
        }
    }
    
    // 🎮 현재 액션 업데이트
    private void UpdateCurrentActions()
    {
        // StateAuthority에서만 액션 변경
        if (!Object.HasStateAuthority) return;
        
        PlayerAction newActions = PlayerAction.None;
        
        // 아이템 사용 중인지 확인
        if (useItemHeld)
        {
            newActions |= PlayerAction.UsingItem;
        }
        
        // 스킬 사용 중인지 확인
        if (shiftSkill != null && shiftSkill.IsSkillActive)
        {
            newActions |= PlayerAction.UsingSkill;
        }
        
        // 🛡️ 무적 상태 동기화
        if (stunInvincible != null && stunInvincible.IsInvincible)
        {
            newActions |= PlayerAction.Invincible;
        }
        
        // 액션이 변경된 경우에만 업데이트
        if (CurrentActions != newActions)
        {
            CurrentActions = newActions;
        }
    }
    
    // 🎮 현재 상태 결정 (PlayerStunInvincibleDie의 상태를 기반으로)
    private PlayerState DetermineCurrentState()
    {
        // 🎯 사망 상태는 최우선 (다른 상태로 전환 불가)
        if (stunInvincible != null && stunInvincible.IsDead) return PlayerState.Dead;
        
        // 🎯 스턴 상태는 두 번째 우선순위 (다른 상태로 전환 불가)
        if (stunInvincible != null && stunInvincible.IsStunned) return PlayerState.Stunned;
        
        // 나머지는 모두 Normal
        return PlayerState.Normal;
    }
    

    
    // 🎮 상태 강제 설정 (외부에서 호출)
    public void SetState(PlayerState newState)
    {
        if (!Object.HasStateAuthority) return;
        CurrentState = newState;
    }
    
    // 🎮 액션 강제 설정 (외부에서 호출)
    public void SetActions(PlayerAction newActions)
    {
        if (!Object.HasStateAuthority) return;
        CurrentActions = newActions;
    }
    

    // 🎮 상태 확인 헬퍼 메서드들
    public bool IsNormal => CurrentState == PlayerState.Normal;
    public bool IsUsingItem => (CurrentActions & PlayerAction.UsingItem) != 0;
    public bool IsUsingSkill => (CurrentActions & PlayerAction.UsingSkill) != 0;
} 