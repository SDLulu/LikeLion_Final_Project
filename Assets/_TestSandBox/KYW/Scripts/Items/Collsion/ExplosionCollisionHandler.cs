using Fusion;
using UnityEngine;

// 폭발형 공격 콜라이더 핸들러 (폭탄 등 AOE 전용)
// - 평소엔 비활성화
// - ActivateOnce() 호출 시 짧은 시간 동안 활성화되어 충돌 대상에 데미지/넉백 적용
public class ExplosionCollisionHandler : NetworkBehaviour
{
	[Header("Explosion Attack Settings")]
	[SerializeField] private int explosionDamage = 5;
	[SerializeField] private float explosionKnockbackForce = 8f;
	[SerializeField] private float explosionKnockbackDuration = 0.4f;
	[SerializeField] private float activeWindowSeconds = 0.05f; // 한 틱~두 틱 정도

	[Header("Collider References")]
	[SerializeField] private Collider2D[] explosionColliders; // 트리거 콜라이더들

	[Header("Tile Destruction (Optional)")]
	[SerializeField] private bool enableTileDestruction = true; // 폭발 시 타일/파괴물 처리
	[SerializeField] private LayerMask destroyLayer; // 타일/파괴 대상 레이어(기존 Bomb.destroyLayer와 동일 용도)
	[SerializeField] private float gridSampleStep = 0.5f; // 포인트 샘플링 간격
	[SerializeField] private float fallbackRadius = 0f; // 콜라이더가 없을 때 사용할 반경(0이면 비활성)
	[SerializeField] private string tileItemTag = "Tileitem";
	[SerializeField] private string destroyObjectTag = "BoobDestoryObj"; // 기존 태그 철자 유지

	private IItemInteraction ownerItem;
	[Networked] private NetworkBool IsActive { get; set; }
	[Networked] private TickTimer activeTimer { get; set; }

	public override void Spawned()
	{
		ownerItem = GetComponentInParent<IItemInteraction>();
		Runner.SetIsSimulated(Object, true);

		SetCollidersEnabled(false);
	}

	public override void FixedUpdateNetwork()
	{
		if (!HasStateAuthority) return;
		if (IsActive && activeTimer.Expired(Runner))
		{
			IsActive = false;
			SetCollidersEnabled(false);
		}
	}

	public void ActivateOnce()
	{
		if (!HasStateAuthority) return;
		IsActive = true;
		SetCollidersEnabled(true);
		activeTimer = TickTimer.CreateFromSeconds(Runner, activeWindowSeconds);

		// 폭발 시점에 타일/파괴물 처리 한 번 수행
		if (enableTileDestruction)
		{
			DestroyTilesAndObjectsWithinExplosion();
		}
	}

	private void SetCollidersEnabled(bool enabled)
	{
		if (explosionColliders == null) return;
		foreach (var c in explosionColliders)
		{
			if (c != null) c.enabled = enabled;
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!HasStateAuthority) return;
		if (!IsActive) return;
		if (ownerItem != null && ownerItem.IsHeld) return; // 들린 상태면 무시

		GameObject target = other.gameObject;

		// 보스 충돌 처리 (우선순위 높음)
		var boss = target.GetComponent<BossBase>();
		if (boss != null)
		{
			boss.TakeDamage(explosionDamage);
		}

		// 플레이어/적/NPC
		var playerInteraction = target.GetComponent<IPlayerInteraction>();
		if (playerInteraction != null)
		{
			ApplyDamageAndKnockback(other, playerInteraction);
		}

		// 아이템
		var itemInteraction = target.GetComponent<IItemInteraction>();
		if (itemInteraction != null)
		{
			ApplyKnockbackOnly(other, itemInteraction);
            // 아이템도 데미지를 받을 수 있다면 체력 감소 처리 (폭발 데미지)
            var damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(explosionDamage);
            }
		}
	}

	private void ApplyDamageAndKnockback(Collider2D hitCollider, IPlayerInteraction interaction)
	{
		Vector2 origin = explosionColliders != null && explosionColliders.Length > 0 && explosionColliders[0] != null
			? (Vector2)explosionColliders[0].bounds.center
			: (Vector2)transform.position;
		Vector2 knockbackDirection = ((Vector2)hitCollider.bounds.center - origin).normalized;
		Vector2 force = knockbackDirection * explosionKnockbackForce;

		interaction.ApplyKnockback(force, explosionKnockbackDuration);
		interaction.TakeDamage(explosionDamage);
		interaction.SetInvincible(true, explosionKnockbackDuration);
	}

	private void ApplyKnockbackOnly(Collider2D hitCollider, IItemInteraction interaction)
	{
		Vector2 origin = explosionColliders != null && explosionColliders.Length > 0 && explosionColliders[0] != null
			? (Vector2)explosionColliders[0].bounds.center
			: (Vector2)transform.position;
		Vector2 knockbackDirection = ((Vector2)hitCollider.bounds.center - origin).normalized;
		Vector2 force = knockbackDirection * explosionKnockbackForce;

		interaction.ApplyKnockback(force, explosionKnockbackDuration);
	}

	// --- Tile/Object destruction helpers ---
	private void DestroyTilesAndObjectsWithinExplosion()
	{
		// 1) 콜라이더 기반 샘플링: 각 폭발 콜라이더의 AABB 안을 gridSampleStep 간격으로 탐색
		bool usedColliderArea = false;
		if (explosionColliders != null && explosionColliders.Length > 0)
		{
			foreach (var col in explosionColliders)
			{
				if (col == null) continue;
				SampleAndDestroyInCollider(col);
				usedColliderArea = true;
			}
		}

		// 2) 콜라이더가 없다면, 선택적으로 폭발체 중심 원형 반경 샘플링
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
}


