using UnityEngine;

// 플레이어 상호작용 인터페이스 (넉백, 데미지, 스턴, 무적, 들기)
public interface IPlayerInteraction : IKnockbackable, IDamageable, IStunnable, IInvincible
{
    // 넉백
    void ApplyKnockback(Vector3 force);

    // 데미지
    void TakeDamage(int damage);

    // 스턴
    void ApplyStun(float duration);

    // 무적
    void SetInvincible(bool value, float duration = 0f);
} 