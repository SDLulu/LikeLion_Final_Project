using Fusion;
using UnityEngine;

// 플레이어 상호작용 추상 클래스 예시 (IPlayerInteraction 구현)
public class PlayerInteractionBase : NetworkBehaviour, IPlayerInteraction
{
    // --- PlayerStunInvincibleDie 참조 ---
    protected PlayerStunInvincibleDie stunInvincible;
    protected PlayerHealth playerHealth;
    protected SpelunkyPlayerController playerController;

    // 🚫 플레이어 들기 상태 관리 (PlayerStunInvincibleDie로 이동됨)
    [Networked] public NetworkObject Holder { get; private set; }
    
    // IHoldable 인터페이스 구현
    public bool IsHoldable => true; // 플레이어는 언제나 들 수 있음

    public override void Spawned()
    {
        base.Spawned();
        playerHealth = GetComponentInChildren<PlayerHealth>();
        stunInvincible = GetComponent<PlayerStunInvincibleDie>();
        playerController = GetComponent<SpelunkyPlayerController>();
        if (playerHealth == null)
            Debug.LogError("[PlayerInteractionBase] PlayerHealth 컴포넌트를 찾을 수 없습니다!");
        if (stunInvincible == null)
            Debug.LogError("[PlayerInteractionBase] PlayerStunInvincibleDie 컴포넌트를 찾을 수 없습니다!");
        if (playerController == null)
            Debug.LogError("[PlayerInteractionBase] SpelunkyPlayerController 컴포넌트를 찾을 수 없습니다!");
    }

    // --- 메서드들 ---
    // 넉백 적용시 방향, 속도, 속도를 받을 시간을 적어서 그 시간동안 입력못받게 할거야todo
    public virtual void ApplyKnockback(Vector2 force, float stunDuration = 0f)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 기본 스턴 지속시간 0.5초로 설정 (stunDuration이 0이면)
        if (stunDuration <= 0f) stunDuration = 0.5f;
        
        // 넉백 힘 적용
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
            Debug.Log($"[{name}] 넉백 적용! 힘: {force}, 스턴시간: {stunDuration}초");
        }
        else
        {
            Debug.LogWarning($"[{name}] Rigidbody2D 컴포넌트를 찾을 수 없어 넉백을 적용할 수 없습니다!");
        }
        
        // 스턴 상태도 함께 적용 (입력 차단)
        ApplyStun(stunDuration);
    }
    


    public virtual void TakeDamage(int damage)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        if (stunInvincible?.IsInvincible == true) return;
        playerHealth?.TakeDamage(damage);
    }

    public virtual void ApplyStun(float duration)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        stunInvincible?.Stun(duration);
    }

    public virtual void SetInvincible(bool value, float duration = 0f)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        stunInvincible?.SetInvincible(value, duration);
    }
    
    // 💀 죽음 처리 (외부에서 호출 가능)
    public virtual void Die()
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 사망 상태로 설정
        stunInvincible?.Die();
        
        Debug.Log($"[{name}] 플레이어 사망 처리 완료!");
    }

    public override void FixedUpdateNetwork()
    {
        // 상태 관리는 PlayerStunInvincibleDie에서 처리
    }
    
    // IHoldable 인터페이스 구현
    public virtual void OnPickedUp()
    {
        if (!HasStateAuthority) return;
        
        stunInvincible?.SetHeld(true);
        
        Debug.Log($"[{name}] 플레이어가 들렸습니다!");
    }
    
    public virtual void OnReleased()
    {
        if (!HasStateAuthority) return;
        
        stunInvincible?.SetHeld(false);
        
        Debug.Log($"[{name}] 플레이어가 놓아졌습니다!");
    }
} 