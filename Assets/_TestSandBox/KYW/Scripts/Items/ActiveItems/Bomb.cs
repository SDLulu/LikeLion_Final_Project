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

    protected virtual void DestroyArea()
    {
        float radius = destroyCollider2D.radius;
        Vector2 origin = transform.position;

        // 한 셀 간격마다 반복 (0.5f로 샘플링 간격 줄이기 가능)
        float step = 0.5f;

        for (float x = -radius; x <= radius; x += step)
        {
            for (float y = -radius; y <= radius; y += step)
            {
                Vector2 checkPos = origin + new Vector2(x, y);
                float distance = Vector2.Distance(origin, checkPos);

                if (distance <= radius)
                {
                    // 타일 파괴
                    Collider2D tileCol = Physics2D.OverlapPoint(checkPos, destroyLayer);
                    if (tileCol != null)
                    {
                        var tileLogic = tileCol.GetComponent<PMK_TileRPC_Manager>();
                        if (tileLogic != null)
                        {
                            tileLogic.Rpc_DestroyTile(checkPos);
                        }
                    }

                    // 타일 아이템 파괴
                    Collider2D[] hitObjects = Physics2D.OverlapPointAll(checkPos, destroyLayer);
                    foreach (var col in hitObjects)
                    {
                        if (col.CompareTag("Tileitem"))
                        {
                            var item = col.GetComponent<PMK_TileItem>();
                            if (item != null)
                            {
                                item.DestroyItem();
                            }
                        }
                        else if (col.CompareTag("BoobDestoryObj"))
                        {
                            Destroy(col.gameObject); // 오브젝트 파괴
                        }
                    }
                }
            }
        }
    }

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
        DestroyArea();

        // 폭발 충돌 처리: 하위 핸들러(예: ExplosionCollisionHandler)가 있다면 1회 활성화 메시지 전파
        gameObject.BroadcastMessage("ActivateOnce", SendMessageOptions.DontRequireReceiver);

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
