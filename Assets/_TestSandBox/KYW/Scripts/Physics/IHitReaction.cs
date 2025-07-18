using UnityEngine;

// 피격(넉백, 스턴, 무적) 반응 인터페이스
// 예시: 플레이어, 적, 오브젝트 등에서 피격 효과를 통합적으로 관리할 때 사용
public interface IHitReaction
{
    // 피격 시 호출: 넉백, 스턴, 무적을 각각 원하는 값으로 적용
    // knockbackForce: 넉백 힘(방향/세기), knockbackDuration: 넉백 지속시간(초)
    // stunDuration: 조작불가(스턴) 시간(초), invincibleDuration: 무적 시간(초)
    // 예) ApplyHit(new Vector2(8, 3), 0.3f, 0.5f, 0.7f);
    void ApplyHit(Vector2 knockbackForce, float knockbackDuration, float stunDuration, float invincibleDuration);

    // 현재 스턴(조작불가) 상태인지
    bool IsStunned { get; }
    // 현재 넉백(물리충돌) 상태인지
    bool IsKnockback { get; }
    // 현재 무적(피격불가) 상태인지
    bool IsInvincible { get; }

    // 피격 상태 종료 시 호출(필요시 후처리)
    void OnHitEnd();
} 