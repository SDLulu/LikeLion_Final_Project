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

    // ===== 🧩 패시브 아이템 상태 관리 =====
    [Header("Passive Items Status")]
    [Networked] public NetworkBool hasRocket { get; set; }        // 로켓 (제트팩 대신)
    [Networked] public NetworkBool hasWings { get; set; }         // 날개
    [Networked] public NetworkBool hasSpeedShoes { get; set; }    // 이속신발
    [Networked] public NetworkBool hasJumpShoes { get; set; }     // 점프신발
    [Networked] public NetworkBool hasMagnet { get; set; }        // 자석
    [Networked] public NetworkBool hasHeadset { get; set; }       // 헤드셋
    [Networked] public NetworkBool hasSunglasses { get; set; }    // 선글라스
    
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

    // ===== 🧩 패시브 아이템 상태 관리 =====
    public void AddPassiveItem(GameObject prefab)
    {
        if (!Object.HasStateAuthority) return;
        
        if (!passiveItemPrefabs.Contains(prefab))
            passiveItemPrefabs.Add(prefab);
        
        // 아이템 타입에 따라 bool 변수 자동 설정
        SetPassiveItemStatus(prefab, true);
    }
    
    public void RemovePassiveItem(GameObject prefab)
    {
        if (!Object.HasStateAuthority) return;
        
        passiveItemPrefabs.Remove(prefab);
        
        // 아이템 타입에 따라 bool 변수 자동 해제
        SetPassiveItemStatus(prefab, false);
    }
    
    public bool HasPassiveItem(GameObject prefab)
    {
        return passiveItemPrefabs.Contains(prefab);
    }
    
    // 패시브 아이템 타입에 따라 bool 변수 자동 설정/해제
    private void SetPassiveItemStatus(GameObject prefab, bool status)
    {
        if (prefab.GetComponent<Rocket>() != null)
            hasRocket = status;
        else if (prefab.GetComponent<Wings>() != null)
            hasWings = status;
        else if (prefab.GetComponent<SpeedShoes>() != null)
            hasSpeedShoes = status;
        else if (prefab.GetComponent<JumpShoes>() != null)
            hasJumpShoes = status;
        else if (prefab.GetComponent<Magnet>() != null)
            hasMagnet = status;
        else if (prefab.GetComponent<Headset>() != null)
            hasHeadset = status;
        else if (prefab.GetComponent<Sunglasses>() != null)
            hasSunglasses = status;
    }
} 