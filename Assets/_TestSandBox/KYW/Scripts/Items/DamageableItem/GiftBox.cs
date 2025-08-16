using UnityEngine;

// 🎁 선물상자 - 체력 1, 부서질 때 아이템/적 스폰
public class GiftBox : MonoBehaviour, IDamageable, IItemInteraction
{
    [Header("🎁 선물상자 설정")]
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private float destroyDelay = 0.1f;
    
    [Header("🎁 스폰 풀")]
    [SerializeField] private RandomSpawnPool spawnPool;
    
    private int currentHealth;
    private bool isDestroyed = false;
    private bool isHeld = false;
    
    // IDamageable 인터페이스 구현
    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }
    public bool IsDead { get { return currentHealth <= 0; } }
    
    private void Start()
    {
        currentHealth = maxHealth;
    }
    
    // 데미지 받기
    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;
        
        currentHealth -= damage;
        
        // 체력이 0 이하가 되면 파괴
        if (currentHealth <= 0)
        {
            DestroyGiftBox();
        }
    }
    
    // 선물상자 파괴
    private void DestroyGiftBox()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        
        // 프리팹 스폰
        SpawnRandomPrefab();
        

        
        // 선물상자 오브젝트 제거
        Destroy(gameObject, destroyDelay);
    }
    
    // 랜덤 프리팹 스폰
    private void SpawnRandomPrefab()
    {
        if (spawnPool == null || !spawnPool.HasPrefabs) return;
        
        // 스폰 풀에서 랜덤으로 프리팹 선택
        GameObject randomPrefab = spawnPool.GetRandomPrefab();
        if (randomPrefab == null) return;
        
        // 선물상자 위치에 스폰
        Vector3 spawnPosition = transform.position;
        spawnPosition.z = 0; // 2D 게임이므로 Z축 고정
        
        Instantiate(randomPrefab, spawnPosition, Quaternion.identity);
    }
    

    
    // --- IItemInteraction 구현 ---
    bool IItemInteraction.IsHeld => isHeld;
    
    public void OnPickedUp()
    {
        isHeld = true;
    }
    
    public void OnReleased()
    {
        isHeld = false;
    }
    
    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    
    public void ApplyKnockback(Vector2 force, float duration = 0f)
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }
    
    // 에디터에서 테스트
    [ContextMenu("Test Break Gift Box")]
    private void TestBreakGiftBox()
    {
        TakeDamage(1);
    }
}
