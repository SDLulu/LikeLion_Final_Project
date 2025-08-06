using UnityEngine;
using Fusion;

// 플라즈마 총알
public class PlasmaBullet : NetworkBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private int damage = 4;           // 총알 데미지
    [SerializeField] private float lifetime = 5f;          // 총알 수명 (초)
    [SerializeField] private LayerMask hitLayers;          // 충돌할 레이어
    [SerializeField] private GameObject hitEffectPrefab;   // 충돌 효과 프리팹
    
    [Networked] private TickTimer lifetimeTimer { get; set; }
    [Networked] private NetworkBool hasHit { get; set; } = false;

    private Rigidbody2D rb;
    private Collider2D bulletCollider;

    public override void Spawned()
    {
        // 물리 시뮬레이션 설정
        Runner.SetIsSimulated(Object, true);
        // 렌더링 소스 설정 (보간 사용)
        base.Object.RenderSource = RenderSource.Interpolated;
        // 원격 렌더링 타임프레임 강제 설정
        base.Object.ForceRemoteRenderTimeframe = true;

        rb = GetComponent<Rigidbody2D>();
        bulletCollider = GetComponent<Collider2D>();

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
        if (hasHit) return;
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

        // 플레이어에게 데미지 주기
        var playerHealth = hitObject.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        // 충돌 효과 생성
        if (hitEffectPrefab != null)
        {
            Runner.Spawn(hitEffectPrefab, transform.position, transform.rotation);
        }

        // 총알 제거
        DestroyBullet();
    }

    private void DestroyBullet()
    {
        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }

    // 데미지 설정 (필요한 경우)
    public void SetDamage(int newDamage)
    {
        if (Object.HasStateAuthority)
        {
            damage = newDamage;
        }
    }
} 