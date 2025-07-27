using Fusion;
using UnityEngine;

// 🛑 플레이어 스턴, 무적 상태 관리 컴포넌트
public class PlayerStunInvincible : NetworkBehaviour
{
    [Networked] public bool IsStunned { get; private set; }
    [Networked] public bool IsDead { get; private set; }
    [Networked] public bool IsInvincible { get; private set; }
    [Networked] private TickTimer StunTimer { get; set; }
    [Networked] private TickTimer InvincibleTimer { get; set; }

    public override void FixedUpdateNetwork()
    {
        // 스턴 타이머 만료 시 스턴 해제
        if (IsStunned && StunTimer.Expired(Runner))
        {
            IsStunned = false;
        }
        // 무적 타이머 만료 시 무적 해제
        if (IsInvincible && InvincibleTimer.Expired(Runner))
        {
            IsInvincible = false;
        }
    }

    // 스턴 처리 (duration초 동안)
    public void Stun(float duration)
    {
        if (IsDead) return;
        IsStunned = true;
        StunTimer = TickTimer.CreateFromSeconds(Runner, duration);
    }

    // 무적 처리 (duration초 동안)
    public void SetInvincible(bool value, float duration = 0f)
    {
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

    // 죽음 처리
    public void Die()
    {
        IsDead = true;
        IsStunned = false;
        IsInvincible = false;
    }

    // 필요시 부활 등 추가 메서드 구현 가능
} 