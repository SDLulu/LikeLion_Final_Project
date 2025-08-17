using UnityEngine;
using Fusion;

// 플라즈마 총알
public class PlasmaBullet : NetworkBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float lifetime = 5f;          // 총알 수명 (초)
    [SerializeField] private LayerMask hitLayers;          // 충돌할 레이어
    [SerializeField] private GameObject hitEffectPrefab;   // 충돌 효과 프리팹
    [SerializeField] private float explosionDespawnDelay = 0.06f; // 폭발 창 종료 후 소멸 딜레이
    
    [Networked] private TickTimer lifetimeTimer { get; set; }
    [Networked] private NetworkBool hasHit { get; set; } = false;
    [Networked] private TickTimer explodeDespawnTimer { get; set; }

    private Rigidbody2D rb;
    private Collider2D bulletCollider;
    private ExplosionCollisionHandler explosionHandler;

    public override void Spawned()
    {
        // 물리 시뮬레이션 설정
        Runner.SetIsSimulated(Object, true);

        rb = GetComponent<Rigidbody2D>();
        bulletCollider = GetComponent<Collider2D>();
        explosionHandler = GetComponentInChildren<ExplosionCollisionHandler>();

        // 수명 타이머 시작
        if (Object.HasStateAuthority)
        {
            lifetimeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // 수명 체크
        if (lifetimeTimer.Expired(Runner))
        {
            DestroyBullet();
            return;
        }

        // 이미 충돌했다면 더 이상 처리하지 않음
        if (hasHit)
        {
            // 폭발 창이 끝났으면 소멸
            if (explodeDespawnTimer.IsRunning && explodeDespawnTimer.Expired(Runner))
            {
                DestroyBullet();
            }
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!Object.HasStateAuthority) return;
        if (hasHit) return;

        // 충돌한 오브젝트가 설정된 레이어에 있는지 확인
        if (((1 << other.gameObject.layer) & hitLayers) != 0)
        {
            HandleHit(other);
        }
    }

    private void HandleHit(Collider2D hitObject)
    {
        hasHit = true;
        
        // 폭발 처리: 범위 데미지/넉백/땅 파괴는 ExplosionCollisionHandler가 수행
        if (explosionHandler != null)
        {
            explosionHandler.ActivateOnce();
        }

        // 충돌 효과 생성
        if (hitEffectPrefab != null)
        {
            Runner.Spawn(hitEffectPrefab, transform.position, transform.rotation);
        }

        // 폭발 소리와 이펙트 재생 (Bomb과 동일)
        RPC_PlayExplosionFeedback(transform.position);

        // 총알 비활성화 후 폭발 창 종료 시점에 소멸
        if (bulletCollider != null) bulletCollider.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        explodeDespawnTimer = TickTimer.CreateFromSeconds(Runner, explosionDespawnDelay);
    }

    private void DestroyBullet()
    {
        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }

    // --- RPC 메서드들 ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayExplosionFeedback(Vector3 pos)
    {
        // 폭발 소리와 이펙트 재생 (Bomb과 동일)
        AudioManager.Inst.PlaySound("폭발2", pos);
        EffectManager.Inst.PlayEffect("폭탄", pos);
    }
} 