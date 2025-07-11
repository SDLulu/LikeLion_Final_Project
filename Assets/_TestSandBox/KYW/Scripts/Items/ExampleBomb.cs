using UnityEngine;

// 🧨 예제 폭탄 아이템 (딜레이 폭발 버전 + 던지기 데미지)
// OnUsePress에서 타이머 시작, 몇 초 후 폭발
// 던져서 맞으면 작은 데미지, 사용하면 큰 폭발 데미지
public class ExampleBomb : UsableItemBase
{
    [Header("Bomb Settings")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionDamage = 50f;
    [SerializeField] private float explosionDelay = 3f; // 폭발 딜레이 (기본 3초)
    [SerializeField] private LayerMask targetLayers = -1;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool isArmed = false; // 폭탄이 활성화되었는지
    private Vector2 explosionPosition; // 폭발할 위치 저장
    private SpriteRenderer spriteRenderer;
    
    protected override void Awake()
    {
        base.Awake();
        // 던지기 설정은 베이스 클래스 기본값 사용 (데미지: 1, 최소속도: 3)
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    // 🔨 클릭 시작할 때 폭탄 활성화 (딜레이 후 폭발)
    public override void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (isArmed)
        {
            Debug.Log("⚠️ 폭탄이 이미 활성화되어 있습니다!");
            return;
        }
        
        Debug.Log($"🧨 {name} 폭탄 활성화! {explosionDelay}초 후 폭발합니다.");
        
        // 폭탄 활성화
        isArmed = true;
        explosionPosition = playerPosition;
        
        // 딜레이 후 폭발
        Invoke(nameof(Explode), explosionDelay);
        
        // 깜박임 효과 시작
        InvokeRepeating(nameof(BlinkEffect), 0f, 0.2f);
    }
    
    // 실제 폭발 처리
    private void Explode()
    {
        Debug.Log($"💥 {name} 폭탄 폭발!");
        
        // 깜박임 효과 중단
        CancelInvoke(nameof(BlinkEffect));
        
        // 폭발 범위 내 타겟들에게 데미지
        Collider2D[] targets = Physics2D.OverlapCircleAll(explosionPosition, explosionRadius, targetLayers);
        
        foreach (var target in targets)
        {
            if (target.transform.position != transform.position)
            {
                Debug.Log($"💥 폭발 데미지: {target.name}");
                
                // TestEnemy에게 폭발 데미지 적용
                var enemy = target.GetComponent<TestEnemy>();
                if (enemy != null)
                {
                    Vector2 targetPos = target.transform.position;
                    Vector2 knockbackDirection = (targetPos - explosionPosition).normalized;
                    float distance = Vector2.Distance(targetPos, explosionPosition);
                    float distanceRatio = 1f - (distance / explosionRadius); // 거리에 따른 데미지 감소
                    float finalDamage = explosionDamage * distanceRatio;
                    
                    enemy.TakeDamage(finalDamage, knockbackDirection, 5f); // 폭발은 강한 넉백
                }
            }
        }
        
        // 폭발 이펙트 생성
        CreateExplosionEffect(explosionPosition);
        
        // 아이템 소모 (폭탄은 일회용)
        DestroyItem();
    }
    
    // 폭탄 활성화 중 깜박임 효과
    private void BlinkEffect()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = spriteRenderer.color == Color.white ? Color.red : Color.white;
        }
    }
    
    // 던지기 데미지는 베이스 클래스 기본 구현 사용 (간소화)
    
    // Hold나 Release는 사용하지 않음 (기본 구현 사용)
    
    private void CreateExplosionEffect(Vector2 position)
    {
        // TODO: 폭발 파티클 이펙트 생성
        Debug.Log($"💥 폭발 이펙트 생성: {position}");
        
        // 임시 시각적 피드백
        GameObject explosion = new GameObject("Explosion Effect");
        explosion.transform.position = position;
        
        // 2초 후 제거
        Destroy(explosion, 2f);
    }
    
    private void DestroyItem()
    {
        // 모든 타이머 정리
        CancelInvoke();
        
        // 아이템 제거
        Debug.Log($"🗑️ {name} 소모됨");
        Destroy(gameObject);
    }
    
    // 기즈모로 폭발 범위 표시
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isArmed ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        
        // 던지기 데미지 표시 (작은 원)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
} 