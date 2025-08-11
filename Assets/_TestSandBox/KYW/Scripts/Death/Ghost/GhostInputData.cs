using Fusion;
using UnityEngine;

// 👻 유령 입력 데이터 (Input 전용)
public struct GhostInputData : INetworkInput
{
    // 🏃 이동 입력 (방향키)
    public float HorizontalInput;  // 좌우 이동 (-1.0 ~ 1.0)
    public float VerticalInput;    // 위아래 이동 (-1.0 ~ 1.0)
    
    // 🖱️ 마우스 입력
    public Vector2 MouseWorldPosition; // 마우스 월드 좌표 (회전 방향용)
    
    // 🔘 버튼 입력들 (Fusion 2 공식 방식 - NetworkButtons로 통합)
    public NetworkButtons NetworkButtons;
}

// 👻 유령 입력 버튼 정의
public enum GhostInputButtons
{
    None = 0,
    LeftClick = 1,    // 마우스 좌클릭 - 입김
    Space = 2,        // 스페이스 - 대쉬
} 