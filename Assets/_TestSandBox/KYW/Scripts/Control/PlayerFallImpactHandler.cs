using Fusion;
using UnityEngine;

// 플레이어 낙하 충격 처리: 낙하 "거리" 기반 데미지/넉백/스턴
public class PlayerFallImpactHandler : NetworkBehaviour
{
    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField, Range(0f, 1f)] private float minUpNormalY = 0.5f; // 착지 판정 노멀 Y 임계값

	[Header("Distance Thresholds (units)")]
    [SerializeField] private float safeFallDistance = 2f;     // 이 거리 이하는 무시
    [SerializeField] private float lethalFallDistance = 7f;   // 이 거리 이상은 최대 효과

    [Header("Start Detection")]
    [SerializeField] private float startFallSpeedThreshold = 1f; // vy < -threshold이면 낙하 시작으로 간주

    [Header("Effects @ t in [0..1]")]
    [SerializeField] private int minDamage = 0;
    [SerializeField] private int maxDamage = 5;
    [SerializeField] private float maxKnockbackForce = 12f; // 위쪽 방향 넉백 힘
    [SerializeField] private float knockbackDuration = 0.2f; // 넉백 유지 시간
    [SerializeField] private float maxStunSeconds = 0.6f; // 스턴 시간(가시적 경직)

    [Header("Curves (optional)")]
    [SerializeField] private bool useCurves = false;
    [SerializeField] private AnimationCurve damageCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve knockbackCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve stunCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Cooldown")]
    [SerializeField] private float cooldownSeconds = 0.2f;
    [Networked] private TickTimer Cooldown { get; set; }

    private Rigidbody2D _rb;
    private IPlayerInteraction _interaction;
    private bool _falling;
    private float _fallStartY;
	private int _fallStartTick; // 유지하되 현재 로직에선 사용하지 않음(향후 전환 대비)

    public override void Spawned()
    {
        _rb = GetComponentInParent<Rigidbody2D>();
        _interaction = GetComponentInParent<IPlayerInteraction>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (_rb == null) return;

        // 낙하 시작 감지: 일정 하강 속도 이상이면 시작으로 간주
        float vy = _rb.linearVelocity.y;
        if (!_falling && vy < -startFallSpeedThreshold)
        {
            _falling = true;
            _fallStartY = _rb.position.y;
            _fallStartTick = Runner.Tick;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasStateAuthority) return;
        if (_interaction == null) return;
        if (!Cooldown.ExpiredOrNotRunning(Runner)) return;

        // 지면 레이어 + 위쪽 노멀 체크
        if (((1 << collision.collider.gameObject.layer) & groundMask) == 0) return;
        if (collision.contactCount == 0) return;
        var contact = collision.GetContact(0);
        if (contact.normal.y < minUpNormalY) return;

        if (!_falling)
        {
            return;
        }

		// 낙하 거리 계산
        float landedY = contact.point.y;
        float fallDistance = Mathf.Max(0f, _fallStartY - landedY);

		if (fallDistance <= safeFallDistance)
		{
			ResetFallState();
			return;
		}

		float t = Mathf.Clamp01(Mathf.InverseLerp(safeFallDistance, lethalFallDistance, fallDistance));

        // 데미지 계산
        float damage01 = useCurves ? Mathf.Clamp01(damageCurve.Evaluate(t)) : t;
        int damage = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, damage01)), 0, maxDamage);

        // 넉백 계산 (위쪽 방향)
        float kb01 = useCurves ? Mathf.Clamp01(knockbackCurve.Evaluate(t)) : t;
        Vector2 knockbackForce = Vector2.up * (maxKnockbackForce * kb01);

        // 스턴 시간
        float stun01 = useCurves ? Mathf.Clamp01(stunCurve.Evaluate(t)) : t;
        float stunSeconds = maxStunSeconds * stun01;

        if (damage > 0)
        {
            _interaction.TakeDamage(damage);
        }

        // 경직/넉백: 넉백 지속 시간을 knockbackDuration로 유지
        if (knockbackForce.sqrMagnitude > 0.0001f)
        {
            _interaction.ApplyKnockback(knockbackForce, knockbackDuration);
        }

        // 간단 스턴: 스턴 시간 동안 무적 처리로 연타 방지(프로젝트 규칙에 맞춰 Invincible 사용)
        if (stunSeconds > 0f)
        {
            _interaction.SetInvincible(true, stunSeconds);
        }

        // 쿨다운 & 피크 속도 초기화
        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldownSeconds);
        ResetFallState();
    }

    private void ResetFallState()
    {
        _falling = false;
        _fallStartTick = 0;
        _fallStartY = 0f;
    }
}


