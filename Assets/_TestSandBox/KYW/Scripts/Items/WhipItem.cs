using Fusion;
using UnityEngine;

// 🪃 스펠렁키 채찍 아이템
// 장착해서 사용할 수 있는 무기 아이템
public class WhipItem : BaseItem
{
    [Header("Whip Settings")]
    [SerializeField] private float attackRange = 2f; // 공격 범위
    [SerializeField] private float damage = 10f; // 공격 데미지
    [SerializeField] private float knockbackForce = 5f; // 넉백 힘
    [SerializeField] private LayerMask attackTargets = -1; // 공격 대상 레이어
    
    public override void Spawned()
    {
        base.Spawned();
        
        // 기본 설정 (장착 가능)
        canBeEquipped = true;
        itemName = "채찍";
        description = "장착해서 사용할 수 있는 무기";
    }
    
    // 채찍 사용 (장착 상태: 공격, 비장착 상태: 던지기)
    public override void UseItem(SpelunkyPlayerController user, Vector2 targetPosition)
    {
        if (!IsPickedUp || CurrentHolder != user.Object.InputAuthority) return;
        
        if (IsEquipped)
        {
            // 장착 상태에서는 공격
            Vector2 attackDirection = (targetPosition - (Vector2)transform.position).normalized;
            PerformWhipAttack(user, attackDirection);
        }
        else
        {
            // 비장착 상태에서는 던지기 (기본 동작)
            base.UseItem(user, targetPosition);
        }
    }
    
    // 채찍 공격 수행
    private void PerformWhipAttack(SpelunkyPlayerController user, Vector2 direction)
    {
        Debug.Log($"🪃 {user.Object.InputAuthority}가 채찍으로 공격!");
        
        // 공격 범위 내 적들 검색
        Vector2 attackCenter = (Vector2)user.transform.position + direction * (attackRange * 0.5f);
        Collider2D[] targets = Physics2D.OverlapCircleAll(attackCenter, attackRange, attackTargets);
        
        foreach (var target in targets)
        {
            // 자기 자신은 제외
            if (target.gameObject == user.gameObject) continue;
            
            // 플레이어 공격
            var targetPlayer = target.GetComponent<SpelunkyPlayerController>();
            if (targetPlayer != null)
            {
                HitTarget(targetPlayer.gameObject, direction);
            }
            
            // 다른 대상들 (적, 오브젝트 등)
            else if (target.GetComponent<Rigidbody2D>() != null)
            {
                HitTarget(target.gameObject, direction);
            }
        }
    }
    
    // 대상 타격 처리
    private void HitTarget(GameObject target, Vector2 attackDirection)
    {
        Debug.Log($"🪃 채찍이 {target.name}을 타격!");
        
        // 넉백 적용
        var targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null)
        {
            targetRb.AddForce(attackDirection * knockbackForce, ForceMode2D.Impulse);
        }
        
        // 데미지 처리 (추후 체력 시스템 구현 시)
        // var health = target.GetComponent<HealthComponent>();
        // if (health != null) health.TakeDamage(damage);
    }
    
    protected override void OnEquip(SpelunkyPlayerController player)
    {
        Debug.Log($"🪃 {player.Object.InputAuthority}가 채찍을 장착했습니다!");
    }
    
    protected override void OnUnEquip()
    {
        Debug.Log("🪃 채찍이 장착 해제되었습니다!");
    }
    
    protected override void OnPickup(PlayerRef player)
    {
        Debug.Log($"🪃 {player}가 채찍을 주웠습니다!");
    }
    
    protected override void OnThrow(Vector2 direction)
    {
        Debug.Log("🪃 채찍이 던져졌습니다!");
    }
    
    protected override void OnDestroy()
    {
        Debug.Log("🪃 채찍이 파괴되었습니다!");
    }
    
    // 🎨 에디터에서 공격 범위 시각화
    private void OnDrawGizmosSelected()
    {
        if (IsEquipped)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
} 