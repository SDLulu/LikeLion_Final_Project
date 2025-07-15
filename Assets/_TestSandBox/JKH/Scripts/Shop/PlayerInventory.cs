using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [Networked]
    public int Gold { get; set; } = 0; // 초기 골드 설정

    // 플레이어가 들고 있는 아이템
    [Networked]
    public NetworkId HeldItemNetworkId { get; set; } = default;

    public ItemType GetHeldItemType()
    {
        if (HeldItemNetworkId == default) return ItemType.None;
        NetworkObject obj = Runner.FindObject(HeldItemNetworkId);
        if (obj != null)
        {
            ShopItem shopItem = obj.GetComponent<ShopItem>();
            if (shopItem != null) return shopItem.itemType;
        }
        return ItemType.None;
    }

    public bool HasItem(ItemType type)
    {
        // 실제 인벤토리 시스템 구현 (예: NetworkDictionary<ItemType, int> InventoryItems)
        // 이 예시에서는 간단히 들고 있는 아이템만 확인
        return GetHeldItemType() == type;
    }

    // 아이템을 인벤토리에 추가하거나 (이 예시에서는 들고 있는 아이템으로)
    public void PickupItem(ShopItem item)
    {
        // 서버에서만 실행
        if (Object.HasStateAuthority)
        {
            HeldItemNetworkId = item.Object.Id;
            // 실제 아이템 오브젝트는 플레이어의 자식으로 설정하거나 다른 로직을 따름
            item.transform.SetParent(transform); // 예시: 플레이어 손에 붙이기
            item.transform.localPosition = Vector3.zero; // 플레이어 로컬 위치로
            Debug.Log($"Host: Player {Object.InputAuthority.PlayerId} picked up {item.itemType}");
        }
    }

    public void DropItem()
    {
        // 서버에서만 실행
        if (Object.HasStateAuthority && HeldItemNetworkId != default)
        {
            NetworkObject heldObject = Runner.FindObject(HeldItemNetworkId);
            if (heldObject != null)
            {
                ShopItem shopItem = heldObject.GetComponent<ShopItem>();
                if (shopItem != null)
                {
                    shopItem.RPC_RequestDrop(Object.InputAuthority, transform.position + Vector3.down * 0.5f); // 현재 플레이어 위치 아래에 드롭
                }
            }
            HeldItemNetworkId = default;
            Debug.Log($"Host: Player {Object.InputAuthority.PlayerId} dropped item.");
        }
    }

    public bool CanPickupItem()
    {
        return HeldItemNetworkId == default; // 이미 들고 있는 아이템이 없으면 픽업 가능
    }

    // 골드 추가/제거는 RPC를 통해 서버에서 직접 호출되도록 합니다 (PlayerController에서)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ModifyGold(int amount)
    {
        Gold += amount;
        if (Gold < 0) Gold = 0; // 최소 골드 0
        Debug.Log($"Host: Player {Object.InputAuthority.PlayerId} gold changed by {amount}. New gold: {Gold}");
    }
}