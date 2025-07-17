using UnityEngine;
using Fusion;

// 플레이어 관련 디버그 정보를 통합 관리하는 컴포넌트
// Player 오브젝트에 부착하여 사용
public class PlayerDebugManager : NetworkBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showItemInfo = true;
    [SerializeField] private bool showMovementInfo = true;
    [SerializeField] private bool showJumpInfo = true;
    [SerializeField] private bool showClimbingInfo = true;
    [SerializeField] private bool showAnimationInfo = true;
    
    // 참조 컴포넌트들
    private PlayerItemPickup itemPickup;
    private PlayerItemUsage itemUsage;
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerGroundCheck groundCheck;
    private PlayerClimbing climbing;
    private PlayerLadderCheck ladderCheck;
    private PlayerAnimation playerAnimation;
    
    private void Awake()
    {
        SetupReferences();
    }
    
    private void SetupReferences()
    {
        // Hand 오브젝트에서 아이템 관련 컴포넌트 찾기
        Transform handObject = transform.Find("Hand");
        if (handObject != null)
        {
            itemPickup = handObject.GetComponent<PlayerItemPickup>();
            itemUsage = handObject.GetComponent<PlayerItemUsage>();
        }
        
        // Visual 오브젝트에서 애니메이션 컴포넌트 찾기
        Transform visualObject = transform.Find("Visual");
        if (visualObject != null)
        {
            playerAnimation = visualObject.GetComponent<PlayerAnimation>();
        }
        
        // 이동/점프 관련 컴포넌트 찾기
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
        groundCheck = GetComponentInChildren<PlayerGroundCheck>();
        climbing = GetComponent<PlayerClimbing>();
        ladderCheck = GetComponentInChildren<PlayerLadderCheck>();
    }
    
    private void OnGUI()
    {
        if (!Object.HasInputAuthority) return;
        
        if (showItemInfo)
        {
            DrawItemDebugInfo();
        }
        
        if (showMovementInfo)
        {
            DrawMovementDebugInfo();
        }
        
        if (showJumpInfo)
        {
            DrawJumpDebugInfo();
        }
        
        if (showClimbingInfo)
        {
            DrawClimbingDebugInfo();
        }

        if (showAnimationInfo)
        {
            DrawAnimationDebugInfo();
        }
    }
    
    private void DrawItemDebugInfo()
    {
        GUILayout.BeginArea(new Rect(10, 250, 300, 140));
        GUILayout.Box("🎒 아이템 시스템");
        
        // 픽업 정보
        if (itemPickup != null)
        {
            GUILayout.Label($"현재 아이템: {itemPickup.CurrentItemName}");
            GUILayout.Label($"감지된 아이템: {itemPickup.NearbyItemsCount}개");
        }
        
        // 사용 정보
        if (itemUsage != null)
        {
            GUILayout.Label($"사용 가능: {(itemPickup?.CurrentItem != null ? "예" : "아니오")}");
        }
        
        GUILayout.Label("");
        GUILayout.Label("조작법:");
        GUILayout.Label("Space(웅크린 상태) - 픽업");
        GUILayout.Label("좌클릭 - 사용");
        GUILayout.Label("우클릭 - 던지기");
        GUILayout.EndArea();
    }
    
    private void DrawMovementDebugInfo()
    {
        if (movement == null) return;
        
        GUILayout.BeginArea(new Rect(10, 400, 300, 100));
        GUILayout.Box("🏃 이동 상태");
        GUILayout.Label($"속도: {movement.NormalizedSpeed:F2}");
        GUILayout.Label($"웅크리기: {(movement.IsDucking ? "예" : "아니오")}");
        GUILayout.Label($"방향: {(movement.IsFacingLeft ? "왼쪽" : "오른쪽")}");
        GUILayout.EndArea();
    }
    
    private void DrawJumpDebugInfo()
    {
        if (jump == null) return;
        
        GUILayout.BeginArea(new Rect(10, 510, 300, 100));
        GUILayout.Box("🦘 점프 상태");
        GUILayout.Label($"땅에 있음: {groundCheck?.IsGrounded}");
        GUILayout.Label($"점프 중: {jump.IsJumping}");
        GUILayout.Label($"점프 시간: {jump.JumpTime:F2}s");
        GUILayout.Label($"수직 속도: {jump.VelocityY:F1}");
        GUILayout.EndArea();
    }
    
    private void DrawClimbingDebugInfo()
    {
        if (climbing == null) return;
        
        GUILayout.BeginArea(new Rect(10, 630, 300, 120));
        GUILayout.Box("🪜 사다리 오르기 시스템");
        GUILayout.Label($"사다리 근처: {ladderCheck?.IsNearLadder}");
        GUILayout.Label($"오르기 중: {climbing.IsClimbing}");
        GUILayout.Label("");
        GUILayout.Label("조작법: 사다리 근처에서 위키(W) 유지");
        GUILayout.Label("점프키로 탈출");
        GUILayout.EndArea();
    }

    private void DrawAnimationDebugInfo()
    {
        GUILayout.BeginArea(new Rect(10, 760, 300, 100));
        GUILayout.Box("🎭 애니메이션 상태");
        GUILayout.Label($"속도: {movement?.NormalizedSpeed:F2}");
        GUILayout.Label($"수직속도: {jump?.VelocityY:F2}");
        GUILayout.Label($"상태: {(groundCheck?.IsGrounded == true ? "지상" : "공중")}");
        GUILayout.EndArea();
    }
} 