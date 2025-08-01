using Fusion;
using UnityEngine;

public class ShopAreaCollider : NetworkBehaviour
{
    private ShopManager _shopManager;
    [SerializeField] private LayerMask playerLayer; // ⭐️ 중요: Inspector에서 아이템 레이어 대신 'Player' 레이어로 변경해주세요!

    void Awake()
    {
        _shopManager = FindFirstObjectByType<ShopManager>();
    }

    // ⭐️ OnTriggerExit2D 로직을 아래와 같이 완전히 변경합니다.
    void OnTriggerExit2D(Collider2D other)
    {
        // 호스트가 아니거나, 나간 것이 플레이어 레이어가 아니면 무시
        if (!Object.HasStateAuthority || ((1 << other.gameObject.layer) & playerLayer) == 0)
        {
            return;
        }

        // 나간 콜라이더에서 PlayerInventory 컴포넌트를 찾습니다.
        testPlayerInventory playerInventory = other.GetComponentInParent<testPlayerInventory>();
        if (playerInventory == null) return;

        // 플레이어가 들고 있는 아이템을 확인합니다.
        ShopItem heldItem = playerInventory.HeldShopItem;

        // ⭐️ 만약 플레이어가 아이템을 들고 있고, 그 아이템이 아직 구매되지 않았다면('판매 가능' 상태라면)
        if (heldItem != null && heldItem.ItemData.IsAvailable)
        {
            Debug.LogWarning($"Host: Theft detected! Player '{playerInventory.name}' exited with unpurchased item '{heldItem.name}'.");

            // ShopManager에게 도둑질을 알립니다.
            // 범인은 이제 명확하게 playerInventory의 주인입니다.
            _shopManager.NotifyTheftAttempt(playerInventory.Object.InputAuthority, heldItem.Object);
        }
    }

    // OnTriggerEnter2D와 OnTriggerStay2D는 이 로직에서 더 이상 필요 없으므로 삭제해도 됩니다.
}