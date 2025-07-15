using Fusion;
using UnityEngine;

/*
 * ===============================================
 * 🎮 플레이어 아이템 사용 시스템 (개발자 참고용)
 * ===============================================
 * 
 * 📋 시스템 구조:
 * 1. 이 스크립트는 Hand 오브젝트에 위치
 * 2. 플레이어 입력(마우스 클릭)을 감지
 * 3. 들고 있는 아이템의 사용 메서드 호출
 * 4. 아이템 회전도 자동으로 처리
 * 
 * 🔄 입력 처리 흐름:
 * 플레이어 클릭 → PlayerItemUsage → IUsableItem 메서드 호출
 * 
 * 🎯 주요 기능:
 * - 3가지 사용 패턴 지원 (Press, Hold, Release)
 * - 아이템을 마우스 방향으로 자동 회전
 * - Fusion 2 네트워크 동기화
 * - 플레이어 방향에 따른 아이템 뒤집기
 * 
 * ⚠️ 주의: 아이템 로직은 건드리지 마세요. 새 아이템은 IUsableItem을 구현해주세요.
 */

// 🎮 플레이어 아이템 사용 컨트롤러 (간소화 버전)
// Hand 하위 오브젝트에 위치하며, 3가지 입력 상태를 아이템에 전달
public class PlayerItemUsage : NetworkBehaviour
{
    [Header("Usage Settings")]
    [Tooltip("디버그 정보를 화면에 표시할지 여부")]
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("Rotation Settings - 아이템 회전 설정")]
    [Tooltip("아이템을 마우스 방향으로 자동 회전시킬지 여부")]
    [SerializeField] private bool enableItemRotation = true;
    
    [Tooltip("회전 속도 (0이면 즉시 회전, 큰 값일수록 부드럽게 회전)")]
    [SerializeField] private float rotationSpeed = 10f;
    
    // Fusion 2 공식 패턴: 이전 버튼 상태 추적
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    
    // 📦 컴포넌트 참조들
    private SpelunkyPlayerController playerController;
    private PlayerItemPickup itemPickup;
    private PlayerMovement playerMovement;  // 플레이어 방향 정보용
    
    public override void Spawned()
    {
        // 컴포넌트 찾기
        playerController = GetComponent<SpelunkyPlayerController>();
        itemPickup = GetComponent<PlayerItemPickup>();
        
        // 부모(Player)에서 PlayerMovement 찾기
        Transform parentPlayer = transform.parent;
        if (parentPlayer != null)
        {
            playerMovement = parentPlayer.GetComponent<PlayerMovement>();
        }
        
        Debug.Log($"🎮 PlayerItemUsage 생성 - HasInputAuthority: {Object.HasInputAuthority}");
    }
    
    /// <summary>
    /// 🎮 아이템 사용 입력 처리 (SpelunkyPlayerController에서 호출)
    /// 
    /// 🔄 처리 순서:
    /// 1. 마우스 입력 상태 감지 (Press, Hold, Release)
    /// 2. 아이템 회전 처리
    /// 3. 해당하는 IUsableItem 메서드 호출
    /// 
    /// ⚠️ 이 메서드는 건드리지 마세요 - 아이템 로직은 IUsableItem에서 구현
    /// </summary>
    /// <param name="input">플레이어 입력 데이터</param>
    public void ProcessInput(SpelunkyPlayerData input)
    {
        // Fusion 2 공식 패턴: GetPressed/GetReleased로 버튼 상태 감지
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        var released = input.NetworkButtons.GetReleased(ButtonsPrevious);
        
        // 이전 상태 업데이트 (공식 패턴)
        ButtonsPrevious = input.NetworkButtons;
        
        // 🔄 아이템 회전 처리 (매 프레임 실행)
        if (enableItemRotation && itemPickup?.CurrentItem != null)
        {
            RotateItemToMouse(input.MouseWorldPosition);
        }
        
        // 아이템이 있을 때만 사용 처리
        if (itemPickup?.CurrentItem != null)
        {
            // 아이템에서 IUsableItem 컴포넌트 찾기
            IUsableItem usableItem = itemPickup.CurrentItem.GetComponent<IUsableItem>();
            
            if (usableItem != null && usableItem.CanUse)
            {
                Vector2 playerPos = transform.position;
                
                // 🔨 3가지 입력 상태를 아이템에 전달
                // 아이템이 필요한 것만 구현하면 됨 (Press만, 또는 Press+Hold 등)
                
                // 클릭 시작 - 즉시 발동 아이템들이 주로 사용
                if (pressed.IsSet(SpelunkyInputButtons.UseItemPress))
                {
                    UseItemRpc(UsageType.Press, input.MouseWorldPosition, playerPos);
                }
                
                // 클릭 유지 - 연속 사용 아이템들이 주로 사용 (매 프레임 호출됨)
                if (input.NetworkButtons.IsSet(SpelunkyInputButtons.UseItemHold))
                {
                    UseItemRpc(UsageType.Hold, input.MouseWorldPosition, playerPos);
                }
                
                // 클릭 종료 - 충전형 아이템들이 주로 사용
                if (released.IsSet(SpelunkyInputButtons.UseItemRelease))
                {
                    UseItemRpc(UsageType.Release, input.MouseWorldPosition, playerPos);
                }
            }
            else if (usableItem == null && pressed.IsSet(SpelunkyInputButtons.UseItemPress))
            {
                Debug.Log("🔨 아이템에 IUsableItem 컴포넌트가 없습니다.");
            }
        }
        else if (pressed.IsSet(SpelunkyInputButtons.UseItemPress))
        {
            Debug.Log("🎮 사용할 아이템이 없음");
        }
    }
    
    /// <summary>
    /// 🔨 아이템 사용 타입 (3가지 패턴)
    /// 아이템 종류에 따라 필요한 것만 구현하면 됨
    /// </summary>
    private enum UsageType
    {
        Press,    // 클릭 시작 - 즉시 발동 (폭탄, 물약 등)
        Hold,     // 클릭 유지 - 연속 사용 (드릴, 기관총 등)  
        Release   // 클릭 종료 - 충전형 (활, 마법 등)
    }
    
    /// <summary>
    /// 🌐 아이템 사용 RPC - 네트워크 동기화
    /// 
    /// Fusion 2 패턴: InputAuthority가 모든 클라이언트에게 전송
    /// 실제 아이템 로직은 IUsableItem 메서드에서 실행됨
    /// 
    /// ⚠️ 건드리지 마세요 - 아이템 로직은 각 아이템 클래스에서 구현
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void UseItemRpc(UsageType usageType, Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (itemPickup?.CurrentItem == null) return;
        
        IUsableItem usableItem = itemPickup.CurrentItem.GetComponent<IUsableItem>();
        if (usableItem == null) return;
        
        // 사용 타입에 따라 해당하는 아이템 메서드 호출
        // 아이템에서 구현하지 않은 메서드는 기본 구현(아무것도 안함)이 실행됨
        switch (usageType)
        {
            case UsageType.Press:
                usableItem.OnUsePress(mouseWorldPosition, playerPosition);
                break;
            case UsageType.Hold:
                usableItem.OnUseHold(mouseWorldPosition, playerPosition);
                break;
            case UsageType.Release:
                usableItem.OnUseRelease(mouseWorldPosition, playerPosition);
                break;
        }
    }
    
    /// <summary>
    /// 🔄 아이템을 마우스 방향으로 자동 회전
    /// 
    /// 🎯 기능:
    /// - 마우스 위치에 따른 아이템 회전
    /// - 플레이어 방향(좌우)에 따른 자동 뒤집기
    /// - 부드러운 회전 또는 즉시 회전 지원
    /// - 아이템별 기본 방향 자동 감지
    /// 
    /// ⚠️ 이제 각 아이템의 DefaultDirection이 자동으로 적용됩니다
    /// </summary>
    /// <param name="mouseWorldPosition">마우스의 월드 좌표</param>
    private void RotateItemToMouse(Vector2 mouseWorldPosition)
    {
        GameObject currentItem = itemPickup.CurrentItem;
        if (currentItem == null || playerMovement == null) return;

        IUsableItem usableItem = currentItem.GetComponent<IUsableItem>();
        if (usableItem == null) return;

        float itemDirectionAngle = 0f;
        if (usableItem is UsableItemBase baseItem)
            itemDirectionAngle = baseItem.DefaultAngle;

        Vector2 direction = (mouseWorldPosition - (Vector2)transform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float angleDiff = itemDirectionAngle;
        targetAngle -= angleDiff;

        // 반전 없이 z축 회전만 적용
        currentItem.transform.localEulerAngles = new Vector3(0, 0, targetAngle);
    }
    
    // ===============================================
    // 📊 상태 확인 및 디버그 (다른 스크립트에서 참조 가능)
    // ===============================================
    
    /// <summary>현재 아이템을 사용할 수 있는 상태인지 반환</summary>
    public bool CanUseItem => itemPickup?.CurrentItem != null;
    
    // 🔍 디버그 정보 표시
    private void OnGUI()
    {
        if (!showDebugInfo || !Object.HasInputAuthority) return;
        
        GUILayout.BeginArea(new Rect(10, 400, 300, 100));
        GUILayout.Box("🎮 아이템 사용 (간소화)");
        GUILayout.Label($"사용 가능: {(CanUseItem ? "예" : "아니오")}");
        GUILayout.Label("조작법: 좌클릭 - 사용");
        GUILayout.EndArea();
    }
} 