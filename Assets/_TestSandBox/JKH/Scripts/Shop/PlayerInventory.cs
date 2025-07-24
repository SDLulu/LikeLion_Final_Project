using Fusion;
using UnityEngine;
using System.Collections.Generic; // List를 사용한다면 필요
// using TMPro; // 골드 UI 표시용 (필요하다면)

public class PlayerInventory : NetworkBehaviour
{
    // ⭐️ 골드 Networked 변수 추가 (상점에서 사용)
    [Networked]
    public int Gold { get; set; } = 100; // 초기 골드

    // ⭐️ 아이템 저장용 Networked List 또는 NetworkDictionary 등 (예시로 Networked KCC ItemType)
    // 실제 게임에 맞춰 List<ItemType>이나 Dictionary<ItemType, int> 등으로 구현
    // 여기서는 간단히 Networked Property를 사용한다고 가정합니다.
    [Networked]
    public ItemType CurrentInventoryItemType { get; private set; } = ItemType.None; // 현재 인벤토리 아이템 타입

    // ⭐️ 현재 들고 있는 ShopItem 오브젝트 참조 (Host만 직접 참조하고 Client는 NetworkId로 찾음)
    private ShopItem _heldShopItem;
    public ShopItem HeldShopItem => _heldShopItem;


    [Header("Inventory Settings")]
    [SerializeField] private int maxInventorySlots = 1; // 최대 아이템 슬롯 (현재는 1개만 가정)
    [SerializeField] private Transform itemHoldPoint; // 아이템을 들고 있을 위치

    // 필요한 경우 다른 컴포넌트 참조
    private PlayerItemPickup _playerItemPickup; // 아이템 픽업 담당 컴포넌트 (Hand 하위)
    private PlayerItemThrower _playerItemThrower; // 아이템 던지기 담당 컴포넌트 (Hand 하위)

    public override void Spawned()
    {
        if (itemHoldPoint == null)
        {
            // 플레이어 Hand 하위의 PlayerItemPickup에서 아이템을 들고 있는 위치를 가져오는 방식
            _playerItemPickup = GetComponentInChildren<PlayerItemPickup>();
            if (_playerItemPickup != null)
            {
                itemHoldPoint = _playerItemPickup.transform; // PlayerItemPickup 위치를 아이템 홀드 위치로 사용
            }
            else
            {
                Debug.LogWarning("PlayerInventory: itemHoldPoint not assigned and PlayerItemPickup not found. Items might not be positioned correctly.");
            }
        }

        _playerItemThrower = GetComponentInChildren<PlayerItemThrower>();

        // ⭐️ Host에서만 초기 골드 설정 (Spawned에서 이미 초기화되었으므로 필요 없을 수도 있음)
        if (Object.HasStateAuthority)
        {
            Gold = 100; // 게임 시작 시 초기 골드
        }
    }

    public override void Render()
    {
        // ⭐️ UI 업데이트 (골드 표시 등)은 InputAuthority 플레이어만
        if (Object.HasInputAuthority)
        {
            // Debug.Log($"Player {Object.InputAuthority.PlayerId} Gold: {Gold}");
            // TODO: 실제 게임 UI에 골드 값을 표시하는 로직 추가
            // if (goldTextUI != null) goldTextUI.text = $"Gold: {Gold}";
        }
    }

    // 아이템을 주울 수 있는지 확인 (현재는 1슬롯만 가정)
    public bool CanPickupItem()
    {
        // ⭐️ 이미 아이템을 들고 있다면 false
        return _heldShopItem == null; // 또는 CurrentInventoryItemType == ItemType.None;
    }

    // ⭐️ 아이템 픽업 처리 (Host에서 PlayerItemPickup이 호출)
    public void PickupItem(ShopItem item)
    {
        if (!Object.HasStateAuthority) return;
        if (!CanPickupItem()) return;

        _heldShopItem = item;
        CurrentInventoryItemType = item.ItemData.ItemType;
        item.OnPickedUp(Object.InputAuthority);

        // ⭐️ 콜라이더를 끄고 켜는 로직을 모두 삭제하고, 이 세 줄만 남깁니다.
        item.transform.SetParent(itemHoldPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        if (item.Object.HasInputAuthority == false)
        {
            item.Object.AssignInputAuthority(Object.InputAuthority);
        }

        Debug.Log($"Host: Player {Object.InputAuthority.PlayerId} picked up {item.ItemData.ItemName}.");
    }


    // ⭐️ 아이템 드롭 처리 (Host에서 PlayerItemThrower 또는 다른 로직이 호출)
    public void DropItem(ShopItem item, Vector3 dropPosition)
    {
        if (!Object.HasStateAuthority) return;
        if (_heldShopItem == null || _heldShopItem != item) return;

        // 아이템을 내려놓기 전에 상태를 먼저 업데이트
        item.OnDropped(Object.InputAuthority);
        item.transform.SetParent(null);
        item.transform.position = dropPosition;

        if (item.Object.HasInputAuthority)
        {
            item.Object.RemoveInputAuthority();
        }

        // 모든 처리가 끝난 후 인벤토리에서 참조를 제거합니다.
        _heldShopItem = null;
        CurrentInventoryItemType = ItemType.None;
    }

    // ⭐️ 아이템 구매 (Host에서 PlayerItemPickup 등이 ShopManager를 통해 호출)
    // 이 메서드는 직접 아이템 오브젝트를 받아 인벤토리에 추가하는 것이 아니라,
    // ShopManager가 구매 처리를 완료한 후 플레이어 인벤토리에 '아이템 타입'을 추가하는 식으로 작동해야 합니다.
    // ShopManager의 Rpc_RequestPurchase가 호출된 후, 구매가 성공하면 ShopManager에서 이 메서드를 호출하는 것이 좋습니다.
    public void AddItemToInventory(ItemType purchasedItemType)
    {
        if (!Object.HasStateAuthority) return; // 호스트만 인벤토리 변경

        if (CurrentInventoryItemType == ItemType.None) // 인벤토리가 비어있을 때만
        {
            CurrentInventoryItemType = purchasedItemType;
            Debug.Log($"Host: Player {Object.InputAuthority.PlayerId} added {purchasedItemType} to inventory after purchase.");
            // TODO: 필요하다면 구매된 아이템의 프리팹을 인벤토리에 시각적으로 스폰하는 로직
            // (이때 스폰되는 오브젝트는 네트워크 오브젝트가 아니어도 됨, 단순 시각적 표현)
        }
        else
        {
            Debug.LogWarning($"Host: Player {Object.InputAuthority.PlayerId}'s inventory is full. Could not add {purchasedItemType}.");
            // 인벤토리가 꽉 찼다면 어떻게 할지 (예: 골드로 환불 또는 버리기)
        }
    }

    // ⭐️ 아이템 사용 후 인벤토리에서 제거 (PlayerItemUsage 또는 아이템 자체에서 호출)
    public void UseAndRemoveCurrentItem()
    {
        if (!Object.HasStateAuthority) return;

        if (_heldShopItem != null)
        {
            // 아이템 사용 효과가 발생한 후 인벤토리에서 제거
            // 예: 포션 마시고 사라짐
            Runner.Despawn(_heldShopItem.Object); // 네트워크에서 아이템 오브젝트 제거

            _heldShopItem = null;
            CurrentInventoryItemType = ItemType.None;
            Debug.Log($"Host: Player {Object.InputAuthority.PlayerId} used and removed current item.");
        }
    }
}