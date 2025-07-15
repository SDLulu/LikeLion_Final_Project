using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public const byte BUTTON_JUMP = 1 << 0;
    public const byte BUTTON_INTERACT = 1 << 1;
    public const byte BUTTON_UP = 1 << 2;
    public const byte BUTTON_DOWN = 1 << 3;
    public const byte BUTTON_LEFT = 1 << 4;
    public const byte BUTTON_RIGHT = 1 << 5;

    public byte Buttons;
    // 기타 필요한 입력 데이터 (예: 마우스 위치 등)

    public bool IsSet(byte button)
    {
        return (Buttons & button) == button;
    }

    public void Set(byte button, bool set)
    {
        if (set)
        {
            Buttons |= button;
        }
        else
        {
            Buttons &= (byte)~button;
        }
    }
}

public class PlayerController : NetworkBehaviour
{
    private PlayerInventory _playerInventory;
    private ShopItem _heldShopItem; // 현재 들고 있는 ShopItem 참조 (로컬에서만 사용)

    public override void Spawned()
    {
        _playerInventory = GetComponent<PlayerInventory>();
        if (_playerInventory == null)
        {
            Debug.LogError("PlayerInventory component not found on PlayerController!");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            // --- 이동 처리 ---
            Vector3 moveDirection = Vector3.zero;
            if (data.IsSet(NetworkInputData.BUTTON_UP)) moveDirection.y += 1;
            if (data.IsSet(NetworkInputData.BUTTON_DOWN)) moveDirection.y -= 1;
            if (data.IsSet(NetworkInputData.BUTTON_LEFT)) moveDirection.x -= 1;
            if (data.IsSet(NetworkInputData.BUTTON_RIGHT)) moveDirection.x += 1;

            transform.position += moveDirection.normalized * Runner.DeltaTime * 5.0f; // 예시 이동

            // --- 상호작용 처리 ---
            if (data.IsSet(NetworkInputData.BUTTON_INTERACT))
            {
                // 입력 처리 쿨다운 (중복 입력 방지)
                if (Object.HasInputAuthority && !Object.GetComponent<InputCooldownHandler>().CanInteract())
                    return;
                Object.GetComponent<InputCooldownHandler>().SetInteractCooldown();

                if (_heldShopItem != null)
                {
                    // 아이템을 들고 있다면 드롭 요청
                    _heldShopItem.RPC_RequestDrop(Object.InputAuthority, transform.position);
                    _heldShopItem = null; // 로컬에서도 참조 해제
                }
                else
                {
                    // 들고 있는 아이템이 없다면 주변 상호작용 오브젝트 찾기
                    CheckForInteraction();
                }
            }
        }
    }

    private void CheckForInteraction()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, 0.7f); // 상호작용 반경
        foreach (Collider2D hitCollider in hitColliders)
        {
            // ShopItem과의 상호작용 시도
            ShopItem shopItem = hitCollider.GetComponentInParent<ShopItem>(); // 부모에서 찾기 (ShopItem은 NetworkObject 자식일 수 있음)
            if (shopItem != null && shopItem.Object.IsValid && !shopItem.Object.IsProxy) // 유효하고 프록시가 아닌지 확인
            {
                shopItem.OnInteract(this);
                return;
            }
            // 다른 상호작용 가능한 오브젝트(제단 등)도 여기서 처리
        }
    }

    public bool CanPickupItem()
    {
        return _playerInventory != null && _playerInventory.CanPickupItem();
    }

    public void PickupItem(ShopItem item)
    {
        if (_playerInventory != null)
        {
            _playerInventory.PickupItem(item);
            _heldShopItem = item; // 로컬 플레이어에게 들고 있는 아이템 참조 할당
            // 💡 들고 있는 아이템의 시각적 표현을 업데이트 (예: 플레이어 손에 붙이기)
        }
    }

    public void DropHeldItemLocal()
    {
        _heldShopItem = null;
        // 💡 들고 있는 아이템 시각적 표현 제거
    }
}

public class InputCooldownHandler : NetworkBehaviour
{
    [Networked]
    private TickTimer InteractCooldownTimer { get; set; }

    private const float INTERACT_COOLDOWN_DURATION = 0.2f; // 초당 5회 상호작용

    public bool CanInteract()
    {
        return InteractCooldownTimer.Expired(Runner);
    }

    public void SetInteractCooldown()
    {
        if (CanInteract())
        {
            InteractCooldownTimer = TickTimer.CreateFromSeconds(Runner, INTERACT_COOLDOWN_DURATION);
        }
    }
}
