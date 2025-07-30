using Fusion;
using UnityEngine;

public class AttackCollisionHandler : NetworkBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.5f;
    
    [Header("Collider Reference")]
    [SerializeField] public Collider2D AttackCollider;
    
    private IItemInteraction weaponItem;
    
    public override void Spawned()
    {
        weaponItem = GetComponentInParent<IItemInteraction>();
        Runner.SetIsSimulated(Object, true);
        
        Debug.Log($"[AttackCollisionHandler] Spawned - weaponItem: {weaponItem?.GetType().Name ?? "null"}");
        if (weaponItem != null)
        {
            Debug.Log($"[AttackCollisionHandler] weaponItem.IsHeld: {weaponItem.IsHeld}");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[AttackCollisionHandler] OnTriggerEnter2D 호출됨: {other.name}");
        
        if (!HasStateAuthority) 
        {
            Debug.Log($"[AttackCollisionHandler] StateAuthority 없음");
            return;
        }
        
        // 자기 자신 또는 같은 아이템의 다른 콜라이더와의 충돌 방지
        if (IsSameItem(other.gameObject)) 
        {
            Debug.Log($"[AttackCollisionHandler] 같은 아이템과 충돌 무시: {other.name}");
            return;
        }
        
        if (weaponItem == null) 
        {
            Debug.Log($"[AttackCollisionHandler] weaponItem이 null");
            return;
        }
        if (!weaponItem.IsHeld) 
        {
            Debug.Log($"[AttackCollisionHandler] weaponItem.IsHeld = {weaponItem.IsHeld} (들려있지 않음)");
            return;
        }
        
        Debug.Log($"[AttackCollisionHandler] 충돌 감지: {other.name}, 레이어: {other.gameObject.layer}");
        HandleCollision(other.gameObject);
    }
    
    // 같은 아이템인지 확인 (자기 자신, 부모, 자식 모두 체크)
    private bool IsSameItem(GameObject other)
    {
        // 자기 자신
        if (other == gameObject) return true;
        
        // 부모가 같은지 확인 (같은 아이템의 다른 콜라이더)
        if (weaponItem != null)
        {
            var otherItem = other.GetComponentInParent<IItemInteraction>();
            if (otherItem != null && otherItem == weaponItem) return true;
        }
        
        // 자식인지 확인
        if (other.transform.IsChildOf(transform)) return true;
        
        // 부모인지 확인
        if (transform.IsChildOf(other.transform)) return true;
        
        return false;
    }
    
    private void HandleCollision(GameObject target)
    {
        Debug.Log($"[AttackCollisionHandler] 충돌 처리 시작: {target.name}");
        
        // 플레이어, 적, NPC는 모두 IPlayerInteraction 사용 (나중에 적/NPC 처리를 다르게 할 수 있음)
        var playerInteraction = target.GetComponent<IPlayerInteraction>();
        if (playerInteraction != null)
        {
            Debug.Log($"[AttackCollisionHandler] 플레이어 상호작용 발견: {playerInteraction.GetType().Name}");
            ApplyDamageAndKnockback(target, playerInteraction);
            return;
        }
        
        // 아이템 충돌 처리
        var itemInteraction = target.GetComponent<IItemInteraction>();
        if (itemInteraction != null)
        {
            Debug.Log($"[AttackCollisionHandler] 아이템 상호작용 발견: {itemInteraction.GetType().Name}");
            ApplyKnockbackOnly(target, itemInteraction);
        }
        
        Debug.Log($"[AttackCollisionHandler] 상호작용 컴포넌트를 찾을 수 없음: {target.name}");
    }
    
    private void ApplyDamageAndKnockback(GameObject target, IPlayerInteraction interaction)
    {
        // 콜라이더의 실제 중심점을 기준으로 넉백 방향 계산 (오프셋 고려)
        Vector2 knockbackDirection = ((Vector2)target.transform.position - (Vector2)AttackCollider.bounds.center).normalized;
        Vector2 knockbackForceVector = knockbackDirection * knockbackForce;
        
        interaction.ApplyKnockback(knockbackForceVector, knockbackDuration);
        interaction.TakeDamage(attackDamage);
        interaction.SetInvincible(true, knockbackDuration);
    }
    
    private void ApplyKnockbackOnly(GameObject target, IItemInteraction interaction)
    {
        // 콜라이더의 실제 중심점을 기준으로 넉백 방향 계산 (오프셋 고려)
        Vector2 knockbackDirection = ((Vector2)target.transform.position - (Vector2)AttackCollider.bounds.center).normalized;
        Vector2 knockbackForceVector = knockbackDirection * knockbackForce;
        
        interaction.ApplyKnockback(knockbackForceVector, knockbackDuration);
    }
    

} 