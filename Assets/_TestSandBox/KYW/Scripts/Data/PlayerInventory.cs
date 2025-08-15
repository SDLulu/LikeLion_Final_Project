using Fusion;
using System;
using UnityEngine;

// 🎒 플레이어 인벤토리 시스템 (손에 든 것만 관리)
public class PlayerInventory : NetworkBehaviour, ISoftReset
{
    // ===== 📦 핵심 데이터 =====
    [Header("Held Object (손에 든 것)")]
    [Networked] private NetworkObject currentHeldObject { get; set; } // 손에 들고 있는 오브젝트(아이템/적/NPC/플레이어 등)
    public GameObject CurrentHeldObject { get { return currentHeldObject?.gameObject; } }

    private ShopItem _heldShopItem;
    public ShopItem HeldShopItem => _heldShopItem;

    [Header("Money")]
    [Networked, OnChangedRender(nameof(OnMoneyChanged))] private int currentMoney { get; set; }
    public int CurrentMoney { get { return currentMoney; } set { if (HasStateAuthority) currentMoney = value; } }

    // ===== 🧩 패시브 아이템 상태 관리 =====
    [Header("Passive Items Status")]
    [Networked, OnChangedRender(nameof(OnPassiveItemsChanged))] public NetworkBool hasRocket { get; set; }        // 로켓 (제트팩 대신)
    [Networked, OnChangedRender(nameof(OnPassiveItemsChanged))] public NetworkBool hasWings { get; set; }         // 날개
    [Networked, OnChangedRender(nameof(OnPassiveItemsChanged))] public NetworkBool hasSpeedShoes { get; set; }    // 이속신발
    [Networked, OnChangedRender(nameof(OnPassiveItemsChanged))] public NetworkBool hasJumpShoes { get; set; }     // 점프신발
    [Networked, OnChangedRender(nameof(OnPassiveItemsChanged))] public NetworkBool hasMagnet { get; set; }        // 자석
    [Networked, OnChangedRender(nameof(OnPassiveItemsChanged))] public NetworkBool hasHeadset { get; set; }       // 헤드셋
    [Networked, OnChangedRender(nameof(OnPassiveItemsChanged))] public NetworkBool hasSunglasses { get; set; }    // 선글라스



    // ===== 🧩 패시브 아이템 프리팹 관리 제거됨 (bool 변수로 대체) =====

    // ===== 🎯 손에 든 오브젝트 관리 =====
    // 아이템/캐릭터 등 무엇이든 손에 들기 (데이터만 관리)
    public bool HoldObject(GameObject obj)
    {
        if (currentHeldObject != null) return false;
        var netObj = obj.GetComponent<NetworkObject>();
        var shopitem = obj.GetComponent<ShopItem>();
        if (netObj == null) return false;

        // ⭐️ 1. 먼저 일반 오브젝트 참조를 설정합니다.
        currentHeldObject = netObj;

        // ⭐️ 2. 들고 있는 오브젝트가 ShopItem인지 확인합니다.
        ShopItem shopItem = obj.GetComponent<ShopItem>();
        if (shopItem != null)
        {
            // ⭐️ 3. _heldShopItem 변수의 값을 여기서 설정합니다.
            _heldShopItem = shopItem;
            // ⭐️ 4. 디버그 로그를 수정하여 ItemData의 유효성을 먼저 확인합니다.
            Debug.Log($"[PlayerInventory] Now holding shop item: '{_heldShopItem.ItemData.ItemName}'");
        }
        else
        {
            // 상점 아이템이 아닌 일반 아이템을 들었을 경우
            _heldShopItem = null;
        }
        currentHeldObject = netObj;

        return true;
    }

    // 손에 든 것 내려놓기 (데이터만 관리)
    public void DropHeldObject()
    {
        currentHeldObject = null;
    }

    // ===== 🧩 패시브 아이템 상태 관리 =====
    public void AddPassiveItem(GameObject itemObject)
    {
        if (!Object.HasStateAuthority) return;

        // 아이템 타입에 따라 bool 변수 자동 설정
        SetPassiveItemStatus(itemObject, true);
    }

    public void RemovePassiveItem(GameObject itemObject)
    {
        if (!Object.HasStateAuthority) return;

        // 아이템 타입에 따라 bool 변수 자동 해제
        SetPassiveItemStatus(itemObject, false);
    }

    public bool HasPassiveItem(GameObject itemObject)
    {
        // 각 아이템 타입별로 bool 변수 확인
        if (itemObject.GetComponent<Rocket>() != null)
            return hasRocket;
        else if (itemObject.GetComponent<Wings>() != null)
            return hasWings;
        else if (itemObject.GetComponent<SpeedShoes>() != null)
            return hasSpeedShoes;
        else if (itemObject.GetComponent<JumpShoes>() != null)
            return hasJumpShoes;
        else if (itemObject.GetComponent<Magnet>() != null)
            return hasMagnet;
        else if (itemObject.GetComponent<Headset>() != null)
            return hasHeadset;
        else if (itemObject.GetComponent<Sunglasses>() != null)
            return hasSunglasses;

        return false;
    }

    // 패시브 아이템 타입에 따라 bool 변수 자동 설정/해제
    private void SetPassiveItemStatus(GameObject itemObject, bool status)
    {
        if (itemObject.GetComponent<Rocket>() != null)
            hasRocket = status;
        else if (itemObject.GetComponent<Wings>() != null)
            hasWings = status;
        else if (itemObject.GetComponent<SpeedShoes>() != null)
            hasSpeedShoes = status;
        else if (itemObject.GetComponent<JumpShoes>() != null)
            hasJumpShoes = status;
        else if (itemObject.GetComponent<Magnet>() != null)
            hasMagnet = status;
        else if (itemObject.GetComponent<Headset>() != null)
            hasHeadset = status;
        else if (itemObject.GetComponent<Sunglasses>() != null)
            hasSunglasses = status;
    }

    // ===== OnChanged 이벤트 메서드들 =====
    private void OnMoneyChanged()
    {
        // 돈이 변경될 때만 UI 업데이트 이벤트 발생
        OnInventoryDataChanged?.Invoke();
    }

    private void OnPassiveItemsChanged()
    {
        // 패시브 아이템이 변경될 때만 UI 업데이트 이벤트 발생
        OnInventoryDataChanged?.Invoke();
    }

    // ===== UI 업데이트 이벤트 =====
    public event System.Action OnInventoryDataChanged;

    /// <summary>
    /// 소프트 리셋: 손에 든 오브젝트와 패시브 아이템 상태를 모두 초기화합니다.
    /// </summary>
    public void SoftResetEquips()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        // 손에 든 오브젝트가 있다면 DeathHandler와 동일하게 해제
        if (currentHeldObject != null)
        {
            var heldGO = currentHeldObject.gameObject;
            var thrower = transform.root != null ? transform.root.GetComponentInChildren<PlayerObjectThrower>() : null;
            if (thrower != null)
            {
                Vector2 randomDirection = UnityEngine.Random.insideUnitCircle.normalized;
                thrower.ReleaseObject(heldGO, true, randomDirection);
            }
            else
            {
                currentHeldObject = null;
            }
        }

        _heldShopItem = null;

        hasRocket = false;
        hasWings = false;
        hasSpeedShoes = false;
        hasJumpShoes = false;
        hasMagnet = false;
        hasHeadset = false;
        hasSunglasses = false;

        OnInventoryDataChanged?.Invoke();
    }

    /// <summary>
    /// ISoftReset 구현: 인벤토리 관련 상태를 초기화합니다.
    /// </summary>
    public void SoftReset()
    {
        SoftResetEquips();
    }
}