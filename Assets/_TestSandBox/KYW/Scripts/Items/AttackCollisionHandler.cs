using Fusion;
using UnityEngine;

public class AttackCollisionHandler : NetworkBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int punchDamage = 1;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.5f;
    
    [Header("Collider Reference")]
    [SerializeField] public Collider2D punchAttackCollider;
    
    private BasicPunchItem punchItem;
    
    public override void Spawned()
    {
        // 부모 오브젝트에서 BasicPunchItem 찾기
        punchItem = GetComponentInParent<BasicPunchItem>();
        Runner.SetIsSimulated(Object, true);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;
        
        if (!IsPunchActive()) return;
        
        if (other.gameObject == gameObject) return;
        
        HandlePunchCollision(other.gameObject);
    }
    
    // 외부에서 호출할 수 있는 트리거 핸들러 (PunchTriggerHandler에서 사용)
    public void HandleTriggerEnter(Collider2D other)
    {
        if (!HasStateAuthority) return;
        
        if (!IsPunchActive()) return;
        
        if (other.gameObject == gameObject) return;
        
        HandlePunchCollision(other.gameObject);
    }
    
    private void HandlePunchCollision(GameObject target)
    {
        if (IsPlayerCollision(target))
        {
            HandlePlayerCollision(target);
        }
        else if (IsEnemyCollision(target))
        {
            HandleEnemyCollision(target);
        }
        else if (IsNpcCollision(target))
        {
            HandleNpcCollision(target);
        }
        else if (IsItemCollision(target))
        {
            HandleItemCollision(target);
        }
    }
    
    private void HandlePlayerCollision(GameObject player)
    {
        var playerInteraction = player.GetComponent<IPlayerInteraction>();
        if (playerInteraction != null)
        {
            Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;
            Vector2 knockbackForceVector = knockbackDirection * knockbackForce;
            
            playerInteraction.ApplyKnockback(knockbackForceVector, knockbackDuration);
            playerInteraction.TakeDamage(punchDamage);
            playerInteraction.SetInvincible(true, knockbackDuration);
        }
    }
    
    private void HandleEnemyCollision(GameObject enemy)
    {
        var enemyInteraction = enemy.GetComponent<IPlayerInteraction>();
        if (enemyInteraction != null)
        {
            Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;
            Vector2 knockbackForceVector = knockbackDirection * knockbackForce;
            
            enemyInteraction.ApplyKnockback(knockbackForceVector, knockbackDuration);
            enemyInteraction.TakeDamage(punchDamage);
            enemyInteraction.SetInvincible(true, knockbackDuration);
        }
    }
    
    private void HandleNpcCollision(GameObject npc)
    {
        var npcInteraction = npc.GetComponent<IPlayerInteraction>();
        if (npcInteraction != null)
        {
            Vector2 knockbackDirection = (npc.transform.position - transform.position).normalized;
            Vector2 knockbackForceVector = knockbackDirection * knockbackForce;
            
            npcInteraction.ApplyKnockback(knockbackForceVector, knockbackDuration);
            npcInteraction.TakeDamage(punchDamage);
            npcInteraction.SetInvincible(true, knockbackDuration);
        }
    }
    
    private void HandleItemCollision(GameObject item)
    {
        var itemInteraction = item.GetComponent<IItemInteraction>();
        if (itemInteraction != null)
        {
            Vector2 knockbackDirection = (item.transform.position - transform.position).normalized;
            Vector2 knockbackForceVector = knockbackDirection * knockbackForce;
            
            itemInteraction.ApplyKnockback(knockbackForceVector, knockbackDuration);
        }
    }
    
    private bool IsPunchActive()
    {
        return punchItem != null && punchItem.IsPunchActive;
    }
    
    private bool IsPlayerCollision(GameObject obj)
    {
        return obj.layer == LayerMask.NameToLayer("Player");
    }
    
    private bool IsEnemyCollision(GameObject obj)
    {
        return obj.layer == LayerMask.NameToLayer("Enemy");
    }
    
    private bool IsNpcCollision(GameObject obj)
    {
        return obj.layer == LayerMask.NameToLayer("NPC");
    }
    
    private bool IsItemCollision(GameObject obj)
    {
        return obj.layer == LayerMask.NameToLayer("Item");
    }
} 