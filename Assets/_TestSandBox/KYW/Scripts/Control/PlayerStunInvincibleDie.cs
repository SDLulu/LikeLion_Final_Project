using Fusion;
using UnityEngine;

// 🛑 플레이어 스턴, 무적, 들림 상태 관리 컴포넌트
public class PlayerStunInvincibleDie : NetworkBehaviour
{
    // 🕐 타이머들 (실제 로직 처리용)
    [Networked] private TickTimer StunTimer { get; set; }
    [Networked] private TickTimer InvincibleTimer { get; set; }
    [Networked] private TickTimer ThrownTimer { get; set; }
    
    // 🛑 상태 관련 네트워크 프로퍼티들
    [Networked] public bool IsStunned { get; private set; }
    [Networked] public bool IsInvincible { get; private set; }
    [Networked] public bool IsHeld { get; private set; } // 들림 상태 추가
    [Networked] public bool IsThrown { get; private set; } // 던진 상태 추가
    [Networked] public bool IsDead { get; private set; } // 죽음 상태 (외부 참조용)

    public override void Spawned()
    {
        Debug.Log($"🛑 PlayerStunInvincibleDie 초기화 완료!");
    }

    public override void FixedUpdateNetwork()
    {
        // 🛑 스턴 타이머 만료 시 스턴 해제
        if (IsStunned && StunTimer.Expired(Runner))
        {
            IsStunned = false;
        }
        
        // 🛡️ 무적 타이머 만료 시 무적 해제
        if (IsInvincible && InvincibleTimer.Expired(Runner))
        {
            IsInvincible = false;
        }
        
        // 🚀 던진 타이머 만료 시 던진 상태 해제
        if (IsThrown && ThrownTimer.Expired(Runner))
        {
            IsThrown = false;
            Debug.Log($"[{name}] 던진 상태 해제됨!");
        }
    }

    // 🛑 스턴 처리 (duration초 동안)
    public void Stun(float duration)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 사망 상태에서는 스턴 불가
        if (IsDead) return;
        
        // 무적 상태에서는 스턴 불가
        if (IsInvincible) return;
        
        IsStunned = true;
        StunTimer = TickTimer.CreateFromSeconds(Runner, duration);
    }

    // 🛡️ 무적 처리 (duration초 동안)
    public void SetInvincible(bool value, float duration = 0f)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 사망 상태에서는 무적 설정 불가
        if (IsDead) return;

        if (value)
        {
            // 이미 무적이라면 타이머 갱신 금지 (연속 히트로 무적 연장 방지)
            if (IsInvincible) return;
            IsInvincible = true;
            if (duration > 0f)
            {
                InvincibleTimer = TickTimer.CreateFromSeconds(Runner, duration);
            }
        }
        else
        {
            if (!IsInvincible) return;
            IsInvincible = false;
            InvincibleTimer = TickTimer.None;
        }
    }

    // 🎯 들림 상태 설정
    public void SetHeld(bool value)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
            
        IsHeld = value;
        Debug.Log($"[{name}] 들림 상태 설정: {value}");
    }
    
    // 🚀 던진 상태 설정 (duration초 동안)
    public void SetThrown(float duration = 1.5f)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;    
        
        IsThrown = true;
        ThrownTimer = TickTimer.CreateFromSeconds(Runner, duration);
        Debug.Log($"[{name}] 던진 상태 설정: {duration}초");
    }
    
    // 💀 죽음 상태 설정 (PlayerDeathHandler에서 호출)
    public void SetDead(bool value)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        IsDead = value;
        Debug.Log($"[{name}] 죽음 상태 설정: {value}");
    }
} 