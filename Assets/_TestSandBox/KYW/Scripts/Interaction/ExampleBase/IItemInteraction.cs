using UnityEngine;

// 아이템 상호작용 인터페이스 (넉백, 들기, 사용만 지원)
public interface IItemInteraction : IKnockbackable, IHoldable, IUsableItem
{
    // 넉백
    void ApplyKnockback(Vector3 force);

    // 들기
    bool IsHoldable { get; }
    void OnPickedUp(Transform holder);
    void OnReleased();

    // 사용 (클릭 입력 기반)
    void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition);
    void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition);
    void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition);
} 