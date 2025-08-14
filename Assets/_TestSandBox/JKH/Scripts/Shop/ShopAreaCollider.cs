using Fusion;
using UnityEngine;

public class ShopAreaCollider : NetworkBehaviour
{
    private ShopManager _shopManager;
    [SerializeField] private LayerMask playerLayer; // Inspector에서 'Player' 레이어로 설정해주세요.

    void Awake()
    {
        // 씬에 있는 ShopManager를 자동으로 찾아 할당합니다.
        _shopManager = FindFirstObjectByType<ShopManager>();
    }

    // ShopAreaCollider.cs의 OnTriggerExit2D 메서드를 아래 코드로 교체합니다.

    void OnTriggerExit2D(Collider2D other)
    {
        // 호스트(Host)에서만 실행
        if (!Object.HasStateAuthority) return;
        if (_shopManager == null) return;

        // --- 경우 1: '플레이어'가 상점 밖으로 나갈 때 (기존 로직) ---
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            Debug.Log($"Host: 플레이어 '{other.gameObject.name}'가 상점 구역을 나갔습니다.");

            PlayerInventory playerInventory = other.GetComponentInChildren<PlayerInventory>();
            if (playerInventory == null) return; // 인벤토리가 없으면 검사할 필요 없음

            ShopItem heldItem = playerInventory.HeldShopItem;

            // 플레이어가 구매하지 않은 아이템을 '들고' 나갔다면 도둑질입니다.
            if (heldItem != null && heldItem.ItemData.IsAvailable)
            {
                Debug.LogWarning($"Host: 도둑질 감지! 플레이어가 미구매 아이템 '{heldItem.name}'을 들고 나갔습니다.");
                _shopManager.NotifyTheftToAllPlayers(heldItem.Object);
            }
            return; // 플레이어에 대한 처리는 여기서 끝냅니다.
        }

        // --- ⭐️ 경우 2: '아이템'이 상점 밖으로 던져졌을 때 (새로운 로직) ---
        ShopItem shopItem = other.GetComponent<ShopItem>();
        if (shopItem != null)
        {
            // 던져진 아이템이 아직 구매되지 않은 아이템이라면 도둑질입니다.
            if (shopItem.ItemData.IsAvailable)
            {
                Debug.LogWarning($"Host: 도둑질 감지! 미구매 아이템 '{shopItem.name}'이 상점 밖으로 던져졌습니다.");
                _shopManager.NotifyTheftToAllPlayers(shopItem.Object);
            }
        }
    }
}