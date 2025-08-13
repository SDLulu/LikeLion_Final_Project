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
    
    [Header("Tile Break / Item Cleanup Settings (단일 마스크)")]
    [SerializeField] private LayerMask tileLayerMask; // 타일 + 타일아이템/파괴오브젝트 공용 레이어
    [SerializeField] private bool breakGroundTile = false; // 타일 파괴 사용 여부
    [SerializeField] private float sweepMargin = 0.2f; // 콜라이더 경계 보정 마진
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
        
        // 접촉 기준점 계산
        Vector2 hitPoint = other.ClosestPoint(AttackCollider != null ? (Vector2)AttackCollider.bounds.center : (Vector2)transform.position);

        // 1) 영역 스윕 처리
        if (breakGroundTile) SweepAndProcessArea(hitPoint, other);
        
        // 2) 캐릭터/아이템 상호작용 처리 (동시에 수행)
        HandleCollision(other.gameObject);
    }

    private void SweepAndProcessArea(Vector2 hitPoint, Collider2D other)
    {
        // 영역: 공격 콜라이더와 상대 콜라이더의 경계를 합치고 마진만큼 확장
        Bounds region = AttackCollider != null ? AttackCollider.bounds : new Bounds(transform.position, Vector3.zero);
        region.Encapsulate(other.bounds);
        region.Expand(new Vector3(sweepMargin, sweepMargin, 0f));

        Vector2 center = region.center;
        Vector2 size = region.size;

        var hits = Physics2D.OverlapBoxAll(center, size, 0f, tileLayerMask);
        if (hits == null || hits.Length == 0) return;

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null) continue;

            // 1) 타일 파괴 (옵션)
            var tileMgr = col.GetComponent<PMK_TileRPC_Manager>();
            if (tileMgr != null)
            {
                Vector2 destroyPoint = col.ClosestPoint(hitPoint);
                tileMgr.Rpc_DestroyTile(destroyPoint);
            }

            // 2) 타일 아이템/파괴 오브젝트 정리
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