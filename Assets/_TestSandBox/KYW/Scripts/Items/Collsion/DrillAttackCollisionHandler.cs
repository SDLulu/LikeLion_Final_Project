using Fusion;
using UnityEngine;

// 드릴 전용 공격 충돌 핸들러 (최적화 버전)
public class DrillAttackCollisionHandler : NetworkBehaviour
{
    [Header("Drill Attack Settings")]
    [SerializeField] private int drillDamage = 1; // 드릴 데미지 (낮지만 연속 공격)
    [SerializeField] private float drillKnockbackForce = 2f; // 드릴 넉백 힘 (약함)
    [SerializeField] private float drillKnockbackDuration = 0.3f; // 드릴 넉백 지속시간
    
    [Header("Collider Reference")]
    [SerializeField] private Collider2D attackCollider; // 단일 콜라이더 (PolygonCollider2D 권장)
    
    private IItemInteraction weaponItem;
    
    public override void Spawned()
    {
        weaponItem = GetComponentInParent<IItemInteraction>();
        Runner.SetIsSimulated(Object, true);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;
        
        if (IsSameItem(other.gameObject)) return;
        
        if (weaponItem == null) return;
        if (!weaponItem.IsHeld) return;
        
        HandleCollision(other.gameObject);
    }
    
    private bool IsSameItem(GameObject other)
    {
        if (other == gameObject) return true;
        
        if (weaponItem != null)
        {
            var otherItem = other.GetComponentInParent<IItemInteraction>();
            if (otherItem != null && otherItem == weaponItem) return true;
        }
        
        if (other.transform.IsChildOf(transform)) return true;
        
        if (transform.IsChildOf(other.transform)) return true;
        
        return false;
    }
    
    private void HandleCollision(GameObject target)
    {
        var playerInteraction = target.GetComponent<IPlayerInteraction>();
        if (playerInteraction != null)
        {
            ApplyDamageAndKnockback(target, playerInteraction);
            return;
        }
        
        var itemInteraction = target.GetComponent<IItemInteraction>();
        if (itemInteraction != null)
        {
            ApplyKnockbackOnly(target, itemInteraction);
        }
    }
    
    private void ApplyDamageAndKnockback(GameObject target, IPlayerInteraction interaction)
    {
        Vector2 knockbackDirection = ((Vector2)target.transform.position - (Vector2)attackCollider.bounds.center).normalized;
        Vector2 knockbackForceVector = knockbackDirection * drillKnockbackForce;
        
        interaction.ApplyKnockback(knockbackForceVector, drillKnockbackDuration);
        interaction.TakeDamage(drillDamage);
        
        Debug.Log($"드릴이 {target.name}에게 {drillDamage} 데미지를 주었습니다!");
    }
    
    private void ApplyKnockbackOnly(GameObject target, IItemInteraction interaction)
    {
        Vector2 knockbackDirection = ((Vector2)target.transform.position - (Vector2)attackCollider.bounds.center).normalized;
        Vector2 knockbackForceVector = knockbackDirection * drillKnockbackForce;
        
        interaction.ApplyKnockback(knockbackForceVector, drillKnockbackDuration);
        
        Debug.Log($"드릴이 {target.name}를 넉백시켰습니다!");
    }
    
    // 콜라이더 활성화/비활성화 메서드 (Drill.cs에서 호출)
    public void SetColliderEnabled(bool enabled)
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = enabled;
        }
    }
} 