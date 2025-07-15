using UnityEngine;

// 🚀 총알 오브젝트 (기관단총 등에서 발사)
// 충돌 시 데미지를 주고 자동으로 제거됨
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float damage = 10f; // 총알 데미지
    [SerializeField] private float speed = 15f; // 총알 속도
    [SerializeField] private float lifetime = 3f; // 생존 시간
    [SerializeField] private LayerMask targetLayers = -1; // 데미지를 줄 레이어
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffectPrefab; // 명중 이펙트
    [SerializeField] private TrailRenderer trail; // 총알 궤적
    
    private Vector2 direction; // 비행 방향
    private GameObject shooter; // 발사한 객체
    private Rigidbody2D rb;
    private bool hasHit = false; // 이미 명중했는지
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        trail = GetComponent<TrailRenderer>();
        
        // 기본 설정
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f; // 총알은 중력 영향 없음
        
        // 자동 제거 타이머
        Destroy(gameObject, lifetime);
    }
    
    // 총알 초기화 (기관단총에서 호출)
    public void Initialize(Vector2 flyDirection, float bulletSpeed, float bulletDamage, float bulletLifetime, GameObject shooterObject)
    {
        direction = flyDirection.normalized;
        speed = bulletSpeed;
        damage = bulletDamage;
        lifetime = bulletLifetime;
        shooter = shooterObject;
        
        // 속도 적용
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
        
        // 회전 (총알이 날아가는 방향으로)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        // 자동 제거 업데이트
        Destroy(gameObject, lifetime);
        
        Debug.Log($"🚀 총알 초기화: 데미지 {damage}, 속도 {speed}");
    }
    
    // 충돌 처리 (Trigger 방식)
    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject);
    }
    
    // 충돌 처리 (일반 충돌)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }
    
    // 충돌 처리 통합 함수
    private void HandleCollision(GameObject target)
    {
        if (hasHit) return; // 이미 명중한 총알은 처리 안함
        if (target == shooter) return; // 발사한 사람은 제외
        if (target == gameObject) return; // 자기 자신 제외
        
        // 레이어 체크
        if (((1 << target.layer) & targetLayers) == 0) return;
        
        hasHit = true;
        
        Debug.Log($"🎯 총알 명중: {target.name}에게 {damage} 데미지!");
        
        // 데미지 적용
        ApplyDamage(target);
        
        // 명중 이펙트
        CreateHitEffect(transform.position);
        
        // 총알 제거
        DestroyBullet();
    }
    
    // 데미지 적용
    private void ApplyDamage(GameObject target)
    {
        // TestEnemy에게 데미지
        var enemy = target.GetComponent<TestEnemy>();
        if (enemy != null)
        {
            Vector2 knockbackDirection = direction.normalized;
            enemy.TakeDamage(damage, knockbackDirection, 1f); // 총알은 약한 넉백
            return;
        }
        
        // 다른 데미지 시스템이 있다면 여기서 처리
        Debug.Log($"⚠️ {target.name}에게 데미지 시스템이 없습니다.");
    }
    
    // 명중 이펙트 생성
    private void CreateHitEffect(Vector2 position)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        else
        {
            // 기본 명중 이펙트 (간단한 파티클)
            CreateBasicHitEffect(position);
        }
    }
    
    // 기본 명중 이펙트
    private void CreateBasicHitEffect(Vector2 position)
    {
        GameObject effect = new GameObject("Bullet Hit Effect");
        effect.transform.position = position;
        
        ParticleSystem ps = effect.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.3f;
        main.startSpeed = 2f;
        main.startSize = 0.1f;
        main.startColor = Color.yellow;
        main.maxParticles = 10;
        
        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 10)
        });
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;
        
        Destroy(effect, 1f);
    }
    
    // 총알 제거
    private void DestroyBullet()
    {
        // 궤적 제거 (TrailRenderer가 있다면)
        if (trail != null)
        {
            trail.enabled = false;
        }
        
        // 충돌체 비활성화 (중복 충돌 방지)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // 0.1초 후 완전히 제거 (이펙트가 보이도록)
        Destroy(gameObject, 0.1f);
    }
    
    // 화면 밖으로 나가면 자동 제거
    private void OnBecameInvisible()
    {
        // 화면에서 벗어난 총알은 제거
        if (hasHit) return; // 이미 명중한 총알은 OnBecameInvisible 무시
        
        Debug.Log("🚀 총알이 화면을 벗어남 - 제거");
        Destroy(gameObject);
    }
    
    // 기즈모로 총알 정보 표시
    private void OnDrawGizmosSelected()
    {
        // 총알 방향 표시
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, direction * 2f);
        
        // 총알 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
} 