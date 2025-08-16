using Fusion;
using UnityEngine;

// 관(부활 스테이션): 파괴되면 가장 가까운 유령을 이 위치로 이동시키고 해당 플레이어를 이 자리에서 부활시킴
public class CoffinReviveStation : NetworkBehaviour, IDamageable, IItemInteraction
{
    [Header("Coffin Settings")]
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private bool destroyOnUse = true; // true면 사용 후 소멸

    [Networked] private int CurrentHealth { get; set; }
    [Networked] private NetworkBool IsHeldNet { get; set; }

    public override void Spawned()
    {
        // 네트워크 시뮬레이션 활성화
        Runner.SetIsSimulated(Object, true);
        // 서버 권한에서 초기화
        if (HasStateAuthority)
        {
            CurrentHealth = Mathf.Max(1, maxHealth);
        }
    }

    // 외부 공격 시스템과의 연결 지점
    public void TakeDamage(int damage)
    {
        if (!HasStateAuthority) return;
        if (CurrentHealth <= 0) return;

        CurrentHealth -= Mathf.Max(1, damage);
        if (CurrentHealth <= 0)
        {
            // 손에 들고 있다면 손에서 놓기
            if (IsHeldNet)
            {
                RemoveFromHand();
            }
            
            TryReviveNearestGhost();

            if (destroyOnUse)
            {
                if (Object != null)
                {
                    Runner.Despawn(Object);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    // --- IItemInteraction 구현 ---
    // 들기 상태 노출
    bool IItemInteraction.IsHeld
    {
        get { return IsHeldNet; }
    }

    // 넉백 적용 (옵션: 리지드바디가 있을 때만)
    public void ApplyKnockback(Vector2 force, float duration = 0f)
    {
        if (!HasStateAuthority) return;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }

    // 들기/놓기 이벤트
    public void OnPickedUp()
    {
        if (!HasStateAuthority) return;
        IsHeldNet = true;
    }

    public void OnReleased()
    {
        if (!HasStateAuthority) return;
        IsHeldNet = false;
    }

    // 사용 입력 (관은 사용 동작 없음; 데미지로만 부활 트리거)
    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    private void TryReviveNearestGhost()
    {
        // 가장 가까운 유령 찾기
        PlayerGhostController[] ghosts = UnityEngine.Object.FindObjectsByType<PlayerGhostController>(FindObjectsSortMode.None);
        if (ghosts == null || ghosts.Length == 0) return;

        PlayerGhostController nearest = null;
        float bestDistSqr = float.MaxValue;
        Vector3 origin = transform.position;

        for (int i = 0; i < ghosts.Length; i++)
        {
            var ghost = ghosts[i];
            if (ghost == null) continue;
            // 같은 러너의 네트워크 객체만 대상으로
            if (!ghost.HasStateAuthority) continue; // 서버에서만 제어

            float distSqr = (ghost.transform.position - origin).sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                nearest = ghost;
            }
        }

        if (nearest == null) return;

        // 유령을 관 위치로 이동
        TeleportTransform(nearest.transform, origin);

        // 유령의 소유 플레이어 찾기 (InputAuthority 기준)
        PlayerDeathHandler[] handlers = UnityEngine.Object.FindObjectsByType<PlayerDeathHandler>(FindObjectsSortMode.None);
        PlayerDeathHandler targetHandler = null;
        for (int i = 0; i < handlers.Length; i++)
        {
            var handler = handlers[i];
            if (handler == null) continue;
            if (!handler.HasStateAuthority) continue;
            if (!handler.IsDead) continue;
            if (handler.Object.InputAuthority == nearest.Object.InputAuthority)
            {
                targetHandler = handler;
                break;
            }
        }

        if (targetHandler == null) return;

        // 관 위치에서 부활
        targetHandler.ResurrectAt(origin);
    }

    private void TeleportTransform(Transform t, Vector3 pos)
    {
        if (t == null) return;
        t.position = pos;
        var rb = t.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
    
    // 손에서 놓기
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


