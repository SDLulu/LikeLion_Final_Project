using Fusion;
using UnityEngine;

// 🎮 플레이어 아이템 사용 컨트롤러 (간소화 버전)
// Hand 하위 오브젝트에 위치하며, 3가지 입력 상태를 아이템에 전달
public class PlayerItemUsage : NetworkBehaviour
{
    [Header("Usage Settings")]
    [SerializeField] private bool showDebugInfo = true;        // 디버그 정보 표시
    
    [Header("Rotation Settings")]
    [SerializeField] private bool enableItemRotation = true;   // 아이템 회전 활성화
    [SerializeField] private float rotationSpeed = 10f;        // 회전 속도 (즉시 회전은 0)
    [SerializeField] private Vector2 rotationOffset = Vector2.right; // 기본 방향 오프셋
    
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
    
    // 아이템 사용 관련 모든 처리를 통합한 메서드 (Fusion 2 공식 패턴)
    public void ProcessInput(SpelunkyPlayerData input)
    {
        // Fusion 2 공식 패턴: GetPressed/GetReleased로 버튼 상태 감지
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        var released = input.NetworkButtons.GetReleased(ButtonsPrevious);
        
        // 이전 상태 업데이트 (공식 패턴)
        ButtonsPrevious = input.NetworkButtons;
        
        // 🔄 아이템 회전 처리 (매 프레임)
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
                
                // 🔨 3가지 입력 상태를 아이템에 전달 (아이템이 알아서 처리)
                if (pressed.IsSet(SpelunkyInputButtons.UseItemPress))
                {
                    UseItemRpc(UsageType.Press, input.MouseWorldPosition, playerPos);
                }
                
                if (input.NetworkButtons.IsSet(SpelunkyInputButtons.UseItemHold))
                {
                    UseItemRpc(UsageType.Hold, input.MouseWorldPosition, playerPos);
                }
                
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
    
    // 🔨 간단한 사용 타입 (3가지만)
    private enum UsageType
    {
        Press,    // 클릭 시작
        Hold,     // 클릭 유지
        Release   // 클릭 종료
    }
    
    // 🌐 아이템 사용 RPC (입력 상태를 아이템에 전달)
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void UseItemRpc(UsageType usageType, Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (itemPickup?.CurrentItem == null) return;
        
        IUsableItem usableItem = itemPickup.CurrentItem.GetComponent<IUsableItem>();
        if (usableItem == null) return;
        
        // 단순하게 3가지 메서드 중 하나 호출
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
    
    // 🔄 아이템을 마우스 방향으로 회전 (플레이어 방향 동기화)
    private void RotateItemToMouse(Vector2 mouseWorldPosition)
    {
        GameObject currentItem = itemPickup.CurrentItem;
        if (currentItem == null || playerMovement == null) return;
        
        // 플레이어(Hand) 위치에서 마우스로의 방향 계산
        Vector2 direction = (mouseWorldPosition - (Vector2)transform.position).normalized;
        
        // 방향 벡터를 각도로 변환
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // rotationOffset 계산 (기본 방향 오프셋)
        float offsetAngle = Mathf.Atan2(rotationOffset.y, rotationOffset.x) * Mathf.Rad2Deg;
        
        // 플레이어 방향에 따른 각도 및 오프셋 조정
        if (playerMovement.IsFacingLeft)
        {
            // 플레이어가 왼쪽을 보고 있으면 180도 회전하여 왼쪽 기준으로 조정
            targetAngle += 180f;
            // 오프셋도 함께 뒤집어줌 (핵심!)
            offsetAngle += 180f;
        }
        
        // 최종 각도 = 마우스 방향 - 아이템 기본 방향 오프셋
        targetAngle -= offsetAngle;
        
        // 회전 적용 (스케일은 건드리지 않음)
        if (rotationSpeed <= 0f)
        {
            // 즉시 회전
            currentItem.transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        }
        else
        {
            // 부드러운 회전
            currentItem.transform.rotation = Quaternion.Slerp(
                currentItem.transform.rotation,
                Quaternion.Euler(0, 0, targetAngle),
                rotationSpeed * Runner.DeltaTime
            );
        }
    }
    
    // 📊 상태 확인 프로퍼티들
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