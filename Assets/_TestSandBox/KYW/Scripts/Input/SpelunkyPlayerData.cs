using Fusion;
using UnityEngine;

// 🎮 스펠렁키 플레이어 입력 데이터
// 이동 + 점프 + 아이템 상호작용 포함
public struct SpelunkyPlayerData : INetworkInput
{
    // 🏃 이동 입력 (방향키)
    public float HorizontalInput;  // 좌우 이동 (-1.0 ~ 1.0)
    public float VerticalInput;    // 위아래 입력 (-1.0 ~ 1.0) - 웅크리기용
    
    // 🖱️ 마우스 입력
    public Vector2 MouseWorldPosition; // 마우스 월드 좌표 (아이템 던지기 방향용)
    
    // 🔘 버튼 입력들
    public NetworkButtons NetworkButtons;
}

// 🎮 플레이어 입력 버튼 정의
public enum SpelunkyInputButtons
{
    None = 0,
    Jump = 1,           // 스페이스 - 점프
    PickupItem = 2,     // 스페이스 (앉은 상태) - 아이템 수집
    UseItem = 3,        // 마우스 좌클릭 - 아이템 사용
    EquipItem = 4,      // 마우스 우클릭 - 아이템 장착/해제
} 