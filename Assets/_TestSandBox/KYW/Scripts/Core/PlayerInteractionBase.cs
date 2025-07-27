using Fusion;
using UnityEngine;

// 플레이어 상호작용 추상 클래스 예시 (IPlayerInteraction 구현)
public class PlayerInteractionBase : NetworkBehaviour, IPlayerInteraction
{
    // --- PlayerStunDeadInvincible 참조 ---
    protected PlayerStunInvincible stunInvincible;
    protected PlayerHealth playerHealth;
    protected SpelunkyPlayerController playerController;

    public override void Spawned()
    {
        base.Spawned();
        playerHealth = GetComponent<PlayerHealth>();
        stunInvincible = GetComponent<PlayerStunInvincible>();
        playerController = GetComponent<SpelunkyPlayerController>();
        if (playerHealth == null)
            Debug.LogError("[PlayerInteractionBase] PlayerHealth 컴포넌트를 찾을 수 없습니다!");
        if (stunInvincible == null)
            Debug.LogError("[PlayerInteractionBase] PlayerStunInvincible 컴포넌트를 찾을 수 없습니다!");
        if (playerController == null)
            Debug.LogError("[PlayerInteractionBase] SpelunkyPlayerController 컴포넌트를 찾을 수 없습니다!");
    }

    // --- 프로퍼티 ---
    public virtual bool IsStunned => stunInvincible != null && stunInvincible.IsStunned;
    public virtual bool IsDead => stunInvincible != null && stunInvincible.IsDead;
    public virtual bool IsInvincible => stunInvincible != null && stunInvincible.IsInvincible;

    // --- 메서드들 ---
    public virtual void ApplyKnockback(Vector3 force)
    {
        // 기본 넉백 로직 (예시: Rigidbody2D에 force 적용)
    }

    public virtual void TakeDamage(int damage)
    {
        if (IsInvincible) return;
        playerHealth?.TakeDamage(damage);
    }

    public virtual void ApplyStun(float duration)
    {
        playerController.SetState(PlayerState.Stunned);
        // 스턴 네트워크 상태 동기화
        stunInvincible?.Stun(duration);
    }

    public virtual void SetInvincible(bool value, float duration = 0f)
    {
        stunInvincible?.SetInvincible(value, duration);
    }

    public override void FixedUpdateNetwork()
    {
        // 상태 관리는 PlayerStunDeadInvincible에서 처리
    }
} 