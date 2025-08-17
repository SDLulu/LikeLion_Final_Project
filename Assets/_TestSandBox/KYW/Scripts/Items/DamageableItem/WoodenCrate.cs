using UnityEngine;using Fusion;

// 📦 나무상자 - 체력 1, 부서질 때 아이템/적 스폰
public class WoodenCrate : NetworkBehaviour, IDamageable, IItemInteraction
{
    [Header("📦 나무상자 설정")]
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private float destroyDelay = 0.1f;
    
    [Header("🎁 스폰 풀")]
    [SerializeField] private RandomSpawnPool spawnPool;
    
    [Networked] private int currentHealth { get; set; }
    [Networked] private bool isDestroyed { get; set; }
    [Networked] private bool isHeld { get; set; }
    
    // IDamageable 인터페이스 구현
    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }
    public bool IsDead { get { return currentHealth <= 0; } }
    
    public override void Spawned()
    {
        currentHealth = maxHealth;
    }
    
    // 데미지 받기
    public void TakeDamage(int damage)
    {
        if (!HasStateAuthority) return;
        if (isDestroyed) return;
        
        currentHealth -= damage;
        
        // 체력이 0 이하가 되면 파괴
        if (currentHealth <= 0)
        {
            RPC_DestroyCrate();
        }
    }
    
    // 상자 파괴 RPC
    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    private void RPC_DestroyCrate()
    {
        if (!HasStateAuthority) return;
        if (isDestroyed) return;
        isDestroyed = true;
        
        // 손에 들고 있다면 손에서 놓기
        if (isHeld)
        {
            RemoveFromHand();
        }
        
        // 프리팹 스폰
        SpawnRandomPrefabs();
        
        // 모든 클라이언트에 피드백 브로드캐스트
        RPC_PlayBreakFeedback(transform.position);

        // 상자 오브젝트 제거
        if (Object != null)
        {
            Runner.Despawn(Object);
        }
        else
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayBreakFeedback(Vector3 pos)
    {
        AudioManager.Inst.PlaySound("나무부서짐/돌부서짐", pos);
        EffectManager.Inst.PlayEffect("나무상자깨짐", pos);
    }
    
    // 랜덤 프리팹 스폰
    private void SpawnRandomPrefabs()
    {
        if (!HasStateAuthority) return;
        if (spawnPool == null)
        {
            Debug.LogError("[WoodenCrate] spawnPool is null");
            return;
        }
        if (!spawnPool.HasPrefabs)
        {
            Debug.LogWarning("[WoodenCrate] spawnPool has no prefabs configured");
            return;
        }
        
        // 단일 프리팹 스폰
        NetworkObject randomPrefab = spawnPool.GetRandomPrefab();
        if (randomPrefab != null)
        {
            SpawnPrefabAtPosition(randomPrefab);
        }
        else
        {
            Debug.LogWarning("[WoodenCrate] GetRandomPrefab returned null");
        }
    }
    
    // 특정 위치에 프리팹 스폰
    private void SpawnPrefabAtPosition(NetworkObject prefab)
    {
        if (prefab == null) return;
        
        // 상자 위치에 스폰
        Vector3 spawnPosition = transform.position;
        spawnPosition.z = 0; // 2D 게임이므로 Z축 고정
        
        var spawned = Runner.Spawn(prefab, spawnPosition, Quaternion.identity);
        if (spawned == null)
        {
            Debug.LogError($"[WoodenCrate] Runner.Spawn failed. Is the prefab registered in NetworkProjectConfig? Prefab={prefab.name}");
        }
    }
    
    // 손에서 놓기
    private void RemoveFromHand()
    {
        if (!Object.HasStateAuthority) return;
        var playerThrower = GetComponentInParent<PlayerObjectThrower>();
        if (playerThrower != null)
        {
            playerThrower.ReleaseObject(gameObject, false);
        }
    }
    
    // --- IItemInteraction 구현 ---
    bool IItemInteraction.IsHeld => isHeld;
    
    public void OnPickedUp()
    {
        if (!Object.HasStateAuthority) return;
        isHeld = true;
    }
    
    public void OnReleased()
    {
        if (!Object.HasStateAuthority) return;
        isHeld = false;
    }
    
    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    
    public void ApplyKnockback(Vector2 force, float duration = 0f)
    {
        if (!HasStateAuthority) return;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }
    
    // 에디터에서 테스트
    [ContextMenu("Test Break Crate")]
    private void TestBreakCrate()
    {
        TakeDamage(1);
    }
}
