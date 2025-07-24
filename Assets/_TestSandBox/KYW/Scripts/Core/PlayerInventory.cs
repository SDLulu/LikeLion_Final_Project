using Fusion;
using UnityEngine;

// 🎒 플레이어 인벤토리 시스템 (손에 든 것만 관리)
public class PlayerInventory : NetworkBehaviour
{
    // ===== 📦 핵심 데이터 =====
    [Header("Held Object (손에 든 것)")]
    [Networked] private NetworkObject currentHeldObject { get; set; } // 손에 들고 있는 오브젝트(아이템/적/NPC/플레이어 등)
    public GameObject CurrentHeldObject { get { return currentHeldObject?.gameObject; } }

    [Header("Money")]
    [Networked] private int currentMoney { get; set; }
    public int CurrentMoney { get { return currentMoney; } set { if (HasStateAuthority) currentMoney = value; } }

    // ===== 🎯 손에 든 오브젝트 관리 =====

    // 아이템/캐릭터 등 무엇이든 손에 들기 (데이터만 관리)
    public bool HoldObject(GameObject obj)
    {
        if (currentHeldObject != null) return false;
        var netObj = obj.GetComponent<NetworkObject>();
        if (netObj == null) return false;
        
        currentHeldObject = netObj;
        return true;
    }

    // 손에 든 것 내려놓기 (데이터만 관리)
    public void DropHeldObject()
    {
        currentHeldObject = null;
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