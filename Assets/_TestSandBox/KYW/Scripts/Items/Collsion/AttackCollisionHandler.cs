using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
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

    // 스왑 예약용 로컬 상태(권한 측 전용)
    private bool pendingSwap;
    private NetworkObject pendingPlayerObj;
    private NetworkObject pendingStatueObj;
    private Vector2 pendingPlayerTargetPos;
    private Vector2 pendingStatueTargetPos;
    
    public override void Spawned()
    {
        weaponItem = GetComponentInParent<IItemInteraction>();
        Runner.SetIsSimulated(Object, true);
    }

    public override void FixedUpdateNetwork()
    {
        // no-op: 스왑은 동상(StateAuthority) RPC에서 수행
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
            // 아이템도 데미지를 받을 수 있다면 체력 감소 처리
            var damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
            }
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

		// 2) 위치 스왑: 플레이어와 동상의 위치를 바꿈
		var playerRoot = attackerRoot != null ? attackerRoot : transform;
		var playerObj = playerRoot.GetComponentInParent<NetworkObject>();
		var statueBehaviour = statue.GetComponentInParent<CharacterStatue>();
		var statueObj = statue.GetComponentInParent<NetworkObject>();
		if (playerObj == null || statueBehaviour == null || statueObj == null) return false;

		// NetworkObject 유효성 가드(초기 스폰 전 호출 방지)
		if (playerObj.Id == default || statueObj.Id == default)
		{
			Debug.LogWarning("[StatueSwap] NetworkObject not valid yet. Abort swap.");
			return false;
		}

		// 원래 플레이어 위치 저장
		Vector3 originalPlayerPos = playerRoot.position;
		Vector3 statuePos = statueObj.transform.position;

		// 플레이어를 동상 위치로 순간이동 (속도 0으로 제한)
		TeleportPlayerToPosition(playerObj, statuePos);

		// 동상 측(StateAuthority)에서 원래 플레이어 위치로 이동 요청
		statueBehaviour.Rpc_RequestMoveTo((Vector2)originalPlayerPos);

		// 쿨다운 시작
		StatueSwapCooldown = TickTimer.CreateFromSeconds(Runner, statueSwapCooldownSeconds);
		return true;
    }

	/// <summary>
	/// 플레이어를 지정 위치로 순간이동하고 속도를 0으로 제한
	/// </summary>
	private void TeleportPlayerToPosition(NetworkObject playerObj, Vector3 targetPos)
	{
		// NetworkRigidbody2D 우선 처리
		var playerNrb = playerObj.GetComponent<NetworkRigidbody2D>();
		if (playerNrb != null)
		{
			playerNrb.Teleport(targetPos, null);
			// 속도를 0으로 제한
			playerNrb.Rigidbody.linearVelocity = Vector2.zero;
			playerNrb.Rigidbody.angularVelocity = 0f;
			return;
		}

		// 일반 Rigidbody2D 처리
		var prb = playerObj.GetComponent<Rigidbody2D>();
		if (prb != null)
		{
			prb.position = (Vector2)targetPos;
			prb.linearVelocity = Vector2.zero;
			prb.angularVelocity = 0f;
			return;
		}

		// Transform만 있는 경우
		playerObj.transform.position = targetPos;
	}
} 