using System.Collections;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Collections.Unicode;

public class Bomb : NetworkBehaviour, IItemInteraction
{
    [Header("기본 설정")]
    [SerializeField] protected GameObject destroyArea; // 파괴 영역 오브젝트
    [SerializeField] protected CircleCollider2D destroyCollider2D; // 파괴 범위 콜라이더
    [SerializeField] protected LayerMask destroyLayer; // 파괴할 타일 레이어

    [Header("파괴 설정")]
    [SerializeField] protected float deleteRadius = 3f; // 파괴 반경
    [SerializeField] protected float delayBeforeBoom = 3f; // 폭발 전 대기 시간

    // --- 네트워크 상태 ---
    [Networked] private NetworkBool IsHeld { get; set; }
    [Networked] private NetworkBool IsArmed { get; set; }
    [Networked] private TickTimer explosionTimer { get; set; }

    // 파괴 로직은 ExplosionCollisionHandler로 이관

    // --- NetworkBehaviour 구현 ---
    public override void Spawned()
    {
        Runner.SetIsSimulated(Object, true);
        if (destroyCollider2D != null)
        {
            destroyCollider2D.radius = deleteRadius;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (IsArmed && explosionTimer.Expired(Runner))
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (destroyArea != null)
        {
            destroyArea.SetActive(true);
        }
        // 파괴 로직은 핸들러에서 수행

        // 폭발 충돌/파괴 처리: 하위 핸들러(ExplosionCollisionHandler) 1회 활성화
        var explosionHandler = GetComponentInChildren<ExplosionCollisionHandler>();
        if (explosionHandler != null)
        {
            explosionHandler.ActivateOnce();
        }

        // 손에서 제거 후 Despawn
        RemoveFromHand();
        if (Object != null)
        {
            Runner.Despawn(Object);
        }
    }

    // --- IItemInteraction 구현 ---
    bool IItemInteraction.IsHeld => IsHeld;

    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (!Object.HasStateAuthority) return;

        if (IsArmed) return; // 이미 설치됨

        // 타이머 장전
        IsArmed = true;
        explosionTimer = TickTimer.CreateFromSeconds(Runner, delayBeforeBoom);
    }

    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 홀드 기능 없음
    }

    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 릴리즈 기능 없음
    }

    public void OnPickedUp()
    {
        if (!Object.HasStateAuthority) return;
        IsHeld = true;
    }

    public void OnReleased()
    {
        if (!Object.HasStateAuthority) return;
        IsHeld = false;
    }

    public void ApplyKnockback(Vector2 force, float duration = 0f)
    {
        if (!HasStateAuthority) return;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }

    public bool CanBeHeld => true;
    public bool CanBeThrown => true;

    private void RemoveFromHand()
    {
        if (!Object.HasStateAuthority) return;
        var playerThrower = GetComponentInParent<PlayerObjectThrower>();
        if (playerThrower != null)
        {
            playerThrower.ReleaseObject(gameObject, false);
        }
    }
}
