using Fusion;
using UnityEngine;
using System.Collections.Generic;

// 🎒 플레이어 인벤토리 시스템 (손에 든 것과 저장 슬롯 분리)
public class PlayerInventory : NetworkBehaviour
{
    // ===== 📦 핵심 데이터 =====
    [Header("Held Object (손에 든 것)")]
    [Networked] private NetworkObject currentHeldObject { get; set; } // 손에 들고 있는 오브젝트(아이템/적/NPC/플레이어 등)
    public GameObject CurrentHeldObject { get { return currentHeldObject?.gameObject; } }

    [Header("Inventory Slots (저장 슬롯)")]
    [SerializeField] private int maxInventorySlots = 3; // 현재 슬롯 개수(패시브 등으로 증가 가능)
    [Networked, Capacity(8)] private NetworkArray<NetworkObject> inventorySlots { get; } // 아이템 저장 슬롯(최대 8개)

    // 현재 선택 슬롯 인덱스(로컬)
    private int selectedSlotIndex = 0;
    public int SelectedSlotIndex { get { return selectedSlotIndex; } set { selectedSlotIndex = value; } }

    [Header("Passive Equipments")]
    [Networked, Capacity(8)] private NetworkArray<NetworkObject> passiveEquipments { get; }

    [Header("Money")]
    [Networked] private int currentMoney { get; set; }
    public int CurrentMoney { get { return currentMoney; } set { if (HasStateAuthority) currentMoney = value; } }

    // ===== 📋 프로퍼티들 =====
    
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

    // 패시브 아이템들
    public IEnumerable<GameObject> PassiveEquipments
    {
        get
        {
            for (int i = 0; i < passiveEquipments.Length; i++)
            {
                var obj = passiveEquipments[i];
                if (obj != null) yield return obj.gameObject;
            }
        }
    }

    // ===== 🎯 손에 든 오브젝트 관리 =====

    // 아이템/캐릭터 등 무엇이든 손에 들기 (슬롯 저장 X, 무조건 손에 듦)
    public bool HoldObject(GameObject obj)
    {
        if (currentHeldObject != null) return false;
        var netObj = obj.GetComponent<NetworkObject>();
        if (netObj == null) return false;
        
        // InputAuthority 할당 (던질 수 있도록)
        if (!netObj.HasInputAuthority)
        {
            netObj.AssignInputAuthority(Object.InputAuthority);
        }
        currentHeldObject = netObj;
        return true;
    }

    // 손에 든 것 내려놓기
    public void DropHeldObject()
    {
        if (currentHeldObject != null)
        {
            var netObj = currentHeldObject.GetComponent<NetworkObject>();
            if (netObj != null && netObj.HasInputAuthority)
            {
                netObj.RemoveInputAuthority(); // InputAuthority 해제
            }
        }
        currentHeldObject = null;
    }

    // ===== 📦 슬롯 관리 =====

    // 슬롯에 아이템 저장 (비어있는 슬롯에 추가, 아이템만 가능)
    public bool StoreItemToSlot(GameObject item)
    {
        if (item.layer != LayerMask.NameToLayer("Item")) return false;
        for (int i = 0; i < maxInventorySlots; i++)
        {
            if (inventorySlots[i] == null)
            {
                inventorySlots.Set(i, item.GetComponent<NetworkObject>());
                return true;
            }
        }
        return false;
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

    // 아이템 스왑: 손에 든 것이 null이거나 아이템일 때만 동작
    public bool SwapHeldItemWithSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxInventorySlots) return false;
        var slotObj = inventorySlots[slotIndex];
        
        // 손에 든 것이 null 또는 아이템만 허용
        if (currentHeldObject == null || IsHeldItem())
        {
            // 손에 든 아이템을 슬롯에 넣기
            if (currentHeldObject != null)
            {
                if (StoreItemToSlot(currentHeldObject.gameObject))
                {
                    currentHeldObject = null;
                }
                else
                {
                    // 슬롯이 가득 차면 스왑 불가
                    return false;
                }
            }
            
            // 슬롯에서 아이템 꺼내 손에 들기
            if (slotObj != null)
            {
                currentHeldObject = slotObj;
                inventorySlots.Set(slotIndex, null);
            }
            return true;
        }
        // 손에 든 것이 아이템이 아니면 스왑 불가
        return false;
    }

    // ===== 🔧 슬롯 설정 관리 =====

    // 슬롯 개수 증가/감소
    public void AddInventorySlot(int amount = 1)
    {
        maxInventorySlots = Mathf.Clamp(maxInventorySlots + amount, 1, inventorySlots.Length);
    }
    
    public void RemoveInventorySlot(int amount = 1)
    {
        maxInventorySlots = Mathf.Clamp(maxInventorySlots - amount, 1, inventorySlots.Length);
    }

    // ===== 🛡️ 패시브 아이템 관리 =====

    // 패시브 아이템 추가/제거
    public bool AddPassiveEquipment(GameObject equipment)
    {
        var netObj = equipment.GetComponent<NetworkObject>();
        if (netObj == null) return false;
        for (int i = 0; i < passiveEquipments.Length; i++)
        {
            if (passiveEquipments[i] == null)
            {
                passiveEquipments.Set(i, netObj);
                return true;
            }
        }
        return false;
    }
    
    public bool RemovePassiveEquipment(GameObject equipment)
    {
        var netObj = equipment.GetComponent<NetworkObject>();
        if (netObj == null) return false;
        for (int i = 0; i < passiveEquipments.Length; i++)
        {
            if (passiveEquipments[i] == netObj)
            {
                passiveEquipments.Set(i, null);
                return true;
            }
        }
        return false;
    }

    // ===== 🔍 유틸리티 함수들 =====

    // 현재 활성 슬롯 개수 반환
    public int GetActiveSlotCount()
    {
        return maxInventorySlots;
    }
    
    // 특정 슬롯에 아이템이 있는지 확인
    public bool HasItemInSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxInventorySlots) return false;
        return inventorySlots[slotIndex] != null;
    }
    
    // 특정 슬롯의 아이템 가져오기
    public GameObject GetItemInSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxInventorySlots) return null;
        return inventorySlots[slotIndex]?.gameObject;
    }

    // ===== 🏷️ 타입 판별 함수들 =====

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