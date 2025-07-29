using Fusion;
using UnityEngine;

// 🛑 플레이어 스턴, 무적, 죽음, 들림 상태 관리 컴포넌트
public class PlayerStunInvincibleDie : NetworkBehaviour
{
    // 🕐 타이머들 (실제 로직 처리용)
    [Networked] private TickTimer StunTimer { get; set; }
    [Networked] private TickTimer InvincibleTimer { get; set; }
    [Networked] private TickTimer ThrownTimer { get; set; }
    
    // 🛑 상태 관련 네트워크 프로퍼티들
    [Networked] public bool IsStunned { get; private set; }
    [Networked] public bool IsDead { get; private set; }
    [Networked] public bool IsInvincible { get; private set; }
    [Networked] public bool IsHeld { get; private set; } // 들림 상태 추가
    [Networked] public bool IsThrown { get; private set; } // 던진 상태 추가

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
        
        IsInvincible = value;
        
        if (value && duration > 0f)
        {
            InvincibleTimer = TickTimer.CreateFromSeconds(Runner, duration);
        }
        else if (!value)
        {
            InvincibleTimer = TickTimer.None;
        }
    }

    // 🎯 들림 상태 설정
    public void SetHeld(bool value)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 사망 상태에서는 들림 설정 불가
        if (IsDead) return;
        
        IsHeld = value;
        Debug.Log($"[{name}] 들림 상태 설정: {value}");
    }
    
    // 🚀 던진 상태 설정 (duration초 동안)
    public void SetThrown(float duration = 1.5f)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 사망 상태에서는 던진 상태 설정 불가
        if (IsDead) return;
        
        IsThrown = true;
        ThrownTimer = TickTimer.CreateFromSeconds(Runner, duration);
        Debug.Log($"[{name}] 던진 상태 설정: {duration}초");
    }

    // 💀 죽음 처리
    public void Die()
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 이미 사망 상태라면 중복 처리 방지
        if (IsDead) return;
        
        IsDead = true;
        IsStunned = false;
        IsInvincible = false;
        IsHeld = false; // 사망 시 들림 상태 해제
        IsThrown = false; // 사망 시 던진 상태 해제
        
        // 타이머들 초기화
        StunTimer = TickTimer.None;
        InvincibleTimer = TickTimer.None;
        ThrownTimer = TickTimer.None;
        
        Debug.Log($"[{name}] 플레이어 사망 상태로 설정됨!");
    }
} 