using Fusion;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.Unicode;

public class ShopAreaCollider : NetworkBehaviour
{
    private ShopManager _shopManager;
    private List<Collider2D> _objectsInShopArea = new List<Collider2D>();

    // 플레이어 레이어는 이제 이 스크립트에서 직접적으로 도둑질 감지에 사용되지 않습니다.
    // 하지만 다른 목적으로 OnTriggerExit2D에서 플레이어를 감지해야 한다면 유지할 수 있습니다.
    // [SerializeField] private LayerMask playerLayer; // 필요 없으면 제거 가능
    [SerializeField] private LayerMask shopItemLayer; // ShopItem에 할당된 레이어를 여기에 할당


    void Awake()
    {
        _shopManager = FindFirstObjectByType<ShopManager>();
        if (_shopManager == null)
        {
            Debug.LogError("ShopManager not found in scene! ShopAreaWatcher might not function correctly.");
        }
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        Debug.Log($"Host: Object staying in ShopAreaTrigger: {other.gameObject.name}");
        if (Runner != null && Object.HasStateAuthority)
        {
            Debug.Log($"Host: Object staying in ShopAreaTrigger: {other.gameObject.name}");
        }
    }
    // --- OnTriggerEnter2D ---
    // 다른 콜라이더가 이 콜라이더(ShopAreaTrigger)에 진입했을 때 호출됩니다.
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Host: Object entered ShopAreaTrigger: {other.gameObject.name}, Tag: {other.tag}");
        // Host에서만 로그를 출력하거나 로직을 처리하는 것이 일반적입니다.
        // 클라이언트에서도 로컬 로그는 가능하지만, 실제 게임 로직은 Host에서.
        if (Runner != null && Object.HasStateAuthority) // Host/Server에서만 동작
        {
            Debug.Log($"Host: Object entered ShopAreaTrigger: {other.gameObject.name}, Tag: {other.tag}");

            // 목록에 추가 (중복 방지)
            if (!_objectsInShopArea.Contains(other))
            {
                _objectsInShopArea.Add(other);
            }
        }
    }

    // --- OnTriggerExit2D ---
    // 다른 콜라이더가 이 콜라이더(ShopAreaTrigger)에서 벗어났을 때 호출됩니다.
    void OnTriggerExit2D(Collider2D other)
    {
        // 이 로직은 호스트(서버)에서만 실행되어야 합니다.
        //if (!Runner.IsServer || _shopManager == null) return;
        // --- 상점 아이템 감지 로직 ---
        // 콜라이더를 벗어난 오브젝트가 ShopItem 레이어에 속하는지 확인합니다.
        if (((1 << other.gameObject.layer) & shopItemLayer) != 0)
        {
            Debug.Log("오브젝트 이탈");
            ShopItem exitedShopItem = other.GetComponent<ShopItem>();

            if (exitedShopItem != null)
            {
                // ShopManager의 ShopItems NetworkArray에서 해당 아이템 데이터를 조회합니다.
                bool isItemUnpurchasedAndExited = false;
                for (int i = 0; i < _shopManager.ShopItems.Length; i++)
                {
                    var shopItemData = _shopManager.ShopItems.Get(i);
                    // 벗어난 아이템의 NetworkId와 일치하고, 아직 구매되지 않은 상태(IsAvailable == true)인지 확인합니다.
                    if (shopItemData.ItemNetworkId == exitedShopItem.Object.Id && shopItemData.IsAvailable)
                    {
                        isItemUnpurchasedAndExited = true;
                        break; // 해당 아이템을 찾았으므로 더 이상 순회할 필요 없음
                    }
                }

                if (isItemUnpurchasedAndExited)
                {
                    Debug.LogWarning($"Host: Theft detected! Unpurchased item '{exitedShopItem.name}' exited the shop area (rolled out or carried).");
                    // ShopManager에게 도둑질 시도를 알립니다.
                    // 이 경우 특정 플레이어가 직접 들고 나간 것이 아닐 수 있으므로, PlayerRef.None을 전달합니다.
                    _shopManager.NotifyTheftAttempt(PlayerRef.None, exitedShopItem.Object);
                }
            }
        }
        // 만약 플레이어가 아이템 없이 그냥 나가는 경우를 감지하고 싶다면,
        // 여기에 playerLayer를 체크하는 else if 블록을 추가할 수 있습니다.
        // 하지만 현재 의도에 따르면 아이템이 나가는 것만 중요하므로 생략합니다.
    }
}
