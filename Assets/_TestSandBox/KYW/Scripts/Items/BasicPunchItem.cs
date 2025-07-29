using Fusion;
using UnityEngine;

// 기본 주먹 공격 - 간소화된 버전
public class BasicPunchItem : NetworkBehaviour, IUsableItem
{
    [Header("Punch Settings")]
    [SerializeField] private float punchDistance = 1.2f;   // 펀치 거리
    [SerializeField] private float punchDuration = 0.15f;   // 펀치 지속 시간

    // 컴포넌트 참조
    private SpriteRenderer spriteRenderer;
    private Collider2D punchCollider;
    private Vector3 originalPosition;

    // 네트워크 변수
    [Networked] private TickTimer PunchTimer { get; set; } // 펀치 타이머 (TickTimer로 변경)
    [Networked] private Vector3 PunchDirection { get; set; }  // 펀치 방향

    // 로컬 캐시
    private bool isPunchActive { get { return PunchTimer.IsRunning; } } // TickTimer 기준으로 변경

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
        if(!HasInputAuthority)
        {
            Runner.SetIsSimulated(Object, true);
            // base.Object.RenderSource = RenderSource.Interpolated;
            // base.Object.ForceRemoteRenderTimeframe = true;
        }

    }

    // 🔄 실제 위치 업데이트 (물리/충돌용)
    public override void FixedUpdateNetwork()
    {
        if (PunchTimer.IsRunning)
        {
            if (PunchTimer.Expired(Runner))
            {
                PunchTimer = TickTimer.None; // 펀치 종료
            }
            else
            {
                // 실제 위치 업데이트 (충돌 처리를 위해)
                float remaining = PunchTimer.RemainingTime(Runner) ?? 0f;
                float progress = 1f - (remaining / punchDuration); // 0~1 진행도
                Vector3 punchOffset = PunchDirection * (punchDistance * progress);
                transform.localPosition = originalPosition + punchOffset;

                // 콜라이더 활성화
                if (punchCollider != null) punchCollider.enabled = true;
            }
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

        // 현재 펀치 오브젝트 위치와 마우스 위치로 방향 벡터 계산
        Vector2 worldDir = ((Vector2)mouseWorldPosition - (Vector2)transform.position).normalized;
        Vector2 localDir = (Vector2)transform.parent.InverseTransformDirection(worldDir);
        PunchDirection = localDir;

        PunchTimer = TickTimer.CreateFromSeconds(Runner, punchDuration); // TickTimer로 시작
    }

    // 사용하지 않는 인터페이스 
    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

} 