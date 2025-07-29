using UnityEngine;

// 적/NPC 상호작용 인터페이스 (넉백, 데미지, 스턴, 무적, 들기(스턴시에만))
public interface IEnemyInteraction : IKnockbackable, IDamageable, IStunnable, IInvincible, IHoldable
{
    // 넉백
    void ApplyKnockback(Vector2 force, float duration = 0f);

    // 데미지
    void TakeDamage(int damage);

    // 스턴
    void ApplyStun(float duration);

    // 무적
    bool IsInvincible { get; }
    void SetInvincible(bool value, float duration = 0f);

    // 들기: 스턴 상태에서만 가능
    bool IsHoldable { get; }
    void OnPickedUp();
    void OnReleased();
} 