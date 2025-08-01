using Fusion;
using System.Collections.Generic;
using TMPro; // TMP_Text 사용
using UnityEngine;

public class ShopManager : NetworkBehaviour
{

    [Header("Shop References")]
    [SerializeField] private NetworkPrefabRef shopkeeperPrefab; // 유니티 에디터에서 상점 주인 프리팹을 할당합니다.
    [SerializeField] private Transform shopkeeperSpawnPoint; // 유니티 에디터에서 상점 주인 스폰 포인트를 할당합니다.

    private NetworkObject _spawnedShopkeeper;
    private Shopkeeper shopkeeper;

    [SerializeField] private int maxShopItems = 3;
    [SerializeField] private Transform[] itemSpawnPoints; // 아이템 스폰 위치 배열
    [SerializeField] private List<ItemStaticData> availableItemDataList; // 상점에서 판매될 아이템 데이터 목록
    [SerializeField] private GameObject shopAreaTrigger; // 상점 영역을 나타내는 Collider2D 오브젝트 (Is Trigger)
    [SerializeField] private Collider2D _shopAreaCollider;

    // 상점 아이템들을 NetworkArray로 관리
    // NetworkArray의 변경이 감지될 때 'OnShopItemsNetworkedChanged' 함수가 호출되도록 설정
    [Networked, Capacity(3), OnChangedRender(nameof(OnShopItemsNetworkedChanged))]
    public NetworkArray<ShopItemData> ShopItems => default;

    // 스폰된 ShopItem 오브젝트들의 참조를 로컬에서 관리 (NetworkId를 키로 사용)
    // 💡 이 Dictionary는 Host에서만 의미가 있으며, 다른 클라이언트는 Runner.FindObject()로 찾아야 합니다.
    // 따라서 이 Dictionary는 InitializeShopItems에서만 참조를 저장하는 용도로 사용하고,
    // 실제 로직에서는 Runner.FindObject()를 사용하는 것이 더 안전합니다.
    // private Dictionary<NetworkId, ShopItem> _spawnedShopItems = new Dictionary<NetworkId, ShopItem>();


    private void Awake()
    {
        if (shopAreaTrigger != null)
        {
            _shopAreaCollider = shopAreaTrigger.GetComponent<Collider2D>();
            if (_shopAreaCollider == null || !_shopAreaCollider.isTrigger)
            {
                Debug.LogError("ShopAreaTrigger must have a Collider2D and be set as Is Trigger.");
            }
        }
    }
    public override void Spawned()
    {
        if (shopAreaTrigger != null)
        {
            _shopAreaCollider = shopAreaTrigger.GetComponent<Collider2D>();
            if (_shopAreaCollider == null || !_shopAreaCollider.isTrigger)
            {
                Debug.LogError("ShopAreaTrigger must have a Collider2D and be set as Is Trigger.");
            }
        }
        else
        {
            Debug.LogWarning("ShopAreaTrigger is not assigned. IsPositionInShopArea will not work.");
        }

        if (Object.HasStateAuthority) // Host/Server에서만 상점 아이템 초기화
        {
            InitializeShopItems();
            SpawnShopkeeper();
        }

        // 초기 동기화 시 비주얼 업데이트는 OnShopItemsNetworkedChanged 콜백에 의해 자동으로 처리됩니다.
        // OnShopItemsNetworkedChanged(); // 필요하다면 초기 상태 강제 업데이트
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_spawnedShopkeeper != null && _spawnedShopkeeper.IsValid)
        {
            runner.Despawn(_spawnedShopkeeper);
        }
        // 상점 매니저가 Despawn될 때 스폰된 아이템들도 Despawn
        if (Object.HasStateAuthority)
        {
            for (int i = 0; i < ShopItems.Length; i++)
            {
                var itemData = ShopItems.Get(i);
                NetworkObject itemObject = Runner.FindObject(itemData.ItemNetworkId);
                if (itemObject != null && itemObject.IsValid)
                {
                    Runner.Despawn(itemObject);
                }
            }
        }
        // _spawnedShopItems.Clear(); // 더 이상 사용하지 않아도 됨
    }

    // Host에서 상점 아이템 초기화
    void InitializeShopItems()
    {
        if (itemSpawnPoints.Length < maxShopItems)
        {
            Debug.LogError("Not enough item spawn points for maxShopItems!");
            return;
        }
        if (availableItemDataList == null || availableItemDataList.Count == 0)
        {
            Debug.LogError("No item data assigned to availableItemDataList!");
            return;
        }

        for (int i = 0; i < maxShopItems; i++)
        {
            ItemStaticData randomStaticItem = GetRandomItemData();
            if (randomStaticItem == null) continue;

            // NetworkObject를 스폰하여 ShopItem 인스턴스 생성
            ShopItem spawnedShopItem = Runner.Spawn(
                randomStaticItem.itemPrefab, // 아이템 타입에 맞는 프리팹 (ShopItem 컴포넌트 포함)
                itemSpawnPoints[i].position,
                Quaternion.identity,
                onBeforeSpawned: (runner, obj) =>
                {
                    // 스폰되기 전에 ShopItem 컴포넌트에 데이터 할당
                    ShopItem shopItemComponent = obj.GetComponent<ShopItem>();
                    if (shopItemComponent != null)
                    {
                        // ⭐️ ShopItem에 ShopItemData를 직접 초기화하도록 변경
                        // 이 데이터는 ShopItem.ItemData [Networked] 변수에 저장됩니다.
                        shopItemComponent.InitializeItemData(randomStaticItem.itemType, randomStaticItem.basePrice, randomStaticItem.itemName);
                    }
                }
            ).GetComponent<ShopItem>();

            if (spawnedShopItem == null)
            {
                Debug.LogError($"Failed to spawn ShopItem from prefab: {randomStaticItem.itemPrefab}");
                continue;
            }

            // ⭐️ ShopManager의 NetworkArray에 데이터 저장
            // 이 데이터는 ShopItem 자체의 Networked ItemData와 논리적으로 동기화되어야 합니다.
            // ShopItems NetworkArray는 상점 "목록"의 상태를 관리하고,
            // ShopItem.ItemData는 개별 "아이템 오브젝트"의 상태를 관리합니다.
            var itemDataInShopManagerArray = new ShopItemData
            {
                ItemNetworkId = spawnedShopItem.Object.Id, // 스폰된 NetworkObject의 ID 저장
                ItemType = randomStaticItem.itemType,
                Price = CalculatePrice(randomStaticItem.itemType),
                IsAvailable = true, // 처음에는 판매 가능
                IsPicked = false,
                CurrentHolder = default,
                OriginalPosition = itemSpawnPoints[i].position,
                ItemName = randomStaticItem.itemName
            };

            ShopItems.Set(i, itemDataInShopManagerArray);
            // _spawnedShopItems[spawnedShopItem.Object.Id] = spawnedShopItem; // 더 이상 사용하지 않음
        }
    }

    // --- 아이템 데이터 관련 헬퍼 함수 ---
    ItemStaticData GetRandomItemData()
    {
        if (availableItemDataList == null || availableItemDataList.Count == 0)
        {
            return null;
        }
        return availableItemDataList[Random.Range(0, availableItemDataList.Count)];
    }

    int CalculatePrice(ItemType type)
    {
        ItemStaticData staticData = availableItemDataList.Find(data => data.itemType == type);
        if (staticData != null)
        {
            return staticData.basePrice;
        }
        return 100;
    }

    public ItemStaticData GetStaticItemData(ItemType itemType)
    {
        return availableItemDataList.Find(data => data.itemType == itemType);
    }

    // --- 플레이어 골드 관리 (Fusion 방식) ---
    private int GetPlayerGold(PlayerRef player)
    {
        NetworkObject playerObject = Runner.GetPlayerObject(player);
        if (playerObject != null)
        {
            testPlayerInventory inventory = playerObject.GetComponent<testPlayerInventory>();
            if (inventory != null)
            {
                return inventory.Gold;
            }
        }
        return 0;
    }

    private void SetPlayerGold(PlayerRef player, int newGold)
    {
        NetworkObject playerObject = Runner.GetPlayerObject(player);
        if (playerObject != null)
        {
            testPlayerInventory inventory = playerObject.GetComponent<testPlayerInventory>();
            if (inventory != null)
            {
                inventory.Gold = newGold;
            }
        }
    }

    // --- RPC: 아이템 구매 요청 (클라이언트 -> 호스트) ---
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RequestPurchase(PlayerRef buyer, NetworkId itemNetworkId) // ⭐️ NetworkId로 변경
    {
        int itemIndexInShopArray = -1;
        ShopItemData itemDataFromShopArray = default; // ShopManager의 ShopItems 배열에 있는 데이터

        // ShopItems NetworkArray에서 해당 NetworkId를 가진 아이템을 찾습니다.
        for (int i = 0; i < ShopItems.Length; i++)
        {
            var currentItem = ShopItems.Get(i);
            if (currentItem.ItemNetworkId == itemNetworkId)
            {
                itemDataFromShopArray = currentItem;
                itemIndexInShopArray = i;
                break;
            }
        }

        if (itemIndexInShopArray == -1 || !itemDataFromShopArray.IsAvailable || itemDataFromShopArray.IsPicked)
        {
            Debug.Log($"Host: Item {itemNetworkId} is not available, picked, or not found in shop array. IsAvailable: {itemDataFromShopArray.IsAvailable}, IsPicked: {itemDataFromShopArray.IsPicked}");
            RPC_SendPurchaseResult(buyer, false, itemDataFromShopArray.ItemName); // 구매 실패 알림
            return;
        }

        int playerGold = GetPlayerGold(buyer);

        if (playerGold >= itemDataFromShopArray.Price)
        {
            // 2. 골드 차감 (기존과 동일)
            SetPlayerGold(buyer, playerGold - itemDataFromShopArray.Price);

            // --- 👇 여기가 핵심 수정 부분입니다 ---

            // 3. 구매할 아이템과 구매자 인벤토리를 찾습니다.
            ShopItem purchasedShopItem = Runner.FindObject(itemNetworkId)?.GetComponent<ShopItem>();
            testPlayerInventory playerInventory = Runner.GetPlayerObject(buyer)?.GetComponent<testPlayerInventory>();

            // 4. 아이템과 인벤토리가 유효하고, 인벤토리에 공간이 있는지 최종 확인합니다.
            if (purchasedShopItem != null && playerInventory != null && playerInventory.CanPickupItem())
            {
                // 5. 아이템을 "판매 완료" 상태로 먼저 변경합니다. (다시 구매 못하도록)
                purchasedShopItem.MarkAsSold();

                // 6. 플레이어 인벤토리의 PickupItem 메서드를 호출하여 아이템을 손으로 옮깁니다.
                playerInventory.PickupItem(purchasedShopItem);

                Debug.Log($"Host: Player {buyer.PlayerId} purchased and picked up {itemDataFromShopArray.ItemName}.");
                RPC_SendPurchaseResult(buyer, true, itemDataFromShopArray.ItemName); // 구매 성공 알림
            }
            else
            {
                // 만약 아이템을 집을 수 없는 예외 상황이라면 골드를 되돌려줍니다.
                SetPlayerGold(buyer, playerGold); // 환불 처리
                Debug.LogWarning($"Host: Purchase approved but pickup failed for Player {buyer.PlayerId}. Gold refunded.");
                RPC_SendPurchaseResult(buyer, false, "인벤토리가 가득 찼습니다!"); // 실패 알림
            }

            // --- 👆 여기까지 수정 ---

        }
        else
        {
            Debug.Log($"Host: Player {buyer.PlayerId} tried to buy {itemDataFromShopArray.ItemName} but had insufficient gold. Gold: {playerGold}, Price: {itemDataFromShopArray.Price}");
            RPC_SendPurchaseResult(buyer, false, itemDataFromShopArray.ItemName);
        }
    }

    // --- RPC: 구매 결과 클라이언트에게 전송 (호스트 -> 특정 클라이언트) ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_SendPurchaseResult(PlayerRef targetPlayer, bool success, NetworkString<_32> itemTypeName)
    {
        if (success)
        {
            Debug.Log($"Client: 구매 성공! {itemTypeName}을(를) 획득했습니다.");
        }
        else
        {
            Debug.Log($"Client: 구매 실패! {itemTypeName}을(를) 구매할 수 없습니다 (돈 부족).");
        }
        // TODO: 구매 결과 UI 표시 로직 (HUD 등)
    }

    // --- Networked 속성 변경 감지 및 처리 ---
    // ShopItems NetworkArray의 변경을 감지하는 콜백
    // Networked 속성에 OnChanged = nameof(OnShopItemsNetworkedChanged)로 지정되어 호출됩니다.
    void OnShopItemsNetworkedChanged()
    {
        // 이 함수는 ShopItems NetworkArray에 변경이 있을 때마다 모든 클라이언트에서 실행됩니다.
        Debug.Log("ShopItems NetworkArray changed! Updating visuals.");

        for (int i = 0; i < maxShopItems; i++)
        {
            var itemData = ShopItems.Get(i); // ShopManager의 NetworkArray에 있는 데이터

            // 해당 NetworkId를 가진 실제 ShopItem 오브젝트를 찾습니다.
            ShopItem shopItemInstance = Runner.FindObject(itemData.ItemNetworkId)?.GetComponent<ShopItem>();

            if (shopItemInstance != null)
            {
                // ShopItemVisual을 통해 아이템의 비주얼 업데이트
                UpdateItemVisual(shopItemInstance, itemData);

                // ⭐️ 아이템이 더 이상 사용 가능하지 않거나 (구매/도난), 혹은 이미 들려있는 경우
                // 오브젝트 자체를 비활성화하여 숨깁니다. (Despawn은 ShopItem.OnPurchased()나 NotifyTheftAttempt에서 처리)
                if (!itemData.IsAvailable || itemData.IsPicked)
                {
                    if (shopItemInstance.gameObject.activeSelf)
                    {
                        //shopItemInstance.gameObject.SetActive(false);
                        Debug.Log($"Client: Hiding item {itemData.ItemName} at index {i} (Available: {itemData.IsAvailable}, Picked: {itemData.IsPicked}).");
                    }
                }
                else // 다시 사용 가능해지면 활성화 (예: 아이템 반납 시)
                {
                    if (!shopItemInstance.gameObject.activeSelf)
                    {
                        shopItemInstance.gameObject.SetActive(true);
                        Debug.Log($"Client: Showing item {itemData.ItemName} at index {i} (Available).");
                    }
                }
            }
            else // ShopItem 인스턴스가 없는 경우 (예: 이미 Despawn 되었을 때)
            {
                if (itemData.IsAvailable || itemData.IsPicked) // 데이터는 존재하는데 오브젝트가 사라졌다면 경고
                {
                    Debug.LogWarning($"ShopItem object for ID {itemData.ItemNetworkId} (Type: {itemData.ItemType}) is null or invalid, but itemData indicates it should be active/available. Was it prematurely despawned?");
                }
            }
        }
    }

    // --- 특정 아이템 상태 업데이트 (Host Authority) ---
    // 이 RPC는 PlayerItemPickup에서 아이템을 줍거나 놓을 때 호출됩니다.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_UpdateItemState(NetworkId itemNetworkId, bool isPicked, PlayerRef holder)
    {
        int itemIndex = -1;
        ShopItemData currentItem = default;

        for (int i = 0; i < ShopItems.Length; i++)
        {
            var tempItem = ShopItems.Get(i);
            if (tempItem.ItemNetworkId == itemNetworkId)
            {
                currentItem = tempItem;
                itemIndex = i;
                break;
            }
        }

        if (itemIndex == -1)
        {
            Debug.LogError($"Host: Item with NetworkId {itemNetworkId} not found in ShopItems NetworkArray for state update.");
            return;
        }

        // 동시성 문제 방지: 이미 다른 플레이어가 들고 있다면 무시
        if (isPicked && currentItem.IsPicked && currentItem.CurrentHolder != holder)
        {
            Debug.Log($"Host: Item {currentItem.ItemName} (ID: {itemNetworkId}) already picked by {currentItem.CurrentHolder}. Request from {holder} ignored.");
            return;
        }

        currentItem.IsPicked = isPicked;
        currentItem.CurrentHolder = holder;

        ShopItems.Set(itemIndex, currentItem); // NetworkArray 업데이트 -> 모든 클라이언트에 동기화
        Debug.Log($"Host: ShopItems Array updated: Item {currentItem.ItemName} (ID: {itemNetworkId}) state updated: IsPicked={isPicked}, Holder={holder}");

        // 추가적으로 ShopItem 자체의 Networked ItemData도 동기화
        ShopItem shopItem = Runner.FindObject(itemNetworkId)?.GetComponent<ShopItem>();
        if (shopItem != null)
        {
            // ShopItem 자체의 ItemData를 업데이트
            shopItem.ItemData = currentItem;
            Debug.Log($"Host: ShopItem instance {shopItem.name} ItemData updated to match ShopManager's state.");
        }
    }

    // --- 아이템 상호작용 락 (서버 측에서 관리) ---
    private Dictionary<NetworkId, float> itemInteractionLock = new Dictionary<NetworkId, float>();
    private const float INTERACTION_COOLDOWN = 0.5f;

    public bool CanInteractWithItem(NetworkId itemNetworkId)
    {
        if (itemInteractionLock.ContainsKey(itemNetworkId))
        {
            return Runner.SimulationTime > itemInteractionLock[itemNetworkId];
        }
        return true;
    }

    public void SetItemInteractionLock(NetworkId itemNetworkId)
    {
        itemInteractionLock[itemNetworkId] = Runner.SimulationTime + INTERACTION_COOLDOWN;
    }

    // --- RPC: 아이템 집기 요청 (클라이언트 -> 호스트) ---
    // 이 RPC는 PlayerItemPickup에서 호출되며, 실제 아이템 픽업 로직을 시작합니다.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RequestItemPickup(NetworkId itemNetworkId, PlayerRef player)
    {
        if (!CanInteractWithItem(itemNetworkId))
        {
            Debug.Log($"Host: Player {player.PlayerId} tried to pick up item {itemNetworkId} too quickly (cooldown).");
            return;
        }

        int itemIndex = -1;
        ShopItemData itemData = default; // ShopManager의 NetworkArray에 있는 데이터

        for (int i = 0; i < ShopItems.Length; i++)
        {
            var tempItem = ShopItems.Get(i);
            if (tempItem.ItemNetworkId == itemNetworkId)
            {
                itemData = tempItem;
                itemIndex = i;
                break;
            }
        }

        if (itemIndex == -1 || !itemData.IsAvailable || (itemData.IsPicked && itemData.CurrentHolder != player))
        {
            Debug.Log($"Host: Item {itemNetworkId} (Type: {itemData.ItemType}) cannot be picked up. IsAvailable: {itemData.IsAvailable}, IsPicked: {itemData.IsPicked}, Holder: {itemData.CurrentHolder}");
            return;
        }

        SetItemInteractionLock(itemNetworkId);

        NetworkObject playerObject = Runner.GetPlayerObject(player);
        if (playerObject != null)
        {
            testPlayerInventory playerInventory = playerObject.GetComponent<testPlayerInventory>();
            ShopItem actualShopItem = Runner.FindObject(itemNetworkId)?.GetComponent<ShopItem>();

            if (playerInventory != null && actualShopItem != null && playerInventory.CanPickupItem())
            {
                // ⭐️ PlayerInventory의 PickupItem을 호출하고, 여기서 ShopItem.OnPickedUp()을 호출합니다.
                playerInventory.PickupItem(actualShopItem);
                Debug.Log($"Host: Player {player.PlayerId} successfully requested pickup of {itemData.ItemName}.");
            }
            else
            {
                Debug.LogWarning($"Host: Player {player.PlayerId} cannot pick up item {itemData.ItemName}. Inventory full or ShopItem null. No state change needed.");
                // 픽업 실패 시 상태 롤백은 여기서 직접 하지 않습니다. (ShopItem.OnPickedUp에서만 ItemData.IsPicked를 변경)
            }
        }
        else
        {
            Debug.LogError($"Host: Player object for {player.PlayerId} not found for pickup request.");
        }
    }

    // --- 아이템 내려놓기 요청 (클라이언트 -> 호스트) ---
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RequestItemDrop(NetworkId itemNetworkId, PlayerRef player, Vector3 dropPosition)
    {
        // 1. 아이템을 찾고 유효성을 검사합니다.
        ShopItem itemToDrop = Runner.FindObject(itemNetworkId)?.GetComponent<ShopItem>();
        if (itemToDrop == null || !itemToDrop.ItemData.IsPicked || itemToDrop.ItemData.CurrentHolder != player)
        {
            Debug.Log($"Host: Item {itemNetworkId} cannot be dropped by Player {player.PlayerId}.");
            return;
        }

        // 2. 플레이어 인벤토리를 찾아 DropItem 메서드를 호출합니다.
        NetworkObject playerObject = Runner.GetPlayerObject(player);
        if (playerObject != null)
        {
            testPlayerInventory playerInventory = playerObject.GetComponent<testPlayerInventory>();
            if (playerInventory != null)
            {
                // 3. 실제 드랍 로직을 실행합니다.
                playerInventory.DropItem(itemToDrop, dropPosition);
            }
        }
    }


    // --- ShopItemVisual 업데이트 헬퍼 ---
    void UpdateItemVisual(ShopItem shopItemInstance, ShopItemData itemData)
    {
        ShopItemVisual visual = shopItemInstance.GetComponent<ShopItemVisual>();
        if (visual != null)
        {
            ItemStaticData staticData = GetStaticItemData(itemData.ItemType);
            if (staticData != null)
            {
                visual.SetItemSprite(staticData.itemSprite);
            }
            //visual.UpdateVisual(itemData); // ShopItemVisual이 ItemData를 받아 적절히 표시하도록
        }
    }


    // --- 상점 영역 확인 함수 ---
    public bool IsPositionInShopArea(Vector3 position)
    {
        if (_shopAreaCollider == null)
        {
            Debug.LogWarning("ShopAreaTrigger collider is not assigned or found!");
            return false;
        }
        return _shopAreaCollider.OverlapPoint(position);
    }

    // 상점 주인을 스폰하는 메서드
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

        _spawnedShopkeeper = Runner.Spawn(shopkeeperPrefab, shopkeeperSpawnPoint.position, shopkeeperSpawnPoint.rotation);
        shopkeeper = _spawnedShopkeeper.GetComponent<Shopkeeper>();
        Debug.Log($"Host: Shopkeeper spawned at {shopkeeperSpawnPoint.position}. NetworkId: {_spawnedShopkeeper.Id}");
    }

    // --- 수정된 NotifyTheftAttempt 메서드 ---
    // 플레이어가 아이템을 구매하지 않고 상점 영역을 벗어났을 때 호출됩니다.
    // (Host에서만 호출되어야 함)
    public void NotifyTheftAttempt(PlayerRef potentialAggressor, NetworkObject stolenItemObject)
    {
        if (!Object.HasStateAuthority) return; // 호스트만 처리합니다.

        Debug.Log($"Host: Theft attempt detected! Item: {stolenItemObject?.name}. Potential Aggressor: {(Runner.GetPlayerObject(potentialAggressor) ? potentialAggressor.PlayerId.ToString() : "None")}");

        // 1. 상점 주인 상태 변경
        if (shopkeeper != null)
        {
            shopkeeper.SetShopkeeperState(ShopkeeperState.Aggressive);

            if (potentialAggressor.IsNone == false && Runner.GetPlayerObject(potentialAggressor) != null)
            {
                shopkeeper.SetLastAggressor(potentialAggressor);
            }
        }

        // 2. 훔쳐진 아이템 처리
        if (stolenItemObject != null)
        {
            for (int i = 0; i < ShopItems.Length; i++)
            {
                var itemData = ShopItems.Get(i);
                if (itemData.ItemNetworkId == stolenItemObject.Id)
                {
                    itemData.IsAvailable = false; // 더 이상 판매 가능한 아이템이 아닙니다.
                    itemData.IsPicked = false;   // 들고 있는 상태였다면, 내려놓음 처리.
                    itemData.CurrentHolder = default; // 소유자 없음 (도난됨)
                    ShopItems.Set(i, itemData);  // NetworkArray 업데이트 반영
                    Debug.Log($"Host: Item {stolenItemObject.name} (ID: {stolenItemObject.Id}) marked as stolen/unavailable.");

                    // 훔쳐진 아이템을 Despawn
                    if (stolenItemObject.IsValid)
                    {
                        //Runner.Despawn(stolenItemObject);
                    }
                    break;
                }
            }
        }
    }
}