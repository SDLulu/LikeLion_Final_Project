using Fusion;
using UnityEngine;

// 폭발형 공격 콜라이더 핸들러 (폭탄 등 AOE 전용)
// - 평소엔 비활성화
// - ActivateOnce() 호출 시 짧은 시간 동안 활성화되어 충돌 대상에 데미지/넉백 적용
public class ExplosionCollisionHandler : NetworkBehaviour
{
	[Header("Explosion Attack Settings")]
	[SerializeField] private int explosionDamage = 2;
	[SerializeField] private float explosionKnockbackForce = 8f;
	[SerializeField] private float explosionKnockbackDuration = 0.4f;
	[SerializeField] private float activeWindowSeconds = 0.05f; // 한 틱~두 틱 정도

	[Header("Collider References")]
	[SerializeField] private Collider2D[] explosionColliders; // 트리거 콜라이더들

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
}


