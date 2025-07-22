using Fusion;
using UnityEngine;
using System.Collections.Generic;

// 🎒 플레이어 인벤토리 시스템 (손에 든 것과 저장 슬롯 분리)
public class PlayerInventory : NetworkBehaviour
{
    [Header("Held Object (손에 든 것)")]
    [Networked] private NetworkObject currentHeldObject { get; set; } // 손에 들고 있는 오브젝트(아이템 or 캐릭터)

    // 현재 손에 들고 있는 오브젝트
    public GameObject CurrentHeldObject => currentHeldObject?.gameObject;
    // 현재 손에 든 것이 아이템이면 반환, 아니면 null
    public GameObject CurrentHeldItem => (IsHeldItem() ? currentHeldObject?.gameObject : null);
    // 현재 손에 든 것이 캐릭터면 반환, 아니면 null
    public GameObject CurrentHeldCharacter => (IsHeldCharacter() ? currentHeldObject?.gameObject : null);

    [Header("Inventory Slots (저장 슬롯)")]
    [SerializeField] private int maxInventorySlots = 1; // 현재 슬롯 개수(패시브 등으로 증가 가능)
    [Networked, Capacity(8)] private NetworkArray<NetworkObject> inventorySlots { get; } // 아이템 저장 슬롯(최대 8개)

    // 저장된 아이템들(슬롯)
    public IEnumerable<GameObject> StoredItems
    {
        get
        {
            for (int i = 0; i < maxInventorySlots; i++)
            {
                var obj = inventorySlots[i];
                if (obj != null) yield return obj.gameObject;
            }
        }
    }

    // 아이템을 슬롯에 저장 (비어있는 슬롯에 추가, 아이템만 가능)
    public bool StoreItemToSlot(GameObject item)
    {
        // 아이템 레이어만 저장 가능
        if (item.layer != LayerMask.NameToLayer("Item")) return false;
        for (int i = 0; i < maxInventorySlots; i++)
        {
            if (inventorySlots[i] == null)
            {
                inventorySlots.Set(i, item.GetComponent<NetworkObject>());
                return true;
            }
        }
        return false; // 슬롯이 가득 찼음
    }

    // 슬롯에서 아이템 꺼내 손에 들기
    public bool HoldItemFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxInventorySlots) return false;
        var netObj = inventorySlots[slotIndex];
        if (netObj == null) return false;
        currentHeldObject = netObj;
        inventorySlots.Set(slotIndex, null);
        return true;
    }

    // 캐릭터 들기
    public bool HoldCharacter(GameObject character)
    {
        if (currentHeldObject != null) return false; // 이미 들고 있음
        var netObj = character.GetComponent<NetworkObject>();
        if (netObj == null) return false;
        currentHeldObject = netObj;
        return true;
    }

    // 손에 든 것 내려놓기
    public void DropHeldObject()
    {
        currentHeldObject = null;
    }

    // 슬롯 개수 증가(패시브 아이템 등으로 확장)
    public void AddInventorySlot(int amount = 1)
    {
        maxInventorySlots = Mathf.Clamp(maxInventorySlots + amount, 1, inventorySlots.Length);
    }

    // 슬롯 개수 감소(예외 상황)
    public void RemoveInventorySlot(int amount = 1)
    {
        maxInventorySlots = Mathf.Clamp(maxInventorySlots - amount, 1, inventorySlots.Length);
        // 필요시 초과 슬롯 아이템 정리 로직 추가 가능
    }

    // 현재 손에 든 것이 아이템인지 판별(레이어로 구분)
    private bool IsHeldItem()
    {
        var obj = currentHeldObject?.gameObject;
        return obj != null && obj.layer == LayerMask.NameToLayer("Item");
    }
    // 현재 손에 든 것이 캐릭터인지 판별(레이어로 구분)
    private bool IsHeldCharacter()
    {
        var obj = currentHeldObject?.gameObject;
        if (obj == null) return false;
        int layer = obj.layer;
        return layer == LayerMask.NameToLayer("PlayerNpc") || layer == LayerMask.NameToLayer("Enemy");
    }
} 