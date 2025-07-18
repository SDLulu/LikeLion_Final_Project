using UnityEngine;

// 아이템 입력 인터페이스 (간단 버전)
public interface IUsableItem
{
    // 사용 가능 여부
    bool CanUse { get; }
    // 클릭 시작
    void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition);
    // 클릭 유지
    void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition);
    // 클릭 종료
    void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition);
}
