using Fusion;
using System.Collections.Generic;
using TMPro; // TMP_Text 사용
using UnityEngine;

public class ShopManager : NetworkBehaviour
{

    [Header("Shop References")]
    [SerializeField] private NetworkPrefabRef shopkeeperPrefab; // 유니티 에디터에서 상점 주인 프리팹을 할당합니다.
    [SerializeField] private Transform shopkeeperSpawnPoint; // 유니티 에디터에서 상점 주인 스폰 포인트를 할당합니다.

    // 스폰된 상점 주인 NetworkObject를 저장할 변수 (선택 사항)
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

    // 스폰된 ShopItem 오브젝트들의 참조를 로컬에서 관리
    private Dictionary<int, ShopItem> _spawnedShopItems = new Dictionary<int, ShopItem>();

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
        // 샵 영역 콜라이더 참조
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
        // 또는 OnShopItemsNetworkedChanged를 Spawned 마지막에 한 번 명시적으로 호출하여 초기 상태를 렌더링할 수도 있습니다.
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
            foreach (var itemKvp in _spawnedShopItems)
            {
                if (itemKvp.Value != null && itemKvp.Value.Object.IsValid)
                {
                    Runner.Despawn(itemKvp.Value.Object);
                }
            }
        }
        _spawnedShopItems.Clear();
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
                        // 초기화 시 ShopItem에 ShopManager와 인덱스 참조 전달
                        shopItemComponent.Initialize(this, i, randomStaticItem.itemType, randomStaticItem.basePrice);
                    }
                }
            ).GetComponent<ShopItem>();

            if (spawnedShopItem == null)
            {
                Debug.LogError($"Failed to spawn ShopItem from prefab: {randomStaticItem.itemPrefab.name}");
                continue;
            }

            // NetworkArray에 데이터 저장
            var itemData = new ShopItemData
            {
                ItemType = randomStaticItem.itemType,
                Price = CalculatePrice(randomStaticItem.itemType), // 가격 계산 로직은 필요에 따라
                IsAvailable = true,
                IsPicked = false,
                CurrentHolder = default, // NetworkId.None 대신 default
                OriginalPosition = itemSpawnPoints[i].position,
                ItemNetworkId = spawnedShopItem.Object.Id // 스폰된 NetworkObject의 ID 저장
            };

            ShopItems.Set(i, itemData);
            _spawnedShopItems[i] = spawnedShopItem; // 로컬 맵에 참조 저장
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
            PlayerInventory inventory = playerObject.GetComponent<PlayerInventory>();
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
            PlayerInventory inventory = playerObject.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.Gold = newGold;
            }
        }
    }

    // --- RPC: 아이템 구매 요청 (클라이언트 -> 호스트) ---
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RequestPurchase(PlayerRef buyer, int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= maxShopItems)
        {
            Debug.LogError($"Invalid itemIndex: {itemIndex}");
            return;
        }

        var item = ShopItems.Get(itemIndex); // NetworkArray에서 값 가져올 때 Get() 사용

        if (!item.IsAvailable || item.IsPicked)
        {
            Debug.Log($"Item {item.ItemType} at index {itemIndex} is not available or is picked.");
            return;
        }

        int playerGold = GetPlayerGold(buyer);

        if (playerGold >= item.Price)
        {
            SetPlayerGold(buyer, playerGold - item.Price);

            item.IsAvailable = false;
            item.CurrentHolder = buyer; // 구매자 설정 (굳이 구매자 설정 필요 없을 수 있음, 판매되었으니)

            ShopItems.Set(itemIndex, item); // NetworkArray 업데이트

            // 실제 아이템 오브젝트 제거
            ShopItem purchasedItemObject = GetShopItemObject(itemIndex);
            if (purchasedItemObject != null && purchasedItemObject.Object.IsValid)
            {
                Runner.Despawn(purchasedItemObject.Object);
                _spawnedShopItems.Remove(itemIndex);
            }
            Debug.Log($"Host: Player {buyer.PlayerId} successfully purchased {item.ItemType} for {item.Price} gold.");

            RPC_SendPurchaseResult(buyer, true, item.ItemType.ToString());
        }
        else
        {
            Debug.Log($"Host: Player {buyer.PlayerId} tried to buy {item.ItemType} but had insufficient gold. Gold: {playerGold}, Price: {item.Price}");
            RPC_SendPurchaseResult(buyer, false, item.ItemType.ToString());
        }
    }

    // --- RPC: 구매 결과 클라이언트에게 전송 (호스트 -> 특정 클라이언트) ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_SendPurchaseResult(PlayerRef targetPlayer, bool success, string itemTypeName)
    {
        if (success)
        {
            Debug.Log($"Client: 구매 성공! {itemTypeName}을(를) 획득했습니다.");
        }
        else
        {
            Debug.Log($"Client: 구매 실패! {itemTypeName}을(를) 구매할 수 없습니다 (돈 부족).");
        }
    }

    // --- Networked 속성 변경 감지 및 처리 ---
    // ShopItems NetworkArray의 변경을 감지하는 콜백
    // Networked 속성에 OnChanged = nameof(OnShopItemsNetworkedChanged)로 지정되어 호출됩니다.
    void OnShopItemsNetworkedChanged()
    {
        // 이 함수는 ShopItems NetworkArray에 변경이 있을 때마다 모든 클라이언트에서 실행됩니다.
        Debug.Log("ShopItems NetworkArray changed! Updating visuals.");

        // 모든 아이템의 상태를 다시 확인하고 비주얼을 업데이트
        for (int i = 0; i < maxShopItems; i++)
        {
            var itemData = ShopItems.Get(i); // NetworkArray에서 값 가져올 때 Get() 사용
            ShopItem shopItemInstance = GetShopItemObject(i);

            if (shopItemInstance != null)
            {
                // ShopItemVisual을 통해 아이템의 비주얼 업데이트
                UpdateItemVisual(shopItemInstance, itemData);

                // 아이템이 판매되었으면 비활성화 또는 Despawn 처리
                if (!itemData.IsAvailable)
                {
                    if (Object.HasStateAuthority) // Host만 Despawn 호출
                    {
                        if (shopItemInstance.Object.IsValid) // 아직 유효한지 다시 확인
                        {
                            Runner.Despawn(shopItemInstance.Object);
                            _spawnedShopItems.Remove(i);
                            Debug.Log($"Host: Despawning sold item {itemData.ItemType} at index {i}");
                        }
                    }
                    else // 클라이언트는 로컬 ShopItem 객체를 숨김 (Host가 Despawn할 때까지)
                    {
                        if (shopItemInstance.gameObject.activeSelf)
                        {
                            shopItemInstance.gameObject.SetActive(false);
                            Debug.Log($"Client: Hiding sold item {itemData.ItemType} at index {i}");
                        }
                    }
                }
            }
            else // ShopItem 인스턴스가 없는 경우 (예: 이미 Despawn 되었는데 _spawnedShopItems에 남아있을 때)
            {
                if (itemData.IsAvailable) // 아직 판매되지 않았는데 오브젝트가 없는 경우 (예외 상황)
                {
                    Debug.LogWarning($"ShopItem object at index {i} is null or invalid, but itemData.IsAvailable is true. Re-spawning or error?");
                    // 여기서 다시 스폰하는 로직을 추가할 수 있으나, 일반적으로는 Host가 관리합니다.
                }
            }
        }
    }

    // --- 특정 아이템 상태 업데이트 (Host Authority) ---
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_UpdateItemState(int itemIndex, bool isPicked, PlayerRef holder)
    {
        if (itemIndex < 0 || itemIndex >= maxShopItems) return;

        var currentItem = ShopItems.Get(itemIndex);

        // 동시성 문제 방지: 이미 다른 플레이어가 들고 있다면 무시
        if (isPicked && currentItem.IsPicked && currentItem.CurrentHolder != holder)
        {
            Debug.Log($"Host: Item {itemIndex} already picked by {currentItem.CurrentHolder}. Request from {holder} ignored.");
            return;
        }

        currentItem.IsPicked = isPicked;
        currentItem.CurrentHolder = holder;

        ShopItems.Set(itemIndex, currentItem); // NetworkArray 업데이트 -> 모든 클라이언트에 동기화
        Debug.Log($"Host: Item {itemIndex} state updated: IsPicked={isPicked}, Holder={holder}");
    }

    // --- 아이템 상호작용 락 (서버 측에서 관리) ---
    private Dictionary<int, float> itemInteractionLock = new Dictionary<int, float>();
    private const float INTERACTION_COOLDOWN = 0.5f;

    public bool CanInteractWithItem(int itemIndex)
    {
        if (itemInteractionLock.ContainsKey(itemIndex))
        {
            return Runner.SimulationTime > itemInteractionLock[itemIndex];
        }
        return true;
    }

    public void SetItemInteractionLock(int itemIndex)
    {
        itemInteractionLock[itemIndex] = Runner.SimulationTime + INTERACTION_COOLDOWN;
    }

    // --- RPC: 아이템 집기 요청 (클라이언트 -> 호스트) ---
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_RequestItemPickup(int itemIndex, PlayerRef player)
    {
        if (!CanInteractWithItem(itemIndex))
        {
            Debug.Log($"Host: Player {player.PlayerId} tried to pick up item {itemIndex} too quickly (cooldown).");
            return;
        }

        var itemData = ShopItems.Get(itemIndex);

        if (!itemData.IsAvailable || (itemData.IsPicked && itemData.CurrentHolder != player))
        {
            Debug.Log($"Host: Item {itemIndex} (Type: {itemData.ItemType}) cannot be picked up. IsAvailable: {itemData.IsAvailable}, IsPicked: {itemData.IsPicked}, Holder: {itemData.CurrentHolder}");
            return;
        }

        SetItemInteractionLock(itemIndex);

        Rpc_UpdateItemState(itemIndex, true, player);

        NetworkObject playerObject = Runner.GetPlayerObject(player);
        if (playerObject != null)
        {
            PlayerInventory playerInventory = playerObject.GetComponent<PlayerInventory>();
            ShopItem actualShopItem = GetShopItemObject(itemIndex);

            if (playerInventory != null && actualShopItem != null && playerInventory.CanPickupItem())
            {
                playerInventory.PickupItem(actualShopItem);
            }
            else
            {
                Debug.LogWarning($"Host: Player {player.PlayerId} cannot pick up item {itemIndex}. Inventory full or ShopItem null. Rolling back state.");
                // 픽업 실패 시 상태 롤백
                Rpc_UpdateItemState(itemIndex, false, default); // default 대신 PlayerRef.None 써도 되지만, default가 더 일반적
            }
        }
        else
        {
            Debug.LogError($"Host: Player object for {player.PlayerId} not found.");
            Rpc_UpdateItemState(itemIndex, false, default); // Player Object 없으면 상태 롤백
        }
    }

    // --- 스폰된 ShopItem 오브젝트 가져오기 (로컬 맵 사용) ---
    public ShopItem GetShopItemObject(int index)
    {
        if (_spawnedShopItems.TryGetValue(index, out ShopItem item))
        {
            return item;
        }
        return null;
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
            visual.UpdateVisual(itemData);
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

        // Runner.Spawn()을 사용하여 상점 주인을 네트워크상에 스폰합니다.
        // 스폰된 오브젝트는 자동으로 State Authority를 가집니다.
        _spawnedShopkeeper = Runner.Spawn(shopkeeperPrefab, shopkeeperSpawnPoint.position, shopkeeperSpawnPoint.rotation);
        shopkeeper = _spawnedShopkeeper.GetComponent<Shopkeeper>();
        Debug.Log($"Host: Shopkeeper spawned at {shopkeeperSpawnPoint.position}. NetworkId: {_spawnedShopkeeper.Id}");

        // 스폰된 상점 주인에게 ShopManager 자신을 알려줄 수도 있습니다 (선택 사항).
        // Shopkeeper shopkeeperComponent = _spawnedShopkeeper.GetComponent<Shopkeeper>();
        // if (shopkeeperComponent != null)
        // {
        //     shopkeeperComponent.SetShopManager(this); // 만약 Shopkeeper에 SetShopManager 메서드가 있다면
        // }
    }

    // --- 수정된 NotifyTheftAttempt 메서드 ---
    // 플레이어가 아이템을 구매하지 않고 상점 영역을 벗어났을 때 호출됩니다.
    // (Host에서만 호출되어야 함)
    // 이제 `PlayerRef`는 아이템을 "직접 들고 나간" 특정 플레이어를 지칭하지 않고,
    // 단순한 도둑질 상황에서는 PlayerRef.None으로 넘어올 수 있습니다.
    public void NotifyTheftAttempt(PlayerRef potentialAggressor, NetworkObject stolenItemObject)
    {
        if (!Object.HasStateAuthority) return; // 호스트만 처리합니다.

        Debug.Log($"Host: Theft attempt detected! Item: {stolenItemObject?.name}. Potential Aggressor: {(Runner.GetPlayerObject(potentialAggressor) ? potentialAggressor.PlayerId.ToString() : "None")}");

        // 1. 상점 주인 상태 변경
        if (shopkeeper != null)
        {
            shopkeeper.SetShopkeeperState(ShopkeeperState.Aggressive);

            // 도둑질을 시도한 특정 플레이어가 있다면 그 플레이어를 _lastAggressor로 설정
            // 그렇지 않다면 (아이템이 굴러나간 경우 등) _lastAggressor는 PlayerRef.None 상태를 유지하며
            // 상점 주인은 FixedUpdateNetwork에서 가장 가까운 플레이어를 찾아 공격할 것입니다.
            if (potentialAggressor.IsNone == false && Runner.GetPlayerObject(potentialAggressor) != null)
            {
                shopkeeper.SetLastAggressor(potentialAggressor);
            }
            // else { _spawnedShopkeeper._lastAggressor는 PlayerRef.None으로 유지 }
        }

        // 2. 훔쳐진 아이템 처리
        if (stolenItemObject != null)
        {
            // ShopItems NetworkArray를 순회하며 훔쳐진 아이템의 상태를 업데이트합니다.
            // 이 아이템은 이제 "구매 불가" 상태로 변경됩니다.
            for (int i = 0; i < ShopItems.Length; i++)
            {
                var itemData = ShopItems.Get(i);
                if (itemData.ItemNetworkId == stolenItemObject.Id)
                {
                    itemData.IsAvailable = false; // 더 이상 판매 가능한 아이템이 아닙니다.
                    itemData.IsPicked = false;    // 혹시 들고 있는 상태였다면, 내려놓음 처리.
                    ShopItems.Set(i, itemData);   // NetworkArray 업데이트 반영
                    Debug.Log($"Host: Item {stolenItemObject.name} (ID: {stolenItemObject.Id}) marked as stolen/unavailable.");

                    // 아이템 오브젝트 자체의 비주얼 변경 또는 파괴 등을 할 수 있습니다.
                    // stolenItemObject.gameObject.SetActive(false); // 예시: 아이템 숨기기
                    break;
                }
            }
        }
    }
}