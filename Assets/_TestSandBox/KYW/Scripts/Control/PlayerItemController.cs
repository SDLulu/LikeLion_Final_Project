using Fusion;
using UnityEngine;

// 🎒 플레이어 아이템 관리 컨트롤러
// 아이템 수집, 사용, 버리기 담당
public class PlayerItemController : NetworkBehaviour
{
    [Header("Item Detection")]
    [SerializeField] private float pickupRange = 1.5f; // 아이템 수집 범위
    [SerializeField] private LayerMask itemLayerMask = -1; // 아이템 레이어
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true; // 범위 표시
    
    // 🎒 플레이어가 들고 있는 아이템 (네트워크 동기화)
    [Networked] public BaseItem HeldItem { get; private set; }
    
    // 📦 컴포넌트 참조들
    private SpelunkyPlayerController playerController;
    
    public override void Spawned()
    {
        playerController = GetComponent<SpelunkyPlayerController>();
    }
    
    // 🎮 아이템 상호작용 처리 (메인 컨트롤러에서 호출)
    public void HandleItemInteractions(SpelunkyPlayerData input)
    {
        // 아이템 수집 처리 (앉은 상태 + 스페이스)
        if (input.NetworkButtons.IsSet(SpelunkyInputButtons.PickupItem))
        {
            if (HeldItem == null)
            {
                TryPickupItem();
            }
            else
            {
                DropItemRpc();
            }
        }
        
        // 아이템 사용 처리 (마우스 좌클릭)
        if (input.NetworkButtons.IsSet(SpelunkyInputButtons.UseItem) && HeldItem != null)
        {
            UseItemRpc(input.MouseWorldPosition);
        }
    }
    
    // 🔍 주변 아이템 찾기 및 수집 시도
    private void TryPickupItem()
    {
        // 주변 아이템들 검색
        Collider2D[] nearbyItems = Physics2D.OverlapCircleAll(transform.position, pickupRange, itemLayerMask);
        
        BaseItem closestItem = null;
        float closestDistance = float.MaxValue;
        
        // 가장 가까운 아이템 찾기
        foreach (var collider in nearbyItems)
        {
            BaseItem item = collider.GetComponent<BaseItem>();
            if (item != null && !item.IsPickedUp) // 기존 BaseItem 프로퍼티 사용
            {
                float distance = Vector2.Distance(transform.position, item.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestItem = item;
                }
            }
        }
        
        // 아이템 수집 (기존 BaseItem 메서드 사용)
        if (closestItem != null)
        {
            closestItem.PickupItemRpc(Object.InputAuthority);
            HeldItem = closestItem;
        }
    }
    
    // 📦 아이템 수집은 BaseItem에서 직접 처리하므로 제거
    // (TryPickupItem에서 직접 BaseItem.PickupItemRpc 호출)
    
    // 🎯 아이템 사용 처리 (기존 BaseItem 메서드 사용)
    private void UseItemRpc(Vector2 targetPosition)
    {
        if (HeldItem != null)
        {
            // 🪨 돌 아이템의 경우 마우스 방향 설정
            if (HeldItem is RockItem rockItem)
            {
                Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
                rockItem.SetThrowDirection(direction);
            }
            
            // 기존 BaseItem의 UseItem 메서드 호출
            bool success = HeldItem.UseItem(playerController);
            
            if (success)
            {
                HeldItem = null;
                Debug.Log($"🎯 {Object.InputAuthority} 플레이어가 아이템 사용!");
            }
        }
    }
    
    // 🗑️ 아이템 버리기 처리 (기존 BaseItem 메서드 사용)
    private void DropItemRpc()
    {
        if (HeldItem != null)
        {
            // 아이템 버리기 처리
            Vector2 dropPosition = (Vector2)transform.position + Vector2.down * 0.5f;
            HeldItem.DropItemRpc(dropPosition);
            HeldItem = null;
            
            Debug.Log($"🗑️ {Object.InputAuthority} 플레이어가 아이템 버림!");
        }
    }
    
    // 📊 상태 확인 프로퍼티
    public bool HasItem => HeldItem != null;
    public string HeldItemName => HeldItem != null ? HeldItem.name : "없음";
    
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