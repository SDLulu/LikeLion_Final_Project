using Fusion;
using UnityEngine;

// 기본 주먹 공격 - 간소화된 버전
public class BasicPunchItem : NetworkBehaviour, IUsableItem
{
    [Header("Punch Settings")]
    [SerializeField] private float punchDistance = 1.2f;   // 펀치 거리
    [SerializeField] private float punchDuration = 0.15f;   // 펀치 지속 시간

    // [Header("Hit Settings")]
    // [SerializeField] private float knockbackForce = 5f;
    // [SerializeField] private float knockbackDuration = 0.1f;
    // [SerializeField] private float stunDuration = 0.2f;
    // [SerializeField] private LayerMask targetLayers = -1;

    // 컴포넌트 참조
    private SpriteRenderer spriteRenderer;
    private Collider2D punchCollider;
    private Vector3 originalPosition;

    // 네트워크 변수
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

    public override void Spawned()
    {
        Runner.SetIsSimulated(Object, true);
        base.Object.RenderSource = RenderSource.Interpolated;
        base.Object.ForceRemoteRenderTimeframe = true;
    }

    // 🔄 실제 위치 업데이트 (물리/충돌용)
    public override void FixedUpdateNetwork()
    {
        if (PunchTimer > 0f)
        {
            PunchTimer -= Runner.DeltaTime;
            if (PunchTimer <= 0f)
            {
                PunchTimer = 0f;  // 펀치 종료
            }
            
            // 실제 위치 업데이트 (충돌 처리를 위해)
            float progress = PunchTimer / punchDuration;
            Vector3 punchOffset = PunchDirection * (punchDistance * progress);
            transform.localPosition = originalPosition + punchOffset;
            
            // 콜라이더 활성화
            if (punchCollider != null) punchCollider.enabled = true;
        }
        else
        {
            // 펀치 비활성 상태 - 원래 위치로
            transform.localPosition = originalPosition;
            if (punchCollider != null) punchCollider.enabled = false;
        }
    }

    // 🎨 시각적 렌더링 (보간만)
    public override void Render()
    {
        // 스프라이트 활성화/비활성화만 처리
        if (isPunchActive)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = true;
        }
        else
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
        }
    }

    // 🎮 펀치 시작
    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (isPunchActive) return;
        
        // 펀치 방향 계산 (플레이어 위치에서 마우스 방향)
        Vector2 direction = (mouseWorldPosition - playerPosition).normalized;
        
        // 네트워크 변수 설정
        PunchTimer = punchDuration;
        PunchDirection = direction;
    }

    // 사용하지 않는 인터페이스
    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

} 