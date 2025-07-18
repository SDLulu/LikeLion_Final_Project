using Fusion;
using UnityEngine;

// 기본 주먹 공격 - 간소화된 버전
public class BasicPunchItem : NetworkBehaviour, IUsableItem
{
    [Header("Punch Settings")]
    [SerializeField] private float punchDistance = 1.2f;   // 펀치 거리
    [SerializeField] private float punchDuration = 0.3f;   // 펀치 지속 시간 (왕복)

    [Header("Hit Settings")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.1f;
    [SerializeField] private float stunDuration = 0.2f;
    [SerializeField] private LayerMask targetLayers = -1;

    // 컴포넌트 참조
    private SpriteRenderer spriteRenderer;
    private Collider2D punchCollider;
    private Vector3 originalPosition;

    // 네트워크 변수 - 최소한으로 줄임
    [Networked] private float PunchTimer { get; set; }        // 펀치 타이머 (0 = 비활성, >0 = 활성)
    [Networked] private Vector3 PunchDirection { get; set; }  // 펀치 방향

    // 로컬 캐시
    private bool isPunchActive => PunchTimer > 0f;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        punchCollider = GetComponent<Collider2D>();
        originalPosition = transform.localPosition;
        
        // 시작 시 비활성화
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (punchCollider != null) punchCollider.enabled = false;
    }

    // 🔄 모든 클라이언트에서 타이머 업데이트 (권한 체크 제거!)
    public override void FixedUpdateNetwork()
    {
        if (PunchTimer > 0f)
        {
            PunchTimer -= Runner.DeltaTime;
            if (PunchTimer <= 0f)
            {
                PunchTimer = 0f;  // 펀치 종료
            }
        }
    }

    // 🎨 시각적 렌더링 - 대폭 간소화
    public override void Render()
    {
        if (isPunchActive)
        {
            // 진행률 계산 (1.0 = 시작, 0.0 = 끝)
            float progress = PunchTimer / punchDuration;
            
            // 펀치 애니메이션 (앞으로 갔다가 뒤로)
            float curve = progress > 0.5f ? 
                (1f - progress) * 2f :      // 전반부: 앞으로
                progress * 2f;              // 후반부: 뒤로
            
            // 위치 적용
            Vector3 punchOffset = PunchDirection * (curve * punchDistance);
            transform.localPosition = originalPosition + punchOffset;
            
            // 시각적 활성화
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            if (punchCollider != null) punchCollider.enabled = true;
        }
        else
        {
            // 펀치 비활성 상태
            transform.localPosition = originalPosition;
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (punchCollider != null) punchCollider.enabled = false;
        }
    }

    // 🎮 펀치 시작 (InputAuthority에서만)
    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (isPunchActive || !Object.HasInputAuthority) return;
        
        // 펀치 방향 계산
        Vector2 direction = (mouseWorldPosition - playerPosition).normalized;
        
        // 네트워크 변수 설정 (모든 클라이언트에 동기화)
        PunchTimer = punchDuration;
        PunchDirection = direction;
    }

    // 사용하지 않는 인터페이스
    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    // 💥 충돌 처리 (StateAuthority에서만)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isPunchActive || !Object.HasStateAuthority) return;
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

        var damageable = other.GetComponent<IHitReaction>();
        if (damageable != null)
        {
            Vector2 hitDirection = (other.transform.position - transform.position).normalized;
            damageable.ApplyHit(
                hitDirection * knockbackForce,
                knockbackDuration,
                stunDuration,
                0.3f
            );
        }
    }
} 