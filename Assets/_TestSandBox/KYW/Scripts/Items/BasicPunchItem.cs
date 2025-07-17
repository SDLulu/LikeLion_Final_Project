using UnityEngine;

// 기본 주먹 공격 (아이템이 없을 때 사용 가능)
public class BasicPunchItem : MonoBehaviour, IUsableItem
{
    [Header("Punch Settings")]
    [SerializeField] private float punchDistance = 1.2f;   // 펀치 거리
    [SerializeField] private float punchSpeed = 0.08f;     // 펀치 속도(왕복에 걸리는 시간)

    [Header("Hit Settings")]
    [SerializeField] private float damage = 1f;            // 데미지
    [SerializeField] private float knockbackForce = 5f;    // 넉백 힘
    [SerializeField] private float knockbackDuration = 0.1f; // 넉백 시간
    [SerializeField] private float stunDuration = 0.2f;    // 스턴 시간
    [SerializeField] private LayerMask targetLayers = -1;  // 타겟 레이어

    private SpriteRenderer spriteRenderer;
    private Transform cachedTransform;
    private Collider2D punchCollider;
    private Vector3 originalLocalPosition;
    private Vector3 punchTargetPosition;
    private bool isPunching = false;
    private float punchTimer = 0f;
    private bool isReturning = false;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        cachedTransform = transform;
        punchCollider = GetComponent<Collider2D>();
        originalLocalPosition = cachedTransform.localPosition;

        // 시작 시 비활성화
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (punchCollider != null) punchCollider.enabled = false;
    }

    private void Update()
    {
        if (!isPunching) return;

        punchTimer += Time.deltaTime;
        float t = punchTimer / punchSpeed;

        if (!isReturning)
        {
            // 앞으로 찌르기
            cachedTransform.localPosition = Vector3.Lerp(originalLocalPosition, punchTargetPosition, t);
            if (t >= 1f)
            {
                isReturning = true;
                punchTimer = 0f;
            }
        }
        else
        {
            // 뒤로 돌아오기
            cachedTransform.localPosition = Vector3.Lerp(punchTargetPosition, originalLocalPosition, t);
            if (t >= 1f)
            {
                isPunching = false;
                if (spriteRenderer != null) spriteRenderer.enabled = false;
                if (punchCollider != null) punchCollider.enabled = false;
                cachedTransform.localPosition = originalLocalPosition;
            }
        }
    }

    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (isPunching) return;
        StartPunch(mouseWorldPosition, playerPosition);
    }

    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    private void StartPunch(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 시각 효과 활성화
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (punchCollider != null) punchCollider.enabled = true;

        // 상태 초기화
        punchTimer = 0f;
        isPunching = true;
        isReturning = false;

        // 방향 계산
        Vector2 dir = (mouseWorldPosition - playerPosition).normalized;
        punchTargetPosition = originalLocalPosition + (Vector3)(dir * punchDistance);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isPunching || ((1 << other.gameObject.layer) & targetLayers) == 0) return;

        var damageable = other.GetComponent<IHitReaction>();
        if (damageable != null)
        {
            Vector2 hitDir = (other.transform.position - transform.position).normalized;
            damageable.ApplyHit(
                hitDir * knockbackForce,  // 넉백 방향과 힘
                knockbackDuration,        // 넉백 시간
                stunDuration,             // 스턴 시간
                0.3f                      // 무적 시간
            );
        }
    }
} 