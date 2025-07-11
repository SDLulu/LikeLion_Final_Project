using UnityEngine;

// 🎭 테스트용 간단한 적 (아이템 데미지 테스트용)
public class TestEnemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color damageColor = Color.red;
    
    private void Awake()
    {
        currentHealth = maxHealth;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    // 데미지 받기
    public void TakeDamage(float damage, Vector2 knockbackDirection = default, float knockbackForce = 0f)
    {
        currentHealth -= damage;
        Debug.Log($"🎭 {name}이(가) {damage} 데미지를 받았습니다! (남은 체력: {currentHealth})");
        
        // 체력 UI 업데이트 (간단한 색상 변화)
        UpdateHealthVisual();
        
        // 데미지 피드백
        StartCoroutine(DamageFlash());
        
        // 넉백 적용
        if (knockbackForce > 0f && knockbackDirection != Vector2.zero)
        {
            ApplyKnockback(knockbackDirection, knockbackForce);
        }
        
        // 죽음 처리
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void UpdateHealthVisual()
    {
        if (spriteRenderer != null)
        {
            // 체력 비율에 따라 투명도 조절
            float healthRatio = currentHealth / maxHealth;
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(0.3f, 1f, healthRatio);
            spriteRenderer.color = color;
        }
    }
    
    private System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = normalColor;
        }
    }
    
    private void ApplyKnockback(Vector2 direction, float force)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
        }
    }
    
    private void Die()
    {
        Debug.Log($"💀 {name}이(가) 죽었습니다!");
        
        // 죽음 이펙트 (간단한 크기 변화)
        transform.localScale = Vector3.zero;
        
        // 1초 후 제거
        Destroy(gameObject, 1f);
    }
    
    // 체력 회복 (테스트용)
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthVisual();
        Debug.Log($"💚 {name}이(가) {amount} 회복! (현재 체력: {currentHealth})");
    }
    
    // 기즈모로 충돌 범위 표시
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>()?.bounds.size ?? Vector3.one);
    }
} 