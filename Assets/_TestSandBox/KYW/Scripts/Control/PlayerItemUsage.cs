using Fusion;
using UnityEngine;

// 🎮 플레이어 아이템 사용 컨트롤러
// 📍 위치: Hand 하위 오브젝트 (Player > Hand > PlayerItemUsage)
// 🎯 목적: 1) 아이템/펀치를 마우스 방향으로 회전 2) 좌클릭으로 아이템/펀치 사용
public class PlayerItemUsage : NetworkBehaviour, ISoftReset
{
    [Header("Rotation Settings")]
    [SerializeField] private bool enableItemRotation = true;  // ⚙️ 회전 기능 켜기/끄기

    [SerializeField] private Transform handTransform;         // Hand 오브젝트(인스펙터에서 할당)
    
    [Header("Basic Punch")]
    [SerializeField] private BasicPunchItem basicPunchItem;   // 👊 기본 펀치 아이템 (아이템 없을 때 사용)
    
    // 🌐 네트워크 동기화 변수들 (모든 클라이언트가 동일한 값을 가짐)
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }    // 🎮 이전 프레임 버튼 상태 (래칭용)
    [Networked] private float NetworkedRotationAngle { get; set; }     // 🔄 회전 각도 (네트워크 동기화)
    [Networked] private NetworkBool NetworkedFlipY { get; set; }       // 🔄 스프라이트 Y축 반전 여부
    [Networked] private bool WasHolding { get; set; }                  // 🎮 이전 프레임 Hold 상태 (상태 변화 감지용)

    
 
    // 📎 참조할 다른 컴포넌트들
    private PlayerObjectPickup itemPickup;      // 📦 아이템 보유 상태 확인용
    private PlayerInventory inventory;        // 인벤토리 참조
    
    // 🚀 NetworkBehaviour 생성 시 호출 (모든 클라이언트에서 실행)
    public override void Spawned()
    {
        // 모든 컴포넌트 참조를 한 번에 설정
        itemPickup = GetComponent<PlayerObjectPickup>();  // 📦 같은 오브젝트의 PlayerItemPickup
        inventory = transform.parent.GetComponentInChildren<PlayerInventory>();    // Player 오브젝트의 자식들 중에서 찾기
        // 필수 컴포넌트 검증
        if (itemPickup == null)
            Debug.LogError($"[{name}] PlayerItemPickup 컴포넌트를 찾을 수 없습니다!");
        if (basicPunchItem == null)
            Debug.LogError($"[{name}] BasicPunchItem이 설정되지 않았습니다!");
        if (inventory == null)
            Debug.LogError($"[{name}] PlayerInventory 컴포넌트를 찾을 수 없습니다!");
    }
    
    private void Awake()
    {
        handTransform = this.transform;
    }

    // 🎮 입력 처리 - 단순화 버전
    public void ProcessInput(SpelunkyPlayerInputData input)
    {
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;

        GameObject held = inventory?.CurrentHeldObject;
        IUsableItem usable = null;

        if (held != null && held.layer == LayerMask.NameToLayer("Item"))
        {
            usable = held.GetComponent<IUsableItem>();
        }

        if (usable != null)
        {
            if (enableItemRotation)
                RotateObjectToMouse(held, input.MouseWorldPosition);
            HandleUsage(usable, input, pressed);
        }
        else if (basicPunchItem != null)
        {
            if (enableItemRotation)
                RotateObjectToMouse(basicPunchItem.gameObject, input.MouseWorldPosition);
            HandleUsage(basicPunchItem, input, pressed);
        }
    }
    
    // 🎯 현재 사용할 아이템 결정 (아이템 > 펀치 우선순위)
    private IUsableItem GetCurrentUsableItem()
    {
        // 손에 든 것이 아이템(레이어 == Item)일 때만 반환
        if (inventory != null && inventory.CurrentHeldObject != null)
        {
            var obj = inventory.CurrentHeldObject;
            if (obj.layer == LayerMask.NameToLayer("Item"))
                return obj.GetComponent<IUsableItem>();
        }
        // 아이템이 아니면 null (기본 펀치는 별도 처리)
        return null;
    }
    
    // 🎯 현재 회전시킬 오브젝트 결정
    private GameObject GetCurrentTargetObject()
    {
        if (inventory != null && inventory.CurrentHeldObject != null)
        {
            var obj = inventory.CurrentHeldObject;
            if (obj.layer == LayerMask.NameToLayer("Item"))
                return obj;
        }
        return basicPunchItem.gameObject;
    }
    

    

    
    // 🎮 Hold 전용 사용 처리 - 상태 변화로 Press/Release 감지
    private void HandleUsage(IUsableItem usableItem, SpelunkyPlayerInputData input, NetworkButtons pressed)
    {
        Vector2 mousePos = input.MouseWorldPosition;
        Vector2 playerPos = transform.position;
        
        bool isCurrentlyHolding = input.NetworkButtons.IsSet(SpelunkyInputButtons.UseItemHold);
        
        // 🎯 Hold 시작 = Press
        if (isCurrentlyHolding && !WasHolding)
        {
            Debug.Log($"[{name}] 아이템 사용 시작: {usableItem.GetType().Name}");
            usableItem.OnUsePress(mousePos, playerPos);
        }
        
        // 🔄 Hold 중
        if (isCurrentlyHolding)
        { 
            usableItem.OnUseHold(mousePos, playerPos);
        }
        
        // 🎯 Hold 끝 = Release
        if (!isCurrentlyHolding && WasHolding)
        {
            Debug.Log($"[{name}] 아이템 사용 종료: {usableItem.GetType().Name}");
            usableItem.OnUseRelease(mousePos, playerPos);
        }
        
        // 🔄 상태 저장
        WasHolding = isCurrentlyHolding;
    }
    
    // 🔄 회전 처리 - Hand 오브젝트를 마우스 방향으로 회전
    private void RotateObjectToMouse(GameObject targetObject, Vector2 mouseWorldPosition)
    {
        if (handTransform == null) return;
        Vector2 direction = (mouseWorldPosition - (Vector2)handTransform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bool isLeft = mouseWorldPosition.x < handTransform.position.x;

        // InputAuthority는 SpelunkyPlayerController에서 이미 체크됨
        NetworkedFlipY = isLeft;
        NetworkedRotationAngle = targetAngle;
        handTransform.rotation = Quaternion.Euler(0, 0, targetAngle);
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (handTransform != null && enableItemRotation)
        {
            handTransform.rotation = Quaternion.Euler(0, 0, NetworkedRotationAngle);
        }
        // 기존 currentObject 회전 코드는 제거
    }
    
    // 🎨 렌더링 - 시각적 요소만 처리
    public override void Render()
    {
        base.Render();
        
        GameObject currentObject = GetCurrentTargetObject();
        if (currentObject != null && enableItemRotation)
        {
            // 스프라이트 뒤집기 (순수 시각적 요소)
            var spriteRenderer = currentObject.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.flipY = NetworkedFlipY;
            }
        }
    }

    /// <summary>
    /// ISoftReset 구현: 입력 상태 초기화
    /// </summary>
    public void SoftReset()
    {
        ButtonsPrevious = default;
        WasHolding = false;
    }
}
