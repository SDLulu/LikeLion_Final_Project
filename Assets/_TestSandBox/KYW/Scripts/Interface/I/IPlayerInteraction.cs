using UnityEngine;

// 플레이어 상호작용 인터페이스 (넉백, 데미지, 스턴, 무적, 들기)
public interface IPlayerInteraction : IKnockbackable, IDamageable, IStunnable, IInvincible, IHoldable
{
    // 넉백 (스턴과 함께 적용)
    new void ApplyKnockback(Vector2 force, float stunDuration = 0f);

    // 데미지
    new void TakeDamage(int damage);

    // 스턴
    new void ApplyStun(float duration);

    // 무적
    new void SetInvincible(bool value, float duration = 0f);
} 