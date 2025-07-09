using Fusion;
using Unity.Collections;
using UnityEngine;

// 🎒 플레이어 아이템 데이터
// 플레이어가 현재 들고 있는 아이템의 상태를 관리
[System.Serializable]
public struct PlayerItemData : INetworkStruct
{
    public NetworkId HeldItemId;       // 들고 있는 아이템 ID (필드로 변경)
    [Networked] public bool IsHoldingItem { get; set; }        // 아이템을 들고 있는지 여부
    [Networked] public bool IsEquipped { get; set; }           // 장착 상태인지 여부
    [Networked] public NetworkString<_32> ItemName { get; set; } // Fusion 전용 문자열 (32바이트)
    
    // 데이터 초기화
    public void Clear()
    {
        HeldItemId = default;
        IsHoldingItem = false;
        IsEquipped = false;
        ItemName = "";
    }
    
    // 아이템 설정
    public void SetItem(BaseItem item)
    {
        if (item != null)
        {
            HeldItemId = item.Object.Id;
            IsHoldingItem = true;
            ItemName = item.ItemName;
            IsEquipped = false; // 새로 든 아이템은 장착 해제 상태
        }
        else
        {
            Clear();
        }
    }
    
    // 장착 상태 설정
    public void SetEquipped(bool equipped)
    {
        IsEquipped = equipped;
    }
}

// 🎮 플레이어 아이템 매니저
// PlayerItemData를 활용하여 아이템을 관리하는 컴포넌트
public class PlayerItemManager : NetworkBehaviour
{
    [Header("Item Management")]
    [SerializeField] private float pickupRange = 1.5f;
    [SerializeField] private LayerMask itemLayerMask = -1;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    [Networked] public PlayerItemData ItemData { get; set; }
    
    // 현재 들고 있는 아이템 참조 (캐시용)
    private BaseItem currentItemCache;
    
    // 컴포넌트 참조
    private SpelunkyPlayerController playerController;
    
    public override void Spawned()
    {
        playerController = GetComponent<SpelunkyPlayerController>();
    }
    
    public override void FixedUpdateNetwork()
    {
        // 아이템 참조 동기화
        SyncItemReference();
    }
    
    // 🔄 아이템 참조 동기화
    private void SyncItemReference()
    {
        // 아이템을 들고 있다고 하는데 캐시가 없으면 찾기
        if (ItemData.IsHoldingItem && ItemData.HeldItemId != default && currentItemCache == null)
        {
            if (Runner.TryFindObject(ItemData.HeldItemId, out var networkObject))
            {
                currentItemCache = networkObject.GetComponent<BaseItem>();
            }
        }
        
        // 아이템을 들고 있지 않다면 캐시 제거
        if (!ItemData.IsHoldingItem)
        {
            currentItemCache = null;
        }
        
        // 캐시된 아이템이 유효하지 않으면 데이터 초기화
        if (currentItemCache != null && !currentItemCache.Object.IsValid)
        {
            var data = ItemData;
            data.Clear();
            ItemData = data;
            currentItemCache = null;
        }
    }
    
    // 📊 상태 확인 프로퍼티들
    public bool HasItem => ItemData.IsHoldingItem && currentItemCache != null;
    public bool IsEquipped => ItemData.IsEquipped;
    public BaseItem CurrentItem => currentItemCache;
    public string CurrentItemName => ItemData.ItemName.ToString();
    
    // 🔍 디버그 정보 표시
    private void OnGUI()
    {
        if (!showDebugInfo || !Object.HasInputAuthority) return;
        
        GUILayout.BeginArea(new Rect(10, 100, 300, 100));
        GUILayout.Box("🎒 아이템 정보");
        GUILayout.Label($"들고 있음: {HasItem}");
        GUILayout.Label($"아이템명: {CurrentItemName}");
        GUILayout.Label($"장착 상태: {IsEquipped}");
        if (currentItemCache != null)
        {
            GUILayout.Label($"장착 가능: {currentItemCache.CanBeEquipped}");
        }
        GUILayout.EndArea();
    }
} 