using Fusion;
using UnityEngine;
using TMPro;
using Fusion.Addons.Physics;



// ItemType, ShopItemData는 이전 답변에서 정의된 대로 유지됩니다.
// ShopItemData struct는 ItemName 필드가 NetworkString<NXX> 타입으로 변경되었어야 합니다.

public class ShopItem : NetworkBehaviour, IUsableItem
{
    public NetworkId ItemId => Object.Id;

    [Networked]
    public ShopItemData ItemData { get; set; }
    [Networked]
    private NetworkButtons _previousInteractingPlayerButtons { get; set; }

    [Header("UI Settings")]
    [SerializeField] private GameObject purchaseUIPrefab;
    //private GameObject _spawnedPurchaseUI; // 구매 UI 오브젝트

    [Networked]
    private NetworkBool _isPlayerColliding { get; set; } = false;

    [Networked] // ⭐️ 이 변수는 Networked로 선언되어야 모든 클라이언트가 알 수 있습니다.
    private PlayerRef _currentInteractingPlayer { get; set; } = PlayerRef.None; // ⭐️ 초기값 설정


    private NetworkRigidbody2D _netRigidbody;
    private Collider2D _collider;
    private ShopManager _shopManager;

    private ShopItemVisual _shopItemVisual;

    public override void Spawned()
    {
        _netRigidbody = GetComponent<NetworkRigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _shopManager = FindFirstObjectByType<ShopManager>();
        _previousInteractingPlayerButtons = default;

        // ⭐️ ShopItemVisual 컴포넌트 참조 가져오기 (자식 오브젝트에도 있을 수 있으므로 GetComponentsInChildren 사용)
        _shopItemVisual = GetComponentInChildren<ShopItemVisual>();
        if (_shopItemVisual == null)
        {
            Debug.LogError($"ShopItem {name}: ShopItemVisual component not found on this object or its children!", this);
        }

        //if (purchaseUIPrefab != null)
        //{
        //    _spawnedPurchaseUI = Instantiate(purchaseUIPrefab);
        //    _spawnedPurchaseUI.SetActive(false);

        //    // ⭐️ 중요: UI를 적절한 UI Canvas의 자식으로 설정해야 화면에 보입니다.
        //    // 씬에 "MainCanvas" 같은 이름의 Canvas가 있다고 가정합니다.
        //    Canvas mainCanvas = FindObjectOfType<Canvas>();
        //    if (mainCanvas != null)
        //    {
        //        _spawnedPurchaseUI.transform.SetParent(mainCanvas.transform, false); // false: 월드 좌표 유지 안함 (UI에 적합)
        //        Debug.Log($"ShopItem {name}: Purchase UI instantiated and parented to {mainCanvas.name}.");
        //    }
        //    else
        //    {
        //        Debug.LogWarning($"ShopItem {name}: No Canvas found in scene. Purchase UI might not be visible.");
        //    }
        //}
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        //if (_spawnedPurchaseUI != null)
        //{
        //    Destroy(_spawnedPurchaseUI);
        //}
    }

    public void InitializeItemData(ItemType type, int price, string name)
    {
        if (Object.HasStateAuthority)
        {
            ItemData = new ShopItemData
            {
                ItemNetworkId = Object.Id,
                ItemType = type,
                Price = price,
                IsAvailable = true,
                IsPicked = false,
                CurrentHolder = default,
                OriginalPosition = transform.position,
                ItemName = name // NetworkString<N> 타입으로 자동 변환될 것입니다.
            };
            Debug.Log($"Host: ShopItem {name} data initialized with price {price}.");

            UpdatePhysicsState(false, true); // 처음에는 물리 비활성, 트리거 활성 (상점에 놓여있음)

            // ⭐️ 초기화 시에도 비주얼 업데이트를 호출하여 PriceText가 바로 보이도록 합니다.
            if (_shopItemVisual != null)
            {
                _shopItemVisual.UpdatePriceText(ItemData.Price);
                _shopItemVisual.UpdateItemColorAndPriceTag(ItemData.IsAvailable, ItemData.IsPicked);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {

        if (Object.HasStateAuthority)
        {
            // FixedUpdateNetwork에서 _currentInteractingPlayer를 사용하는 로직은 기존과 동일합니다.
            if (!_currentInteractingPlayer.IsNone && Runner.TryGetInputForPlayer(_currentInteractingPlayer, out SpelunkyPlayerInputData input))
            {
                const SpelunkyInputButtons PICKUP_BUTTON_MASK = SpelunkyInputButtons.pick;
                const SpelunkyInputButtons BUY_BUTTON_MASK = SpelunkyInputButtons.buy;

                if (input.NetworkButtons.IsSet(PICKUP_BUTTON_MASK) && !_previousInteractingPlayerButtons.IsSet(PICKUP_BUTTON_MASK))
                {
                    Debug.Log($"Host: Player {_currentInteractingPlayer.PlayerId} pressed PICK for purchase on {ItemData.ItemName}.");
                    if (_shopManager != null)
                    {
                        _shopManager.Rpc_RequestItemPickup(Object.Id, _currentInteractingPlayer);
                    }
                }
                if (input.NetworkButtons.IsSet(BUY_BUTTON_MASK) && !_previousInteractingPlayerButtons.IsSet(BUY_BUTTON_MASK))
                {
                    Debug.Log($"Host: Player {_currentInteractingPlayer.PlayerId} pressed BUY for purchase on {ItemData.ItemName}.");
                    if (_shopManager != null)
                    {
                        _shopManager.Rpc_RequestPurchase(_currentInteractingPlayer, Object.Id);
                    }
                }

                _previousInteractingPlayerButtons = input.NetworkButtons;

            }
            else if (_currentInteractingPlayer.IsNone)
            {
                _previousInteractingPlayerButtons = default;
            }
        }
    }

    public override void Render()
    {
        base.Render();

        // ⭐️ ShopItemVisual 업데이트 로직 (모든 클라이언트에서 실행)
        if (_shopItemVisual != null)
        {
            _shopItemVisual.UpdatePriceText(ItemData.Price); // 가격 업데이트
            _shopItemVisual.UpdateItemColorAndPriceTag(ItemData.IsAvailable, ItemData.IsPicked); // 색상 및 가격표 활성화/비활성화
        }
        

        // ⭐️ 구매 UI 표시 로직 (로컬 플레이어에게만)
        // 로컬 플레이어가 현재 이 아이템과 충돌 중인지 확인합니다.
        // _currentInteractingPlayer는 호스트에 의해 Networked 변수로 동기화되므로 모든 클라이언트에서 동일한 값을 가집니다.
        if (purchaseUIPrefab != null)
        {
            if (_currentInteractingPlayer == Runner.LocalPlayer && _isPlayerColliding && ItemData.IsAvailable && !ItemData.IsPicked)
            {
                ShowPurchaseUI();
            }
            else
            {
                HidePurchaseUI();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!Object.HasStateAuthority) return;

        if (other.CompareTag("Player"))
        {
            SpelunkyPlayerController spelunkyPlayerController = other.GetComponentInParent<SpelunkyPlayerController>();
            if (spelunkyPlayerController != null)
            {
                _isPlayerColliding = true;
                _currentInteractingPlayer = spelunkyPlayerController.Object.InputAuthority;
                Debug.Log($"Host: Player {spelunkyPlayerController.Object.InputAuthority.PlayerId} entered {name} trigger. _isPlayerColliding = true. CurrentInteractingPlayer: {_currentInteractingPlayer.PlayerId}");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(Object == null)
        {
            Debug.LogWarning($"ShopItem {name}: NetworkObject is null in OnTriggerExit2D. Skipping processing.");
            return; // Object가 null이면 더 이상 진행하지 않습니다.
        }

        if (!Object.HasStateAuthority) return;

        if (other.CompareTag("Player"))
        {
            SpelunkyPlayerController spelunkyPlayerController = other.GetComponentInParent<SpelunkyPlayerController>();
            // ⭐️ 중요한 부분: 나가는 플레이어가 현재 상호작용 중이던 플레이어와 일치하는지 확인
            if (spelunkyPlayerController != null && _currentInteractingPlayer == spelunkyPlayerController.Object.InputAuthority)
            {
                _isPlayerColliding = false;
                _currentInteractingPlayer = PlayerRef.None; // 참조 해제
                Debug.Log($"Host: Player {spelunkyPlayerController.Object.InputAuthority.PlayerId} exited {name} trigger. _isPlayerColliding = false. CurrentInteractingPlayer reset.");
            }
        }
    }

    private void ShowPurchaseUI()
    {
        if (purchaseUIPrefab != null )
        {
            //_spawnedPurchaseUI.SetActive(true);
            purchaseUIPrefab.SetActive(true);
            //TextMeshProUGUI uiText = _spawnedPurchaseUI.GetComponentInChildren<TextMeshProUGUI>();
            //if (uiText != null)
            //{
            //    uiText.text = $"[좌클릭] 구매: {ItemData.ItemName}\n({ItemData.Price}G)";
            //    // ⭐️ UI 위치를 아이템 오브젝트의 스크린 좌표로 업데이트 (Render에서 매 프레임 업데이트)
            //    Vector2 screenPoint = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1f); // 아이템 위 1유닛
            //    _spawnedPurchaseUI.transform.position = screenPoint;
            //}
        }
    }

    private void HidePurchaseUI()
    {
        purchaseUIPrefab.SetActive(false);
        //if (_spawnedPurchaseUI != null && _spawnedPurchaseUI.activeSelf)
        //{
        //    _spawnedPurchaseUI.SetActive(false);
        //}
    }

    // OnPickedUp, OnDropped, OnPurchased, OnUsePress, OnUseHold, OnUseRelease, UpdatePhysicsState는 기존과 동일
    public void OnPickedUp(PlayerRef picker)
    {
        if (!Object.HasStateAuthority) return;

        ItemData = new ShopItemData
        {
            ItemNetworkId = ItemData.ItemNetworkId,
            ItemType = ItemData.ItemType,
            Price = ItemData.Price,
            IsAvailable = ItemData.IsAvailable,
            IsPicked = true,
            CurrentHolder = picker,
            OriginalPosition = ItemData.OriginalPosition,
            ItemName = ItemData.ItemName
        };
        

        UpdatePhysicsState(false, true); // 시뮬레이션 비활성, 트리거 활성

        _isPlayerColliding = false;
        _currentInteractingPlayer = PlayerRef.None;

        Debug.Log($"Host: Item {ItemData.ItemName} picked up by Player {picker.PlayerId}. ItemData.IsPicked: {ItemData.IsPicked}");
    }

    public void OnDropped(PlayerRef dropper)
    {
        if (!Object.HasStateAuthority) return;

        ItemData = new ShopItemData
        {
            ItemNetworkId = ItemData.ItemNetworkId,
            ItemType = ItemData.ItemType,
            Price = ItemData.Price,
            IsAvailable = ItemData.IsAvailable,
            IsPicked = false,
            CurrentHolder = default,
            OriginalPosition = ItemData.OriginalPosition,
            ItemName = ItemData.ItemName
            
        };

        UpdatePhysicsState(true, false); // 시뮬레이션 활성, 트리거 비활성

        Debug.Log($"Host: Item {ItemData.ItemName} dropped by Player {dropper.PlayerId}. ItemData.IsPicked: {ItemData.IsPicked}");
    }

    public void MarkAsSold()
    {
        if (!Object.HasStateAuthority) return;

        // ItemData는 struct이므로, 복사본을 만들어 수정한 뒤 다시 할당해야 합니다.
        var data = ItemData;
        data.IsAvailable = false; // 판매 불가능 상태로 변경
        ItemData = data;
    }

    // IUsableItem 구현
    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        Debug.Log($"ShopItem {ItemData.ItemName} used (Press). Implement actual item effect here.");
    }
    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
    }
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        Debug.Log($"ShopItem {ItemData.ItemName} used (Release).");
    }

    private void UpdatePhysicsState(bool simulatePhysics, bool isTriggerCollider)
    {
        if (_netRigidbody != null)
        {
            _netRigidbody.Rigidbody.simulated = simulatePhysics;
            _netRigidbody.Rigidbody.isKinematic = !simulatePhysics;
            _netRigidbody.Rigidbody.linearVelocity = Vector2.zero;
            _netRigidbody.Rigidbody.angularVelocity = 0f;
        }

        if (_collider != null)
        {
            _collider.isTrigger = isTriggerCollider;
        }
    }
}