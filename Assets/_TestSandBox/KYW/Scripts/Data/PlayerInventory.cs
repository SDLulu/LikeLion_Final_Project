using Fusion;
using System;
using System.Collections.Generic;
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

    // ===== 🧩 패시브 아이템 프리팹 관리 (로컬 전용) =====
    [Header("Passive Items (Prefab)")]
    public List<GameObject> passiveItemPrefabs = new List<GameObject>();

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

    // ===== 🧩 패시브 아이템 프리팹 관리 (로컬 전용) =====
    public void AddPassiveItem(GameObject prefab)
    {
        if (!passiveItemPrefabs.Contains(prefab))
            passiveItemPrefabs.Add(prefab);
    }
    public void RemovePassiveItem(GameObject prefab)
    {
        passiveItemPrefabs.Remove(prefab);
    }
    public bool HasPassiveItem(GameObject prefab)
    {
        return passiveItemPrefabs.Contains(prefab);
    }
} 