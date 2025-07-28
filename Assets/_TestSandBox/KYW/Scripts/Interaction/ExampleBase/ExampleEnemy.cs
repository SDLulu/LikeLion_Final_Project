using Fusion;
using UnityEngine;

// EnemyInteractionBase를 상속받는 예시 클래스
public class ExampleEnemy : EnemyInteractionBase
{
    [Header("Enemy Settings")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float stunDuration = 2f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    public override void Spawned()
    {
        base.Spawned();
        
        // 컴포넌트 캐싱
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 초기 상태 설정
        Health = maxHealth;
    }

    // 넉백 오버라이드: Rigidbody2D에 실제 힘 적용
    public override void ApplyKnockback(Vector2 force, float duration = 0f)
    {
        if (rb != null)
        {
            rb.AddForce(force * knockbackForce, ForceMode2D.Impulse);
        }
    }

    // 데미지 오버라이드: 데미지 표시 + 사망 처리
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage); // 기본 데미지 로직 실행
        
        // 데미지 표시 (예시)
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashRed());
        }
        
        // 사망 처리
        if (Health <= 0)
        {
            Die();
        }
    }

    // 스턴 오버라이드: 시각적 효과 추가
    public override void ApplyStun(float duration)
    {
        base.ApplyStun(duration);
        
        // 스턴 시각적 효과 (예시)
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.yellow; // 노란색으로 변경
        }
    }

    // 무적 설정 오버라이드: 무적 시간 타이머 추가
    public override void SetInvincible(bool value, float duration = 0f)
    {
        base.SetInvincible(value, duration);
        
        if (value && duration > 0f)
        {
            // 무적 시간이 끝나면 자동으로 해제
            StartCoroutine(InvincibleTimer(duration));
        }
    }

    // 들기 오버라이드: 물리 비활성화
    public override void OnPickedUp()
    {
        base.OnPickedUp();
        
        // 물리 시뮬레이션 비활성화
        if (rb != null)
        {
            rb.simulated = false;
        }
        
        // 시각적 효과
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.blue; // 파란색으로 변경
        }
    }

    // 놓기 오버라이드: 물리 활성화
    public override void OnReleased()
    {
        base.OnReleased();
        
        // 물리 시뮬레이션 활성화
        if (rb != null)
        {
            rb.simulated = true;
        }
        
        // 시각적 효과 복구
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    // FixedUpdateNetwork 오버라이드: 추가 로직
    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork(); // 기본 스턴 해제 로직 실행
        
        // 스턴 상태가 해제되면 시각적 효과도 복구
        if (!IsStunned && spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    // --- 헬퍼 메서드들 ---
    
    private void Die()
    {
        Debug.Log($"[{name}] 사망!");
        // 사망 처리 로직 (예: 파괴, 애니메이션 등)
        Runner.Despawn(Object);
    }

    private System.Collections.IEnumerator FlashRed()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }

    private System.Collections.IEnumerator InvincibleTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        SetInvincible(false);
    }

    // --- 외부에서 호출 가능한 메서드들 ---
    
    // 데미지를 받는 메서드 (외부에서 호출)
    public void Hit(int damage, Vector3 knockbackDirection)
    {
        TakeDamage(damage);
        ApplyKnockback(knockbackDirection);
    }

    // 스턴을 받는 메서드 (외부에서 호출)
    public void GetStunned()
    {
        ApplyStun(stunDuration);
    }

    // 무적 상태로 만드는 메서드 (외부에서 호출)
    public void BecomeInvincible(float duration = 1f)
    {
        SetInvincible(true, duration);
    }
} 