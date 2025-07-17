using Fusion;
using UnityEngine;

// 🎮 플레이어 아이템 사용 컨트롤러
// Hand 하위 오브젝트에 위치하며, 아이템 사용과 회전을 담당
public class PlayerItemUsage : NetworkBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private bool enableItemRotation = true;  // 회전 기능 켜기/끄기
    [SerializeField] private float rotationSpeed = 10f;       // 회전 속도 (0 = 즉시)
    
    [Header("Basic Punch")]
    [SerializeField] private BasicPunchItem basicPunchItem;   // 기본 펀치 아이템
    
    // 🌐 네트워크 동기화 상태
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    [Networked] private NetworkBool WasHoldingButton { get; set; }
    [Networked] private NetworkBool ButtonLatched { get; set; }
    [Networked] private float NetworkedRotationAngle { get; set; }
    [Networked] private NetworkBool NetworkedFlipY { get; set; }
    
    // 참조 컴포넌트들
    private SpelunkyPlayerController playerController;
    private PlayerItemPickup itemPickup;
    private PlayerMovement playerMovement;
    
    public override void Spawned()
    {
        SetupReferences();
    }
    
    private void SetupReferences()
    {
        playerController = GetComponent<SpelunkyPlayerController>();
        itemPickup = GetComponent<PlayerItemPickup>();
        
        Transform parentPlayer = transform.parent;
        if (parentPlayer != null)
        {
            playerMovement = parentPlayer.GetComponent<PlayerMovement>();
        }
    }
    
    
    public void ProcessInput(SpelunkyPlayerData input)
    {
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;
        
        // 현재 사용 중인 오브젝트 결정 (아이템 또는 기본 펀치)
        GameObject activeObject = itemPickup?.CurrentItem;
        bool useBasicPunch = (activeObject == null && basicPunchItem != null);
        
        // 회전 처리
        if (enableItemRotation)
        {
            if (activeObject != null)
            {
                RotateObjectToMouse(activeObject, input.MouseWorldPosition);
            }
            else if (useBasicPunch && basicPunchItem.gameObject.activeSelf)
            {
                RotateObjectToMouse(basicPunchItem.gameObject, input.MouseWorldPosition);
            }
        }
        
        // 사용 처리
        if (activeObject != null)
        {
            // 아이템 사용
            HandleItemUse(input, pressed);
        }
        else if (useBasicPunch)
        {
            // 기본 펀치 사용 (아이템과 동일한 방식)
            HandleBasicPunchUse(input, pressed);
        }
    }
    
    private void HandleItemUse(SpelunkyPlayerData input, NetworkButtons pressed)
    {
        IUsableItem usableItem = itemPickup.CurrentItem.GetComponent<IUsableItem>();
        if (usableItem == null) return;
        
        Vector2 playerPos = transform.position;
        
        // 현재 프레임의 버튼 상태
        bool isHoldingButton = input.NetworkButtons.IsSet(SpelunkyInputButtons.UseItemPress) ||
                              input.NetworkButtons.IsSet(SpelunkyInputButtons.UseItemHold);

        // Press 처리 (래치되지 않은 상태에서만)
        if (!ButtonLatched && isHoldingButton)
        {
            UseItemRpc(UsageType.Press, input.MouseWorldPosition, playerPos);
            ButtonLatched = true;
        }

        // Hold 처리 (연사 등을 위해 계속 전달)
        if (input.NetworkButtons.IsSet(SpelunkyInputButtons.UseItemHold))
        {
            UseItemRpc(UsageType.Hold, input.MouseWorldPosition, playerPos);
        }

        // Release 처리 - 버튼을 떼면 래치 해제
        if (WasHoldingButton && !isHoldingButton)
        {
            UseItemRpc(UsageType.Release, input.MouseWorldPosition, playerPos);
            ButtonLatched = false;
        }

        // 상태 업데이트
        WasHoldingButton = isHoldingButton;
    }
    
    private void HandleBasicPunchUse(SpelunkyPlayerData input, NetworkButtons pressed)
    {
        if (basicPunchItem == null) return;

        Vector2 playerPos = transform.position;
        
        // 현재 프레임의 버튼 상태
        bool isHoldingButton = input.NetworkButtons.IsSet(SpelunkyInputButtons.UseItemPress) ||
                              input.NetworkButtons.IsSet(SpelunkyInputButtons.UseItemHold);

        // Press 처리 (래치되지 않은 상태에서만)
        if (!ButtonLatched && isHoldingButton)
        {
            UseBasicPunchRpc(UsageType.Press, input.MouseWorldPosition, playerPos);
            ButtonLatched = true;
        }

        // Hold 처리 (연사 등을 위해 계속 전달)
        if (input.NetworkButtons.IsSet(SpelunkyInputButtons.UseItemHold))
        {
            UseBasicPunchRpc(UsageType.Hold, input.MouseWorldPosition, playerPos);
        }

        // Release 처리 - 버튼을 떼면 래치 해제
        if (WasHoldingButton && !isHoldingButton)
        {
            UseBasicPunchRpc(UsageType.Release, input.MouseWorldPosition, playerPos);
            ButtonLatched = false;
        }

        // 상태 업데이트
        WasHoldingButton = isHoldingButton;
    }
    
    private enum UsageType
    {
        Press,    // 클릭 시작 - 즉시 발동 (폭탄, 물약 등)
        Hold,     // 클릭 유지 - 연속 사용 (드릴, 기관총 등)  
        Release   // 클릭 종료 - 충전형 (활, 마법 등)
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void UseItemRpc(UsageType usageType, Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (itemPickup?.CurrentItem == null) return;
        
        IUsableItem usableItem = itemPickup.CurrentItem.GetComponent<IUsableItem>();
        if (usableItem == null) return;
        
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
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void UseBasicPunchRpc(UsageType usageType, Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        Debug.Log($"[펀치] UseBasicPunchRpc 호출: {usageType}");
        if (basicPunchItem == null) return;
        
        switch (usageType)
        {
            case UsageType.Press:
                basicPunchItem.OnUsePress(mouseWorldPosition, playerPosition);
                break;
            case UsageType.Hold:
                basicPunchItem.OnUseHold(mouseWorldPosition, playerPosition);
                break;
            case UsageType.Release:
                basicPunchItem.OnUseRelease(mouseWorldPosition, playerPosition);
                break;
        }
    }
    
    // 오브젝트를 마우스 방향으로 회전
    private void RotateObjectToMouse(GameObject targetObject, Vector2 mouseWorldPosition)
    {
        if (targetObject == null || playerMovement == null || !Object.HasStateAuthority) return;

        // 회전 각도 계산
        Vector2 direction = (mouseWorldPosition - (Vector2)transform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 스프라이트 좌우 반전 상태 계산
        bool isLeft = mouseWorldPosition.x < transform.position.x;
        
        // 네트워크 상태 업데이트
        NetworkedFlipY = isLeft;
        
        // 회전 적용 (즉시 또는 보간)
        if (rotationSpeed <= 0)
        {
            NetworkedRotationAngle = targetAngle;
        }
        else
        {
            float currentAngle = NetworkedRotationAngle;
            NetworkedRotationAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * rotationSpeed);
        }
    }

    public override void Render()
    {
        base.Render();
        
        // 현재 사용 중인 오브젝트 결정
        GameObject activeObject = itemPickup?.CurrentItem;
        bool useBasicPunch = (activeObject == null && basicPunchItem != null);
        
        GameObject targetObject = useBasicPunch ? basicPunchItem.gameObject : activeObject;
        if (targetObject != null)
        {
            // 회전 적용
            targetObject.transform.localEulerAngles = new Vector3(0, 0, NetworkedRotationAngle);
            
            // 스프라이트 반전 적용
            var spriteRenderer = targetObject.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipY = NetworkedFlipY;
            }
        }
    }
} 