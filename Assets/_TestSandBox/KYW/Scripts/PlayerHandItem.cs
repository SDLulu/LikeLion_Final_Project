using Fusion;
using UnityEngine;

// 🤲 스펠렁키 플레이어 손에 든 아이템
// 한 번에 하나의 아이템만 들 수 있는 핸드 아이템 시스템
public class PlayerHandItem : NetworkBehaviour
{
    [Header("Hand Item Settings")]
    [SerializeField] private Transform itemHoldPoint; // 아이템을 들고 있을 위치
    
    [Networked] public NetworkId HeldItemId { get; private set; } = default;
    
    // 현재 들고 있는 아이템 참조
    private BaseItem currentItem;
    
    // 컴포넌트 참조
    private SpelunkyPlayerController playerController;
    
    public override void Spawned()
    {
        playerController = GetComponent<SpelunkyPlayerController>();
        
        // 아이템 들고 있는 위치가 없으면 플레이어 위치 사용
        if (itemHoldPoint == null)
        {
            GameObject holdPoint = new GameObject("ItemHoldPoint");
            holdPoint.transform.SetParent(transform);
            holdPoint.transform.localPosition = new Vector3(0, 0.5f, 0);
            itemHoldPoint = holdPoint.transform;
        }
    }
    
    public override void FixedUpdateNetwork()
    {
        // 들고 있는 아이템 동기화
        SyncHeldItem();
    }
    
    // 아이템 수집 시도
    public bool TryPickupItem(BaseItem item)
    {
        // 이미 아이템을 들고 있으면 실패
        if (HasItem()) return false;
        
        // 네트워크 권한 확인
        if (!Object.HasInputAuthority) return false;
        
        // 아이템 수집 처리
        item.PickupItemRpc(Object.InputAuthority);
        SetHeldItem(item);
        
        return true;
    }
    
    // 현재 들고 있는 아이템 사용
    public bool UseHeldItem()
    {
        if (!HasItem() || !Object.HasInputAuthority) return false;
        
        bool success = currentItem.UseItem(playerController);
        
        if (success)
        {
            // 사용 성공시 손에서 제거
            ClearHeldItem();
        }
        
        return success;
    }
    
    // 들고 있는 아이템 드롭
    public void DropHeldItem()
    {
        if (!HasItem() || !Object.HasInputAuthority) return;
        
        Vector3 dropPosition = transform.position + Vector3.down * 0.5f;
        currentItem.DropItemRpc(dropPosition);
        ClearHeldItem();
    }
    
    // 아이템을 들고 있는지 확인
    public bool HasItem()
    {
        return currentItem != null && HeldItemId != default;
    }
    
    // 들고 있는 아이템 설정
    private void SetHeldItem(BaseItem item)
    {
        currentItem = item;
        HeldItemId = item.Object.Id;
        
        // 아이템 위치를 들고 있는 위치로 설정
        if (itemHoldPoint != null)
        {
            item.transform.SetParent(itemHoldPoint);
            item.transform.localPosition = Vector3.zero;
        }
    }
    
    // 들고 있는 아이템 제거
    private void ClearHeldItem()
    {
        currentItem = null;
        HeldItemId = default;
    }
    
    // 네트워크 동기화를 위한 아이템 참조 업데이트
    private void SyncHeldItem()
    {
        // 네트워크 ID로 실제 아이템 객체 찾기
        if (HeldItemId != default && currentItem == null)
        {
            if (Runner.TryFindObject(HeldItemId, out var networkObject))
            {
                currentItem = networkObject.GetComponent<BaseItem>();
                
                // 아이템 위치 동기화
                if (currentItem != null && itemHoldPoint != null)
                {
                    currentItem.transform.SetParent(itemHoldPoint);
                    currentItem.transform.localPosition = Vector3.zero;
                }
            }
        }
        
        // 아이템이 파괴되었으면 손에서 제거
        if (HeldItemId != default && currentItem != null && !currentItem.Object.IsValid)
        {
            ClearHeldItem();
        }
    }
    
    // 📊 상태 접근 프로퍼티들
    public BaseItem CurrentItem => currentItem;
    public string CurrentItemName => HasItem() ? currentItem.ItemName : "";
    public Sprite CurrentItemIcon => HasItem() ? currentItem.ItemIcon : null;
} 