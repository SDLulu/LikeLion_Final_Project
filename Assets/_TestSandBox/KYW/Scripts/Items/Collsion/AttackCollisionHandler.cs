using System.Collections.Generic;
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
    
    [Header("Tile Destruction (Explosion-style)")]
    [SerializeField] private bool enableTileDestruction = true; // 폭발 방식의 타일/오브젝트 처리 사용
    [SerializeField] private LayerMask destroyLayer; // 타일/파괴 대상 레이어
    [SerializeField] private float gridSampleStep = 0.5f; // 영역 내 포인트 샘플링 간격
    [SerializeField] private float fallbackRadius = 0f; // 콜라이더가 없을 때 사용할 반경(0이면 비활성)
    [SerializeField] private string tileItemTag = "Tileitem";
    [SerializeField] private string destroyObjectTag = "BoobDestoryObj";

    [Header("Statue Hit Control")]
    [SerializeField] private float statueSwapCooldownSeconds = 0.15f;
    [Networked] private TickTimer StatueSwapCooldown { get; set; }
    
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
        // 1) 타일/파괴 오브젝트 처리 (폭발 방식)
        if (enableTileDestruction)
        {
            DestroyTilesAndObjectsWithinAttackArea();
        }
        
        // 2) 캐릭터/아이템 상호작용 처리
        HandleCollision(other.gameObject);
    }

    // 폭발 핸들러와 동일한 방식: 공격 콜라이더 영역을 샘플링하며 파괴/정리
    private void DestroyTilesAndObjectsWithinAttackArea()
    {
        bool usedColliderArea = false;
        if (AttackCollider != null)
        {
            SampleAndDestroyInCollider(AttackCollider);
            usedColliderArea = true;
        }

        // 콜라이더가 없고 폴백 반경이 지정되어 있으면 원형 샘플링
        if (!usedColliderArea && fallbackRadius > 0f)
        {
            SampleAndDestroyInCircle((Vector2)transform.position, fallbackRadius);
        }
    }

    private void SampleAndDestroyInCollider(Collider2D areaCollider)
    {
        Bounds b = areaCollider.bounds;
        float step = Mathf.Max(0.05f, gridSampleStep);
        for (float x = b.min.x; x <= b.max.x; x += step)
        {
            for (float y = b.min.y; y <= b.max.y; y += step)
            {
                Vector2 p = new Vector2(x, y);
                // 실제 콜라이더 내부만 처리
                if (!areaCollider.OverlapPoint(p)) continue;
                ProcessDestroyAtPoint(p);
            }
        }
    }

    private void SampleAndDestroyInCircle(Vector2 origin, float radius)
    {
        float step = Mathf.Max(0.05f, gridSampleStep);
        for (float x = -radius; x <= radius; x += step)
        {
            for (float y = -radius; y <= radius; y += step)
            {
                Vector2 p = origin + new Vector2(x, y);
                if (Vector2.SqrMagnitude(new Vector2(x, y)) > radius * radius) continue;
                ProcessDestroyAtPoint(p);
            }
        }
    }

    private void ProcessDestroyAtPoint(Vector2 point)
    {
        // 셀 기반 RPC로 일원화: 히트 지점 기준 가장 가까운 1셀 파괴 + 해당 셀 아이템/파괴오브젝트 정리
        var mgr = UnityEngine.Object.FindAnyObjectByType<PMK_TileRPC_Manager>();
        if (mgr != null)
        {
            mgr.Rpc_DestroyTileAndCleanup(point);
            return;
        }
        // 폴백: 매니저를 찾지 못한 경우 기존 포인트 기반 처리를 최소한으로 수행
        Collider2D tileCol = Physics2D.OverlapPoint(point, destroyLayer);
        if (tileCol != null)
        {
            var tileLogic = tileCol.GetComponent<PMK_TileRPC_Manager>();
            if (tileLogic != null)
            {
                Vector3Int cellPos = Vector3Int.FloorToInt(point);
                tileLogic.Rpc_DestroyTile(cellPos);
            }
        }
        Collider2D[] hits = Physics2D.OverlapPointAll(point, destroyLayer);
        if (hits == null) return;
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null) continue;
            if (col.CompareTag(tileItemTag))
            {
                var item = col.GetComponent<PMK_TileItem>();
                if (item != null) item.DestroyItem();
            }
            else if (col.CompareTag(destroyObjectTag))
            {
                UnityEngine.Object.Destroy(col.gameObject);
            }
        }
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
            TryHandleStatueSwap(target);
        }
    }

    private void ApplyDamageAndKnockback(GameObject target, IPlayerInteraction interaction)
    {
        Vector2 knockbackDirection = ((Vector2)target.transform.position - (Vector2)AttackCollider.bounds.center).normalized;
        Vector2 knockbackForceVector = knockbackDirection * knockbackForce;
        
        interaction.ApplyKnockback(knockbackForceVector, knockbackDuration);
        interaction.TakeDamage(attackDamage);
        interaction.SetInvincible(true, knockbackDuration);
    }
    
    private void ApplyKnockbackOnly(GameObject target, IItemInteraction interaction)
    {
        Vector2 knockbackDirection = ((Vector2)target.transform.position - (Vector2)AttackCollider.bounds.center).normalized;
        Vector2 knockbackForceVector = knockbackDirection * knockbackForce;
        
        interaction.ApplyKnockback(knockbackForceVector, knockbackDuration);
    }
        // 동상 추가 처리: 타겟이 동상이면 스킨 변경 + 위치 스왑 (쿨다운 중이면 무시)
    private bool TryHandleStatueSwap(GameObject target)
    {
        var statue = target.GetComponent<CharacterStatue>();
        if (statue == null) return false;
        if (!StatueSwapCooldown.ExpiredOrNotRunning(Runner)) return false;

        var attackerRoot = transform.root;
        var appearance = attackerRoot != null ? attackerRoot.GetComponentInChildren<PlayerAppearance>() : null;
        if (appearance == null || !appearance.HasStateAuthority) return false;

        // 1) 스킨 변경 (네트워크 값만 변경)
        appearance.ChangeSkin(statue.SkinKey);

        // 2) 위치 스왑 (StateAuthority에서만 적용)
        var playerTransform = attackerRoot;
        var statueTransform = (statue.RootToMove != null ? statue.RootToMove : statue.transform);

        Vector3 playerPos = playerTransform.position;
        Vector3 statuePos = statueTransform.position;

        var playerRb = playerTransform.GetComponent<Rigidbody2D>();
        var statueRb = statueTransform.GetComponent<Rigidbody2D>();

        // 속도 정지
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
        }
        if (statueRb != null)
        {
            statueRb.linearVelocity = Vector2.zero;
            statueRb.angularVelocity = 0f;
        }

        // 위치 교환 (Z는 각자 유지)
        if (playerRb != null)
            playerRb.position = new Vector2(statuePos.x, statuePos.y);
        else
            playerTransform.position = new Vector3(statuePos.x, statuePos.y, playerPos.z);

        if (statueRb != null)
            statueRb.position = new Vector2(playerPos.x, playerPos.y);
        else
            statueTransform.position = new Vector3(playerPos.x, playerPos.y, statuePos.z);

        // 추가 트리거 방지용 쿨다운 시작
        StatueSwapCooldown = TickTimer.CreateFromSeconds(Runner, statueSwapCooldownSeconds);
        return true;
    }
} 