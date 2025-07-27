using Fusion;
using UnityEngine;

// 🎮 플레이어 상태 열거형 (실제 사용되는 상태만 유지)
public enum PlayerState
{
    Normal,     // 기본 상태 (Idle, Walking, Jumping, Falling, Climbing 모두 포함)
    Ducking,    // 웅크리기 상태
    Stunned,    // 기절 상태
    Dead        // 사망 상태
}

// 🎮 플레이어 액션 상태 (동시에 여러 개 가능)
[System.Flags]
public enum PlayerAction
{
    None = 0,
    UsingItem = 1 << 0,      // 아이템 사용 중 (Hold)
    UsingSkill = 1 << 1,     // 스킬 사용 중
    Invincible = 1 << 2      // 무적 상태
}

// 🎮 상태별 행동 가능 여부 확인 (실제 사용되는 것만 유지)
public static class PlayerStateHelper
{
    // 상태별 이동 가능 여부
    public static bool CanMove(PlayerState state)
    {
        return state switch
        {
            PlayerState.Normal => true,
            PlayerState.Ducking => true,
            PlayerState.Stunned => false,
            PlayerState.Dead => false,
            _ => false
        };
    }
    
    // 상태별 점프 가능 여부
    public static bool CanJump(PlayerState state)
    {
        return state switch
        {
            PlayerState.Normal => true,
            PlayerState.Ducking => false,
            PlayerState.Stunned => false,
            PlayerState.Dead => false,
            _ => false
        };
    }
    
    // 상태별 아이템 사용 가능 여부
    public static bool CanUseItem(PlayerState state)
    {
        return state switch
        {
            PlayerState.Normal => true,
            PlayerState.Ducking => false,
            PlayerState.Stunned => false,
            PlayerState.Dead => false,
            _ => false
        };
    }
    
    // 상태별 아이템 던지기 가능 여부
    public static bool CanThrowItem(PlayerState state)
    {
        return state switch
        {
            PlayerState.Normal => true,
            PlayerState.Ducking => false,
            PlayerState.Stunned => false,
            PlayerState.Dead => false,
            _ => false
        };
    }
    
    // 상태별 아이템 줍기 가능 여부
    public static bool CanPickupItem(PlayerState state)
    {
        return state == PlayerState.Ducking;
    }
    
    // 상태별 웅크리기 가능 여부
    public static bool CanDuck(PlayerState state)
    {
        return state switch
        {
            PlayerState.Normal => true,
            PlayerState.Ducking => true,  // 🎯 Ducking 상태에서도 웅크리기 유지 가능
            PlayerState.Stunned => false,
            PlayerState.Dead => false,
            _ => false
        };
    }
}

// 🎮 상태 전환 조건 정의 (단순화)
public static class PlayerStateTransitions
{
    // 상태 전환이 가능한지 확인하는 메서드들
    public static bool CanTransitionTo(PlayerState currentState, PlayerState newState)
    {
        switch (currentState)
        {
            case PlayerState.Normal:
                return newState == PlayerState.Ducking || 
                       newState == PlayerState.Stunned ||
                       newState == PlayerState.Dead;
                       
            case PlayerState.Ducking:
                return newState == PlayerState.Normal || 
                       newState == PlayerState.Stunned ||
                       newState == PlayerState.Dead;
                       
            case PlayerState.Stunned:
                return newState == PlayerState.Normal || 
                       newState == PlayerState.Dead;
                       
            case PlayerState.Dead:
                return false; // 사망 상태에서는 다른 상태로 전환 불가
                
            default:
                return false;
        }
    }
} 