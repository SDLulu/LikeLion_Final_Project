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
    [SerializeField] private bool showPassiveItemInfo = true;
    [SerializeField] private bool showInteractionInfo = true;
    
    // 참조 컴포넌트들
    private PlayerObjectPickup itemPickup;
    private PlayerItemUsage itemUsage;
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerGroundCheck groundCheck;
    private PlayerWallCheck wallCheck;
    private PlayerClimbing climbing;
    private PlayerLadderCheck ladderCheck;
    private PlayerAnimation playerAnimation;
    private PlayerInventory inventory; // 인벤토리 참조 추가
    private PlayerInteraction playerInteraction; // 상호작용 참조 추가
    private PlayerDeathHandler playerDeathHandler; // 부활 디버그용
    
    [Header("Respawn Debug")]
    [SerializeField] private bool showRespawnControls = true;
    [SerializeField] private Transform debugRespawnPoint;
    
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
            itemPickup = handObject.GetComponent<PlayerObjectPickup>();
            itemUsage = handObject.GetComponent<PlayerItemUsage>();
            inventory = handObject.GetComponent<PlayerInventory>(); // 인벤토리 캐싱
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
        wallCheck = GetComponentInChildren<PlayerWallCheck>();
        climbing = GetComponent<PlayerClimbing>();
        ladderCheck = GetComponentInChildren<PlayerLadderCheck>();
        playerInteraction = GetComponent<PlayerInteraction>();
        playerDeathHandler = GetComponent<PlayerDeathHandler>();
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
        
        if (showPassiveItemInfo)
        {
            DrawPassiveItemDebugInfo();
        }
        
        if (showInteractionInfo)
        {
            DrawInteractionDebugInfo();
        }
        
        if (showRespawnControls)
        {
            DrawRespawnDebugControls();
        }
    }
    
    private void DrawItemDebugInfo()
    {
        GUILayout.BeginArea(new Rect(10, 250, 300, 140));
        GUILayout.Box("🎒 아이템 시스템");
        
        // 픽업 정보
        if (inventory != null)
        {
            string currentItemName = inventory.CurrentHeldObject?.name ?? "없음";
            GUILayout.Label($"현재 들고 있는 오브젝트: {currentItemName}");
            GUILayout.Label($"오브젝트 보유: {(inventory.CurrentHeldObject != null ? "예" : "아니오")}");
        }
        
        // 사용 정보
        if (itemUsage != null && inventory != null)
        {
            GUILayout.Label($"사용 가능: {(inventory.CurrentHeldObject != null && inventory.CurrentHeldObject.layer == LayerMask.NameToLayer("Item") ? "예" : "아니오")}");
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
        
        GUILayout.BeginArea(new Rect(10, 400, 300, 120));
        GUILayout.Box("🏃 이동 상태");
        GUILayout.Label($"속도: {movement.NormalizedSpeed:F2}");
        GUILayout.Label($"웅크리기: {(movement.IsDucking ? "예" : "아니오")}");
        GUILayout.Label($"방향: {(movement.IsFacingLeft ? "왼쪽" : "오른쪽")}");
        GUILayout.Label($"왼쪽벽: {(wallCheck?.IsTouchingWallLeft == true ? "붙음" : "없음")}");
        GUILayout.Label($"오른쪽벽: {(wallCheck?.IsTouchingWallRight == true ? "붙음" : "없음")}");
        GUILayout.EndArea();
    }
    
    private void DrawJumpDebugInfo()
    {
        if (jump == null) return;
        
        GUILayout.BeginArea(new Rect(10, 510, 300, 120));
        GUILayout.Box("🦘 점프 상태");
        GUILayout.Label($"땅에 있음: {groundCheck?.IsGrounded}");
        GUILayout.Label($"점프 중: {jump.IsJumping}");
        GUILayout.Label($"점프 시간: {jump.JumpTime:F2}s");
        GUILayout.Label($"점프 횟수: {jump.CurrentJumpCount}");
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
    
    private void DrawPassiveItemDebugInfo()
    {
        if (inventory == null || jump == null) return;
        
        GUILayout.BeginArea(new Rect(10, 870, 300, 140));
        GUILayout.Box("🧩 패시브 아이템");
        GUILayout.Label($"점프신발: {inventory.hasJumpShoes}");
        GUILayout.Label($"이속신발: {inventory.hasSpeedShoes}");
        GUILayout.Label($"날개: {inventory.hasWings}");
        GUILayout.Label($"로켓: {inventory.hasRocket}");
        
        if (inventory.hasRocket)
        {
            GUILayout.Label($"로켓 연료: {jump.RocketFuel:F1}");
            GUILayout.Label($"로켓 추진 중: {jump.IsRocketThrusting}");
        }
        
        if (inventory.hasWings)
        {
            GUILayout.Label($"최대 점프 횟수: {jump.CurrentJumpCount}");
        }
        GUILayout.EndArea();
    }
    
    private void DrawInteractionDebugInfo()
    {
        if (playerInteraction == null) return;
        
        GUILayout.BeginArea(new Rect(10, 1020, 300, 80));
        GUILayout.Box("🎯 상호작용 상태");
        
        // 상호작용 범위 정보
        GUILayout.Label($"상호작용 범위: {playerInteraction.InteractionRange:F1}");
        
        // 가장 가까운 상호작용 가능한 오브젝트 찾기
        var nearest = playerInteraction.FindNearestInteractable();
        if (nearest != null)
        {
            float distance = Vector2.Distance(transform.position, nearest.transform.position);
            GUILayout.Label($"가장 가까운 대상: {nearest.name} ({distance:F1}m)");
        }
        else
        {
            GUILayout.Label("상호작용 가능한 대상 없음");
        }
        GUILayout.EndArea();
    }
    
    private void DrawRespawnDebugControls()
    {
        if (playerDeathHandler == null) return;
        
        GUILayout.BeginArea(new Rect(320, 10, 300, 120));
        GUILayout.Box("🩺 부활 디버그");
        GUILayout.Label($"죽음 상태: {playerDeathHandler.IsDead}");
        
        if (GUILayout.Button("부활 (지정 위치)"))
        {
            var pos = debugRespawnPoint != null ? debugRespawnPoint.position : transform.position;
            playerDeathHandler.ResurrectAt(pos);
        }
        
        GUILayout.EndArea();
    }
} 