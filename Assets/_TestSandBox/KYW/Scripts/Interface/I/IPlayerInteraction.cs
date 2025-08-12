using UnityEngine;

// 플레이어/적 상호작용 인터페이스 (넉백, 데미지, 스턴, 무적, 들기)
// 아이템과 동일한 패턴으로 들림 상태를 노출하기 위해 IsHeld를 포함
public interface IPlayerInteraction : IKnockbackable, IDamageable, IStunnable, IInvincible, IHoldable
{
    // 현재 들려있는지 여부 (아이템의 IItemInteraction.IsHeld와 동일 개념)
    bool IsHeld { get; }

    // 넉백 (스턴과 함께 적용)
    new void ApplyKnockback(Vector2 force, float stunDuration = 0f);

    // 데미지
    new void TakeDamage(int damage);

    // 스턴
    new void ApplyStun(float duration);

    // 무적
    new void SetInvincible(bool value, float duration = 0f);

    // 들기 (IHoldable에서 상속)
    new void OnPickedUp();
    new void OnReleased();
} 