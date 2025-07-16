using UnityEngine;
using System.Collections;

// 플레이어 피격(넉백, 스턴, 무적) 반응 스크립트
// 예시: 몬스터에게 맞았을 때, 돌진 공격에 당했을 때 등 다양한 상황에서
// 원하는 효과(넉백, 스턴, 무적)를 조합해서 적용할 수 있음
public class PlayerHitReaction : MonoBehaviour, IHitReaction
{
    [Header("레이어 설정")]
    public int defaultLayer;      // 평상시 레이어(플레이어끼리 충돌X 등)
    public int knockbackLayer;    // 넉백/스턴 상태 레이어(플레이어끼리 충돌O 등)
    [Header("애니메이션 트리거명")]
    public string stunAnimTrigger = "Stun";

    private Rigidbody2D rb;
    private Animator anim;

    public bool IsStunned { get; private set; }    // 현재 스턴 상태(조작불가)
    public bool IsKnockback { get; private set; }  // 현재 넉백 상태(물리충돌)
    public bool IsInvincible { get; private set; } // 현재 무적 상태(피격불가)

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        defaultLayer = gameObject.layer;
    }

    // 피격 시 원하는 효과를 조합해서 적용
    // 예시1: 돌진 몬스터에게 맞음 → ApplyHit(new Vector2(8,3), 0.3f, 0.5f, 0.7f);
    //   → 0.3초간 넉백(물리충돌), 0.5초간 스턴(조작불가), 0.7초간 무적(피격불가)
    // 예시2: 일반 몬스터에 부딪힘 → ApplyHit(Vector2.zero, 0f, 0f, 0.5f);
    //   → 넉백/스턴 없이 0.5초간 무적만 적용
    public void ApplyHit(Vector2 knockbackForce, float knockbackDuration, float stunDuration, float invincibleDuration)
    {
        // 넉백: 물리충돌/튕김 등
        if (knockbackDuration > 0 && !IsKnockback)
        {
            IsKnockback = true;
            gameObject.layer = knockbackLayer; // 플레이어끼리 충돌 가능 등
            rb.isKinematic = false;
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackForce, ForceMode2D.Impulse);
            StartCoroutine(KnockbackCoroutine(knockbackDuration));
        }
        // 스턴: 조작불가(애니메이션 등)
        if (stunDuration > 0 && !IsStunned)
        {
            IsStunned = true;
            if (anim != null && !string.IsNullOrEmpty(stunAnimTrigger))
                anim.SetTrigger(stunAnimTrigger);
            StartCoroutine(StunCoroutine(stunDuration));
        }
        // 무적: 피격불가(연타 방지)
        if (invincibleDuration > 0 && !IsInvincible)
        {
            IsInvincible = true;
            StartCoroutine(InvincibleCoroutine(invincibleDuration));
        }
    }

    // 넉백 상태 유지 후 복구
    private IEnumerator KnockbackCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;
        rb.simulated = false;
        gameObject.layer = defaultLayer; // 원래 레이어로 복귀
        IsKnockback = false;
        OnHitEnd(); // 필요시 후처리
    }

    // 스턴 상태 유지 후 복구
    private IEnumerator StunCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        IsStunned = false;
        // 필요시 애니메이션 복구
    }

    // 무적 상태 유지 후 복구
    private IEnumerator InvincibleCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        IsInvincible = false;
    }

    // 피격 상태 종료 후 후처리(필요시)
    public void OnHitEnd()
    {
        // 예: 넉백/스턴/무적이 모두 끝난 뒤 추가 처리
    }
} 