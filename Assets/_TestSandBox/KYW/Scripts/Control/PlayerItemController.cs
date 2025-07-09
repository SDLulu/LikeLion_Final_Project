using Fusion;
using Unity.Collections;
using UnityEngine;

// 🎒 플레이어 아이템 관리 컨트롤러
// 아이템 수집, 사용, 장착, 버리기 담당
public class PlayerItemController : NetworkBehaviour
{
    [Header("Item Detection")]
    [SerializeField] private float pickupRange = 1.5f; // 아이템 수집 범위
    [SerializeField] private LayerMask itemLayerMask = -1; // 아이템 레이어
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true; // 범위 표시
    [SerializeField] private bool showDebugInfo = true; // 디버그 정보 표시
    
    // 🎒 네트워크로 동기화되는 아이템 데이터
    [Networked] public NetworkId HeldItemId { get; private set; }
    [Networked] public bool IsHoldingItem { get; private set; }
    [Networked] public bool IsEquipped { get; private set; }
    [Networked] public NetworkString<_32> ItemName { get; private set; }
    
    // 현재 들고 있는 아이템 참조 (캐시용)
    private BaseItem currentItemCache;
    
    // 📦 컴포넌트 참조들
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
    
    // 🎮 아이템 상호작용 처리 (메인 컨트롤러에서 호출)
    public void HandleItemInteractions(SpelunkyPlayerData input)
    {
        // 아이템 수집/버리기 처리 (앉은 상태 + 스페이스)
        if (input.NetworkButtons.IsSet(SpelunkyInputButtons.PickupItem))
        {
            if (!HasItem)
            {
                TryPickupItemRpc();
            }
            else
            {
                DropCurrentItemRpc();
            }
        }
        
        // 아이템 사용 처리 (마우스 좌클릭)
        if (input.NetworkButtons.IsSet(SpelunkyInputButtons.UseItem) && HasItem)
        {
            UseCurrentItemRpc(input.MouseWorldPosition);
        }
        
        // 아이템 장착/해제 처리 (마우스 우클릭)
        if (input.NetworkButtons.IsSet(SpelunkyInputButtons.EquipItem) && HasItem)
        {
            TryEquipCurrentItemRpc();
        }
    }
    
    // 🔄 아이템 참조 동기화
    private void SyncItemReference()
    {
        // 아이템을 들고 있다고 하는데 캐시가 없으면 찾기
        if (IsHoldingItem && HeldItemId != default && currentItemCache == null)
        {
            if (Runner.TryFindObject(HeldItemId, out var networkObject))
            {
                currentItemCache = networkObject.GetComponent<BaseItem>();
            }
        }
        
        // 아이템을 들고 있지 않다면 캐시 제거
        if (!IsHoldingItem)
        {
            currentItemCache = null;
        }
        
        // 캐시된 아이템이 유효하지 않으면 데이터 초기화
        if (currentItemCache != null && !currentItemCache.Object.IsValid)
        {
            ClearItemData();
        }
    }
    
    // 🔍 주변 아이템 찾기 및 수집 시도
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void TryPickupItemRpc()
    {
        // 주변 아이템들 검색
        Collider2D[] nearbyItems = Physics2D.OverlapCircleAll(transform.position, pickupRange, itemLayerMask);
        
        BaseItem closestItem = null;
        float closestDistance = float.MaxValue;
        
        // 가장 가까운 아이템 찾기
        foreach (var collider in nearbyItems)
        {
            BaseItem item = collider.GetComponent<BaseItem>();
            if (item != null && !item.IsPickedUp)
            {
                float distance = Vector2.Distance(transform.position, item.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestItem = item;
                }
            }
        }
        
        // 아이템 수집
        if (closestItem != null)
        {
            closestItem.PickupItemRpc(Object.InputAuthority);
            SetItemData(closestItem);
            Debug.Log($"📦 {closestItem.ItemName} 수집!");
        }
    }
    
    // 🎯 아이템 사용 처리
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void UseCurrentItemRpc(Vector2 targetPosition)
    {
        if (currentItemCache != null)
        {
            currentItemCache.UseItem(playerController, targetPosition);
            
            // 아이템이 소모되었는지 확인 (던져졌거나 파괴됨)
            if (!currentItemCache.IsPickedUp || !currentItemCache.Object.IsValid)
            {
                ClearItemData();
            }
        }
    }
    
    // ⚔️ 아이템 장착/해제 처리
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void TryEquipCurrentItemRpc()
    {
        if (currentItemCache != null && currentItemCache.CanBeEquipped)
        {
            bool equipped = currentItemCache.TryEquip(playerController);
            IsEquipped = currentItemCache.IsEquipped; // BaseItem의 상태와 동기화
            
            if (equipped)
            {
                Debug.Log($"⚔️ {currentItemCache.ItemName} 장착!");
            }
            else
            {
                Debug.Log($"⚔️ {currentItemCache.ItemName} 장착 해제!");
            }
        }
        else
        {
            Debug.Log("🚫 장착할 수 없는 아이템입니다.");
        }
    }
    
    // 🗑️ 아이템 버리기 처리
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void DropCurrentItemRpc()
    {
        if (currentItemCache != null)
        {
            // 장착된 아이템이면 해제
            if (currentItemCache.CanBeEquipped)
            {
                currentItemCache.UnEquip();
            }
            
            // 아이템 버리기
            Vector2 dropPosition = (Vector2)transform.position + Vector2.down * 0.5f;
            currentItemCache.DropItemRpc(dropPosition);
            
            ClearItemData();
            Debug.Log($"🗑️ {ItemName} 버림!");
        }
    }
    
    // 아이템 데이터 설정
    private void SetItemData(BaseItem item)
    {
        if (item != null)
        {
            HeldItemId = item.Object.Id;
            IsHoldingItem = true;
            ItemName = item.ItemName;
            IsEquipped = false;
            currentItemCache = item;
        }
    }
    
    // 아이템 데이터 초기화
    private void ClearItemData()
    {
        HeldItemId = default;
        IsHoldingItem = false;
        IsEquipped = false;
        ItemName = "";
        currentItemCache = null;
    }
    
    // 📊 상태 확인 프로퍼티들 (다른 시스템에서 사용)
    public bool HasItem => IsHoldingItem && currentItemCache != null;
    public string HeldItemName => ItemName.ToString();
    public BaseItem CurrentItem => currentItemCache;
    
    // 🔍 디버그 정보 표시
    private void OnGUI()
    {
        if (!showDebugInfo || !Object.HasInputAuthority) return;
        
        GUILayout.BeginArea(new Rect(10, 100, 300, 100));
        GUILayout.Box("🎒 아이템 정보");
        GUILayout.Label($"들고 있음: {HasItem}");
        GUILayout.Label($"아이템명: {HeldItemName}");
        GUILayout.Label($"장착 상태: {IsEquipped}");
        if (currentItemCache != null)
        {
            GUILayout.Label($"장착 가능: {currentItemCache.CanBeEquipped}");
        }
        GUILayout.EndArea();
    }
    
    // 🎨 디버그 기즈모 (에디터에서 수집 범위 표시)
    private void OnDrawGizmosSelected()
    {
        if (showDebugGizmos)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }
    }
} 