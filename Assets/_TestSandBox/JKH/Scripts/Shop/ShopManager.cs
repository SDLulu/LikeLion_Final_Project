// ShopManager.cs (리팩토링 버전)

using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : NetworkBehaviour
{
    [Header("Shop References")]
    [SerializeField] private NetworkPrefabRef shopkeeperPrefab;
    [SerializeField] private Transform shopkeeperSpawnPoint;
    [SerializeField] private Transform[] itemSpawnPoints;
    [SerializeField] private List<ItemStaticData> availableItemDataList;

    private Shopkeeper shopkeeper;
    // ⭐️ 스폰된 아이템들을 로컬 리스트로 관리 (Despawn 시 필요)
    private List<NetworkObject> _spawnedItems = new List<NetworkObject>();

    private PMK_TileRogic tileRogic => PMK_TileRogic.Instance;
    // ❌ NetworkArray<ShopItemData> ShopItems => default; // 이중 상태 관리 제거

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            InitializeShopItems();
            SpawnShopkeeper();
        }
    }

    // ❌ OnShopItemsNetworkedChanged, Rpc_RequestItemPickup, Rpc_UpdateItemState, Rpc_RequestItemDrop 삭제

    private IEnumerator InitializeShopItems()
    {
        yield return new WaitForSeconds(3f); // 잠시 대기하여 네트워크 초기화 보장

        for (int i = 0; i < itemSpawnPoints.Length; i++)
        {
            // ... (아이템 데이터 랜덤 선택 로직은 동일) ...
            ItemStaticData randomStaticItem = availableItemDataList[Random.Range(0, availableItemDataList.Count)];

            NetworkObject spawnedItemObj = Runner.Spawn(randomStaticItem.itemPrefab, itemSpawnPoints[i].position, Quaternion.identity,
                onBeforeSpawned: (runner, obj) => {
                    obj.transform.SetParent(tileRogic.parentTrans);
                    var shopItem = obj.GetComponent<ShopItem>();
                    if (shopItem != null)
                    {
                        shopItem.InitializeItemData(randomStaticItem.itemType, randomStaticItem.basePrice, randomStaticItem.itemName);
                    }
                });

        _spawnedItems.Add(spawnedItemObj); // ⭐️ Despawn을 위해 로컬 리스트에 추가
        }
    }

    // ShopManager.cs 에 붙여넣을 수정된 Rpc_RequestPurchase 메서드

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RequestPurchase(PlayerRef buyer, NetworkId itemNetworkId)
    {
        
        var purchasedShopItem = Runner.FindObject(itemNetworkId)?.GetComponent<ShopItem>();
        if (purchasedShopItem == null)
        {
            // --- 실패 원인 로그 추가 ---
            Debug.LogError($"Host: [구매 실패] 요청받은 ID({itemNetworkId})에 해당하는 ShopItem을 찾을 수 없습니다.");
            return;
        }

        if (!purchasedShopItem.ItemData.IsAvailable)
        {
            // --- 실패 원인 로그 추가 ---
            Debug.LogWarning($"Host: [구매 실패] 아이템({itemNetworkId})은 이미 판매되었거나 구매할 수 없는 상태입니다.");
            return;
        }

        var playerObject = Runner.GetPlayerObject(buyer);
        if (playerObject == null)
        {
            // --- 실패 원인 로그 추가 ---
            Debug.LogError($"Host: [구매 실패] 구매자({buyer})의 플레이어 오브젝트를 찾을 수 없습니다.");
            return;
        }

        // ⭐️ PlayerInventory 컴포넌트는 플레이어 프리팹 최상단에 있어야 합니다.
        var playerInventory = playerObject.GetComponentInChildren<PlayerInventory>();
        if (playerInventory == null)
        {
            // --- 실패 원인 로그 추가 ---
            Debug.LogError($"Host: [구매 실패] 플레이어 '{playerObject.name}'에서 PlayerInventory 컴포넌트를 찾을 수 없습니다! 프리팹 구조를 확인하세요.");
            return;
        }

        // 기존 구매 로직
        if (playerInventory.CurrentMoney >= purchasedShopItem.ItemData.Price)
        {
            playerInventory.CurrentMoney -= purchasedShopItem.ItemData.Price;
            purchasedShopItem.MarkAsSold();
            Debug.Log("Host: [구매 성공] 구매 완료처리. 아이템을 판매된 것으로 표시했습니다.");
        }
        else
        {
            Debug.LogWarning($"Host: [구매 실패] 돈이 부족합니다. (소지금: {playerInventory.CurrentMoney} / 가격: {purchasedShopItem.ItemData.Price})");
        }
    }

    // 상점 주인을 스폰하고 참조를 저장하는 메서드
    private void SpawnShopkeeper()
    {
        if (Runner == null)
        {
            Debug.LogError("NetworkRunner is not assigned or running in ShopManager!");
            return;
        }

        if (shopkeeperPrefab.IsValid == false)
        {
            Debug.LogError("Shopkeeper Prefab is not assigned in ShopManager!");
            return;
        }

        if (shopkeeperSpawnPoint == null)
        {
            Debug.LogError("Shopkeeper Spawn Point is not assigned in ShopManager!");
            return;
        }

        // ⭐️ 수정: 스폰된 NetworkObject를 변수에 저장합니다.
        NetworkObject spawnedShopkeeperObject = Runner.Spawn(shopkeeperPrefab, shopkeeperSpawnPoint.position, shopkeeperSpawnPoint.rotation, null, (runner, obj) =>
        {
            obj.transform.SetParent(tileRogic.parentTrans);
        });

        // ⭐️ 수정: 스폰된 오브젝트에서 Shopkeeper 컴포넌트를 찾아 변수에 할당합니다.
        if (spawnedShopkeeperObject != null)
        {
            shopkeeper = spawnedShopkeeperObject.GetComponent<Shopkeeper>();
            Debug.Log($"Host: Shopkeeper spawned at {shopkeeperSpawnPoint.position}. NetworkId: {spawnedShopkeeperObject.Id}");
        }
        else
        {
            Debug.LogError("Host: Shopkeeper spawning failed!");
        }
    }

    // 도둑질 시 모든 플레이어를 적대하는 새로운 알림 메서드
    public void NotifyTheftToAllPlayers(NetworkObject stolenItemObject)
    {
        if (!Object.HasStateAuthority) return;

        // ⭐️ 추가: shopkeeper 참조가 유효한지 확인하는 방어 코드
        if (shopkeeper != null)
        {
            shopkeeper.EnrageAgainstAllPlayers();
        }
        else
        {
            Debug.LogError("Host: [도둑질 감지 실패] ShopManager가 Shopkeeper에 대한 참조를 가지고 있지 않습니다! SpawnShopkeeper 메서드를 확인하세요.");
        }

        // 훔쳐진 아이템은 판매 불가 처리
        var stolenShopItem = stolenItemObject.GetComponent<ShopItem>();
        if (stolenShopItem != null)
        {
            stolenShopItem.MarkAsSold();
        }
    }
    // ShopManager.cs 에 새로 추가할 메서드

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_ReportTheftByData(ShopItemData stolenItemData)
    {
        // 이 메서드는 NetworkObject 대신 ShopItemData 구조체를 직접 받습니다.
        Debug.LogWarning($"Host: 데이터로 도둑질 신고 접수! 범죄 아이템: {stolenItemData.ItemName}");

        if (shopkeeper != null)
        {
            shopkeeper.EnrageAgainstAllPlayers();
        }
        else
        {
            Debug.LogError("Host: ShopManager가 Shopkeeper 참조를 잃어버렸습니다!");
        }

        // 여기서는 stolenItemObject를 MarkAsSold 처리할 수 없습니다.
        // 어차피 아이템은 Despawn되어 사라질 것이기 때문입니다.
        // 하지만 상점 주인을 화나게 만드는 목적은 달성할 수 있습니다.
    }
}