using Fusion;
using UnityEngine;

// 총알 공격 충돌 처리기
public class BulletAttackCollisionHandler : NetworkBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int bulletDamage = 1; // 총알 데미지
    [SerializeField] private float knockbackForce = 2f; // 넉백 힘
    [SerializeField] private float knockbackDuration = 0.5f; // 넉백 지속시간
    
    private Bullets bulletScript; // 부모 총알 스크립트 참조
    
    public override void Spawned()
    {
        // 부모 총알 스크립트 참조
        bulletScript = GetComponentInParent<Bullets>();
        Runner.SetIsSimulated(Object, true);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;
        
        // 충돌 대상 처리
        HandleCollision(other.gameObject);
    }
    
    private void HandleCollision(GameObject target)
    {
        bool shouldDespawn = false;
        
        // 플레이어, 적, NPC 충돌 처리
        var playerInteraction = target.GetComponent<IPlayerInteraction>();
        if (playerInteraction != null)
        {
            ApplyDamageAndKnockback(target, playerInteraction);
            shouldDespawn = true;
        }
        
        // 아이템 충돌 처리
        var itemInteraction = target.GetComponent<IItemInteraction>();
        if (itemInteraction != null)
        {
            ApplyKnockbackOnly(target, itemInteraction);
            shouldDespawn = true;
        }
        
        // 실제 상호작용이 일어났을 때만 despawn
        if (shouldDespawn && bulletScript != null)
        {
            Runner.Despawn(bulletScript.Object);
            Debug.Log($"총알이 {target.name}와 충돌하여 소멸됩니다.");
        }
    }
    
    private void ApplyDamageAndKnockback(GameObject target, IPlayerInteraction interaction)
    {
        // 충돌 지점에서 넉백 방향 계산
        Vector2 knockbackDirection = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        Vector2 knockbackForceVector = knockbackDirection * knockbackForce;
        
        interaction.ApplyKnockback(knockbackForceVector, knockbackDuration);
        interaction.TakeDamage(bulletDamage);
        
        Debug.Log($"총알이 {target.name}에게 {bulletDamage} 데미지를 주었습니다!");
    }
    
    private void ApplyKnockbackOnly(GameObject target, IItemInteraction interaction)
    {
        // 충돌 지점에서 넉백 방향 계산
        Vector2 knockbackDirection = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        Vector2 knockbackForceVector = knockbackDirection * knockbackForce;
        
        interaction.ApplyKnockback(knockbackForceVector, knockbackDuration);
        
        Debug.Log($"총알이 {target.name}를 넉백시켰습니다!");
    }
} 