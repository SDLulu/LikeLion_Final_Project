using Fusion;
using UnityEngine;

// 🎮 간단한 스펠렁키 플레이어 입력 데이터
// 좌우이동 + 상하입력 + 점프만 포함
public struct SpelunkyPlayerData : INetworkInput
{
    // 🏃 이동 입력 (방향키)
    public float HorizontalInput;  // 좌우 이동 (-1.0 ~ 1.0)
    public float VerticalInput;    // 위아래 입력 (-1.0 ~ 1.0) - 웅크리기용
    
    // 🔘 버튼 입력들
    public NetworkButtons NetworkButtons;
}

// 🎮 간단한 플레이어 입력 버튼 정의
public enum SpelunkyInputButtons
{
    None = 0,
    Jump = 1,           // 스페이스 - 점프
} 