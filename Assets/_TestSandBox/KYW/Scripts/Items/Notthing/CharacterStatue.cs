using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

// 캐릭터 동상: 때리면 플레이어 스킨을 바꾸는 대상
public class CharacterStatue : NetworkBehaviour, IItemInteraction
{
	public override void Spawned()
	{
		// 원격 클라이언트 렌더 보간 설정(시각 반영 개선)
		Object.RenderSource = RenderSource.Interpolated;
		Object.ForceRemoteRenderTimeframe = true;
	}

	[Header("Statue Skin Data")]
	[SerializeField] private string skinKey = "";

	public string SkinKey { get { return skinKey; } }

	[Header("Movement Root")]
	[SerializeField, Tooltip("위치 스왑 시 실제로 이동시킬 루트(전체 동상 오브젝트). 비워두면 현재 오브젝트를 이동")]
	private Transform statueRoot;

	public Transform RootToMove
	{
		get { return statueRoot != null ? statueRoot : transform; }
	}

	// IItemInteraction 구현 (동상은 들 수 없음 → 픽업 후보에서 제외되도록 true 반환)
	public bool IsHeld { get { return true; } }

	public void ApplyKnockback(Vector2 force, float duration = 0f)
	{
		if (!HasStateAuthority) return;
		var rb = GetComponent<Rigidbody2D>();
		if (rb != null)
		{
			rb.AddForce(force, ForceMode2D.Impulse);
		}
	}

	public void OnPickedUp() { }
	public void OnReleased() { }

	public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
	public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
	public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

	// 공격 측에서 호출: 동상을 지정 위치로 이동 요청 (상태권한에서 수행)
	[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
	public void Rpc_RequestMoveTo(Vector2 targetPos)
	{
		// 초기화 가드
		if (Object == null) return;
		if (!HasStateAuthority) return;
		
		// 동상을 지정 위치로 순간이동하고 속도를 0으로 제한
		TeleportStatueToPosition(targetPos);
	}

	/// <summary>
	/// 동상을 지정 위치로 순간이동하고 속도를 0으로 제한
	/// </summary>
	private void TeleportStatueToPosition(Vector2 targetPos)
	{
		// NetworkRigidbody2D 우선 처리
		var nrb = Object.GetComponent<NetworkRigidbody2D>();
		if (nrb != null)
		{
			nrb.Teleport(new Vector3(targetPos.x, targetPos.y, transform.position.z), null);
			// 속도를 0으로 제한
			nrb.Rigidbody.linearVelocity = Vector2.zero;
			nrb.Rigidbody.angularVelocity = 0f;
			return;
		}

		// 일반 Rigidbody2D 처리
		var rb = GetComponent<Rigidbody2D>();
		if (rb != null)
		{
			rb.position = targetPos;
			rb.linearVelocity = Vector2.zero;
			rb.angularVelocity = 0f;
			return;
		}

		// Transform만 있는 경우
		transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
	}
}


