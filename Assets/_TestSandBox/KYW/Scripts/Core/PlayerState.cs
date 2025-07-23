using Fusion;
using UnityEngine;

// 🎮 플레이어 상태 열거형
// 기본적인 플레이어 상태 (상호 배타적)
public enum PlayerState
{
    Idle,           // 기본 상태 (땅에 서있음)
    Walking,        // 걷는 중
    Ducking,        // 웅크린 상태
    Jumping,        // 점프 중
    Falling,        // 낙하 중
    Climbing,       // 사다리 오르는 중
    Stunned,        // 기절 상태
    Dead            // 사망 상태 (유령 프리팹으로 전환 예정)
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

// 🎮 상태별 행동 가능 여부 확인
public static class PlayerStateHelper
{
    // 상태별 이동 가능 여부
    public static bool CanMove(PlayerState state)
    {
        return state switch
        {
            PlayerState.Idle => true,
            PlayerState.Walking => true,
            PlayerState.Ducking => true,
            PlayerState.Jumping => true,
            PlayerState.Falling => true,
            PlayerState.Climbing => true,
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
            PlayerState.Idle => true,
            PlayerState.Walking => true,
            PlayerState.Ducking => false,
            PlayerState.Jumping => false,
            PlayerState.Falling => false,
            PlayerState.Climbing => true,
            PlayerState.Stunned => false,
            PlayerState.Dead => false,
            _ => false
        };
    }
    
    // 상태별 사다리 오르기 가능 여부
    public static bool CanClimb(PlayerState state)
    {
        return state switch
        {
            PlayerState.Idle => true,
            PlayerState.Walking => true,
            PlayerState.Ducking => false,
            PlayerState.Jumping => true,
            PlayerState.Falling => true,
            PlayerState.Climbing => true,
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
            PlayerState.Idle => true,
            PlayerState.Walking => true,
            PlayerState.Ducking => false,
            PlayerState.Jumping => true,
            PlayerState.Falling => true,
            PlayerState.Climbing => true,
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
            PlayerState.Idle => true,
            PlayerState.Walking => true,
            PlayerState.Ducking => false,
            PlayerState.Jumping => true,
            PlayerState.Falling => true,
            PlayerState.Climbing => true,
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
            PlayerState.Idle => true,
            PlayerState.Walking => true,
            PlayerState.Ducking => true,  // 🎯 Ducking 상태에서도 웅크리기 유지 가능
            PlayerState.Jumping => false,
            PlayerState.Falling => false,
            PlayerState.Climbing => false,
            PlayerState.Stunned => false,
            PlayerState.Dead => false,  // 사망 시 모든 행동 불가 (유령 전환 대기)
            _ => false
        };
    }
}

// 🎮 상태 전환 조건 정의
public static class PlayerStateTransitions
{
    // 상태 전환이 가능한지 확인하는 메서드들
    public static bool CanTransitionTo(PlayerState currentState, PlayerState newState)
    {
        switch (currentState)
        {
            case PlayerState.Idle:
                return newState == PlayerState.Walking || 
                       newState == PlayerState.Ducking || 
                       newState == PlayerState.Jumping ||
                       newState == PlayerState.Climbing ||
                       newState == PlayerState.Stunned ||
                       newState == PlayerState.Dead;
                       
            case PlayerState.Walking:
                return newState == PlayerState.Idle || 
                       newState == PlayerState.Ducking || 
                       newState == PlayerState.Jumping ||
                       newState == PlayerState.Climbing ||
                       newState == PlayerState.Stunned ||
                       newState == PlayerState.Dead;
                       
            case PlayerState.Ducking:
                return newState == PlayerState.Idle || 
                       newState == PlayerState.Walking || 
                       newState == PlayerState.Stunned ||
                       newState == PlayerState.Dead;
                       
            case PlayerState.Jumping:
                return newState == PlayerState.Falling || 
                       newState == PlayerState.Idle || 
                       newState == PlayerState.Climbing ||
                       newState == PlayerState.Stunned ||
                       newState == PlayerState.Dead;
                       
            case PlayerState.Falling:
                return newState == PlayerState.Idle || 
                       newState == PlayerState.Climbing ||
                       newState == PlayerState.Stunned ||
                       newState == PlayerState.Dead;
                       
            case PlayerState.Climbing:
                return newState == PlayerState.Jumping || 
                       newState == PlayerState.Idle || 
                       newState == PlayerState.Falling ||
                       newState == PlayerState.Stunned ||
                       newState == PlayerState.Dead;
                       
            case PlayerState.Stunned:
                return newState == PlayerState.Idle || 
                       newState == PlayerState.Dead;
                       
            case PlayerState.Dead:
                return false; // 사망 상태에서는 다른 상태로 전환 불가 (유령 프리팹으로 전환 예정)
                
            default:
                return false;
        }
    }
    
    // 액션별 동작 제한 확인
    public static bool CanMove(PlayerAction action)
    {
        return true; // 액션은 이동을 제한하지 않음
    }
    
    public static bool CanJump(PlayerAction action)
    {
        return true; // 액션은 점프를 제한하지 않음
    }
    
    public static bool CanUseItem(PlayerAction action)
    {
        return true; // 액션은 아이템 사용을 제한하지 않음
    }
} 