using Fusion;
using UnityEngine;

// 적/NPC 상호작용 추상 클래스 예시 (IEnemyInteraction 구현)
public abstract class EnemyInteractionBase : NetworkBehaviour, IEnemyInteraction
{
    // --- 네트워크 동기화 상태 ---
    [Networked] public int Health { get; protected set; }
    [Networked] public NetworkBool IsStunned { get; protected set; }
    [Networked] public NetworkBool _isInvincible { get; protected set; }
    [Networked] public TickTimer StunTimer { get; protected set; }

    // --- 한 줄짜리 프로퍼티 (필드 바로 밑에 배치) ---
    public virtual bool IsInvincible => _isInvincible;
    public virtual bool IsHoldable => IsStunned;

    // --- 메서드들 ---
    public virtual void ApplyKnockback(Vector3 force)
    {
        // 기본 넉백 로직 (예시: Rigidbody2D에 force 적용)
    }

    public virtual void TakeDamage(int damage)
    {
        if (_isInvincible) return;
        Health -= damage;
    }

    public virtual void ApplyStun(float duration)
    {
        IsStunned = true;
        StunTimer = TickTimer.CreateFromSeconds(Runner, duration);
    }

    public virtual void SetInvincible(bool value, float duration = 0f)
    {
        _isInvincible = value;
        // duration > 0이면 타이머로 해제 구현 가능
    }

    public virtual void OnPickedUp(Transform holder)
    {
        // 들렸을 때 로직
    }
    public virtual void OnReleased()
    {
        // 놓아졌을 때 로직
    }

    public override void FixedUpdateNetwork()
    {
        // 스턴 해제 및 들기 상태 자동 해제
        if (IsStunned && StunTimer.Expired(Runner))
        {
            IsStunned = false;
            OnReleased();
        }
    }
} 