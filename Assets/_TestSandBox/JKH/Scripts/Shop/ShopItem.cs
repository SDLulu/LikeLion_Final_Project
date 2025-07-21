using Fusion;
using UnityEngine;

// IInteractable 인터페이스 정의 (플레이어 상호작용 스크립트에서 사용)
public interface IInteractable
{
    void OnInteract(PlayerController player);
}

public class ShopItem : NetworkBehaviour, IInteractable
{
    [Networked] public ShopItemData ItemData { get; set; }

    [Networked] public ItemType itemType { get; set; }
    [Networked] public int Price { get; set; } // Networked로 변경하여 동기화

    private int _shopIndex; // ShopManager의 NetworkArray 내 인덱스
    private ShopManager _shopManager;
    private ShopItemVisual _shopItemVisual;

    public override void Spawned()
    {
        
        _shopItemVisual = GetComponent<ShopItemVisual>();
        if (_shopItemVisual == null)
        {
            Debug.LogError("ShopItemVisual component not found on ShopItem!");
        }

        // Networked 속성 변경 감지 (ItemType, Price 변경 시 비주얼 업데이트)
        // FixedUpdateNetwork에서 ShopManager의 ShopItems 데이터를 참조하여 비주얼 업데이트하는 것이 더 일관적일 수 있습니다.
        // 여기서는 그냥 초기값 설정에 사용
    }

    // Host에서 ShopManager가 아이템을 스폰할 때 초기화하는 함수
    public void Initialize(ShopManager manager, int index, ItemType type, int price)
    {
        _shopManager = manager;
        _shopIndex = index;
        itemType = type; // Networked 속성 설정
        Price = price;   // Networked 속성 설정
    }

    // 아이템 집기 시도 (플레이어의 상호작용 시스템에서 호출)
    public void OnInteract(PlayerController player)
    {
        if (player.Object.HasInputAuthority) // 입력 권한을 가진 플레이어만 요청 가능
        {
            // ShopManager에 아이템 픽업 요청
            _shopManager.Rpc_RequestItemPickup(_shopIndex, player.Object.InputAuthority);
        }
    }

    // 아이템 놓기 처리 (PlayerController에서 호출)
    // 이 RPC는 플레이어가 들고 있는 ShopItem 객체 자체에서 호출됩니다.
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestDrop(PlayerRef player, Vector3 dropPosition)
    {
        // 이 RPC는 Host에서 실행됩니다.
        NetworkObject playerObject = Runner.GetPlayerObject(player);
        PlayerController playerController = playerObject?.GetComponent<PlayerController>();
        PlayerInventory playerInventory = playerObject?.GetComponent<PlayerInventory>();

        if (playerController == null || playerInventory == null)
        {
            Debug.LogError($"Host: Player components not found for {player.PlayerId}");
            return;
        }

        // 플레이어 인벤토리에서 들고 있는 아이템 상태 해제
        if (playerInventory.HeldItemNetworkId == Object.Id)
        {
            playerInventory.HeldItemNetworkId = default;
            playerController.DropHeldItemLocal(); // 로컬 플레이어의 _heldShopItem 참조 해제
        }
        else
        {
            Debug.LogWarning($"Host: Player {player.PlayerId} tried to drop item {Object.Id} but wasn't holding it.");
            return;
        }


        // 상점 영역 내에서 놓았는지 확인
        bool isInShopArea = _shopManager.IsPositionInShopArea(dropPosition);

        if (isInShopArea)
        {
            // 상점 내에서 놓기 - 원래 위치로 복귀
            var itemData = _shopManager.ShopItems[_shopIndex];
            transform.position = itemData.OriginalPosition; // NetworkTransform을 사용한다면 NetworkTransform을 통해 위치 설정

            // 아이템 상태 업데이트 (들고 있지 않음, 홀더 없음)
            _shopManager.Rpc_UpdateItemState(_shopIndex, false, PlayerRef.None);

            // 아이템 활성화
            SetItemActive(true);
            Debug.Log($"Host: Item {itemType} dropped in shop area. Returned to original pos.");
        }
        else
        {
            // 상점 밖에서 놓기 - 구매 처리
            // 아이템이 필드에 드롭되었으므로 더 이상 ShopManager에서 관리하지 않음.
            // 대신, 구매 처리 후 해당 ShopItem 오브젝트는 파괴되어야 합니다.
            _shopManager.Rpc_RequestPurchase(player, _shopIndex); // 이 함수 내부에서 아이템 Despawn 처리
            Debug.Log($"Host: Item {itemType} dropped outside shop area. Initiating purchase.");
            // SetItemActive(true)는 RequestPurchase에서 아이템이 Despawn되므로 여기서는 필요 없음.
        }
    }


    // 실제 GameObject의 활성화/비활성화 (Host에서 실행 후 Fusion이 동기화)
    private void SetItemActive(bool active)
    {
        if (Object.HasStateAuthority)
        {
            gameObject.SetActive(active); // NetworkObject가 활성화/비활성화되면 모든 클라이언트에 동기화
        }
    }

    // ShopManager의 OnShopItemsChanged에서 호출되어 비주얼 업데이트
    public void UpdateItemVisual(ShopItemData itemData)
    {
        if (_shopItemVisual != null)
        {
            _shopItemVisual.UpdateVisual(itemData);
        }

        // 아이템이 들고 있는 상태면 실제 오브젝트는 비활성화 (플레이어 손에 모델이 나타나도록)
        if (Object.HasStateAuthority)
        {
            bool isActive = !itemData.IsPicked && itemData.IsAvailable;
            if (gameObject.activeSelf != isActive)
            {
                gameObject.SetActive(isActive);
            }
        }
    }
}