using Fusion;
using UnityEngine;

// 🎮 플레이어 아이템 사용 컨트롤러
// Hand 하위 오브젝트에 위치하며, 아이템 사용과 회전을 담당
public class PlayerItemUsage : NetworkBehaviour
{
    [Header("Usage Settings")]
    [SerializeField] private bool enableItemRotation = true;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private BasicPunchItem basicPunchItem; // 기본 펀치 아이템
    
    // 🌐 네트워크 동기화 상태
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    
    // 참조 컴포넌트들
    private SpelunkyPlayerController playerController;
    private PlayerItemPickup itemPickup;
    private PlayerMovement playerMovement;
    
    public override void Spawned()
    {
        SetupReferences();
        InitializeBasicPunch();
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
    
    private void InitializeBasicPunch()
    {
        if (basicPunchItem != null)
        {
            basicPunchItem.gameObject.SetActive(false);
        }
    }
    
    public void ProcessInput(SpelunkyPlayerData input)
    {
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;
        
        // 아이템 회전 처리
        if (enableItemRotation)
        {
            if (itemPickup?.CurrentItem != null)
            {
                RotateItemToMouse(input.MouseWorldPosition);
            }
            else if (basicPunchItem != null && basicPunchItem.gameObject.activeSelf)
            {
                RotatePunchToMouse(input.MouseWorldPosition);
            }
        }

        // 아이템이 있을 때 사용 처리
        if (itemPickup?.CurrentItem != null)
        {
            HandleItemUse(input, pressed);
        }
        // 아이템이 없을 때는 기본 주먹 사용
        else
        {
            HandleBasicPunchUse(input, pressed);
        }
    }
    
    private void HandleItemUse(SpelunkyPlayerData input, NetworkButtons pressed)
    {
        IUsableItem usableItem = itemPickup.CurrentItem.GetComponent<IUsableItem>();
        if (usableItem == null || !usableItem.CanUse) return;
        
        Vector2 playerPos = transform.position;
        
        if (pressed.IsSet(SpelunkyInputButtons.UseItemPress))
        {
            UseItemRpc(UsageType.Press, input.MouseWorldPosition, playerPos);
        }
        if (input.NetworkButtons.IsSet(SpelunkyInputButtons.UseItemHold))
        {
            UseItemRpc(UsageType.Hold, input.MouseWorldPosition, playerPos);
        }
        if (pressed.IsSet(SpelunkyInputButtons.UseItemRelease))
        {
            UseItemRpc(UsageType.Release, input.MouseWorldPosition, playerPos);
        }
    }
    
    private void HandleBasicPunchUse(SpelunkyPlayerData input, NetworkButtons pressed)
    {
        if (basicPunchItem == null) return;
        
        Vector2 playerPos = transform.position;
        
        if (pressed.IsSet(SpelunkyInputButtons.UseItemPress))
        {
            basicPunchItem.gameObject.SetActive(true);
            basicPunchItem.OnUsePress(input.MouseWorldPosition, playerPos);
        }
        if (input.NetworkButtons.IsSet(SpelunkyInputButtons.UseItemHold))
        {
            basicPunchItem.OnUseHold(input.MouseWorldPosition, playerPos);
        }
        if (pressed.IsSet(SpelunkyInputButtons.UseItemRelease))
        {
            basicPunchItem.OnUseRelease(input.MouseWorldPosition, playerPos);
        }
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
    
    private void RotateItemToMouse(Vector2 mouseWorldPosition)
    {
        GameObject currentItem = itemPickup.CurrentItem;
        if (currentItem == null || playerMovement == null) return;

        IUsableItem usableItem = currentItem.GetComponent<IUsableItem>();
        if (usableItem == null) return;

        Vector2 direction = (mouseWorldPosition - (Vector2)transform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        var spriteRenderer = currentItem.GetComponentInChildren<SpriteRenderer>();
        bool isLeft = (mouseWorldPosition.x < transform.position.x);
        if (spriteRenderer != null)
            spriteRenderer.flipY = isLeft;

        currentItem.transform.localEulerAngles = new Vector3(0, 0, targetAngle);
    }
    
    private void RotatePunchToMouse(Vector2 mouseWorldPosition)
    {
        GameObject punchObj = basicPunchItem.gameObject;
        if (punchObj == null) return;

        Vector2 direction = (mouseWorldPosition - (Vector2)transform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        var spriteRenderer = punchObj.GetComponentInChildren<SpriteRenderer>();
        bool isLeft = (mouseWorldPosition.x < transform.position.x);
        if (spriteRenderer != null)
            spriteRenderer.flipY = isLeft;

        punchObj.transform.localEulerAngles = new Vector3(0, 0, targetAngle);
    }
} 