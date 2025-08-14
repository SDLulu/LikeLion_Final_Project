using Fusion;
using UnityEngine;
using TMPro;
using Fusion.Addons.Physics;



// ItemType, ShopItemData는 이전 답변에서 정의된 대로 유지됩니다.
// ShopItemData struct는 ItemName 필드가 NetworkString<NXX> 타입으로 변경되었어야 합니다.

public class ShopItem : NetworkBehaviour, IItemInteraction, IInteractable
{
    public NetworkId ItemId => Object.Id;

    [Networked]
    public ShopItemData ItemData { get; set; }

    [Header("UI Settings")]
    [SerializeField] private GameObject purchaseUIPrefab;
    //private GameObject _spawnedPurchaseUI; // 구매 UI 오브젝트

    [Networked]
    private NetworkBool _isPlayerColliding { get; set; } = false;

    [Networked] // ⭐️ 이 변수는 Networked로 선언되어야 모든 클라이언트가 알 수 있습니다.
    private PlayerRef _currentInteractingPlayer { get; set; } = PlayerRef.None; // ⭐️ 초기값 설정

    public bool IsHeld => throw new System.NotImplementedException();

    private NetworkRigidbody2D _netRigidbody;
    private Collider2D _collider;
    private ShopManager _shopManager;

    private ShopItemVisual _shopItemVisual;

    public override void Spawned()
    {
        _netRigidbody = GetComponent<NetworkRigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _shopManager = FindFirstObjectByType<ShopManager>();


        // ⭐️ ShopItemVisual 컴포넌트 참조 가져오기 (자식 오브젝트에도 있을 수 있으므로 GetComponentsInChildren 사용)
        _shopItemVisual = GetComponentInChildren<ShopItemVisual>();
        if (_shopItemVisual == null)
        {
            Debug.LogError($"ShopItem {name}: ShopItemVisual component not found on this object or its children!", this);
        }

    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        //if (_spawnedPurchaseUI != null)
        //{
        //    Destroy(_spawnedPurchaseUI);
        //}
    }
    public void OnInteract(PlayerInteraction interactor)
    {
        if (ItemData.IsAvailable && !ItemData.IsPicked)
        {
            var shopManager = FindFirstObjectByType<ShopManager>();
            if (shopManager != null)
            {
                shopManager.Rpc_RequestPurchase(interactor.Object.InputAuthority, Object.Id);
            }
        }
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
        if (Object == null)
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
        if (purchaseUIPrefab != null)
        {

            purchaseUIPrefab.SetActive(true);

        }
    }

    private void HidePurchaseUI()
    {
        purchaseUIPrefab.SetActive(false);

    }

    // OnPickedUp, OnDropped, OnPurchased, OnUsePress, OnUseHold, OnUseRelease, UpdatePhysicsState는 기존과 동일
    public void OnPickedUp()
    {
        if (!Object.HasStateAuthority) return;
        var data = ItemData;
        data.IsPicked = true;
        ItemData = data;
        _shopItemVisual.enabled = false;
    }

    public void OnReleased()
    {
        if (!Object.HasStateAuthority) return;
        var data = ItemData;
        data.IsPicked = false;
        ItemData = data;
        if (data.IsAvailable)
        {
            _shopItemVisual.enabled = true;
        }
    }

    public void MarkAsSold()
    {
        if (!Object.HasStateAuthority) return;

        // ItemData는 struct이므로, 복사본을 만들어 수정한 뒤 다시 할당해야 합니다.
        var data = ItemData;
        data.IsAvailable = false; // 판매 불가능 상태로 변경
        ItemData = data;
        _shopItemVisual.enabled = false;

    }

    // IUsableItem 구현
    // ShopItem.cs의 OnUsePress 메서드를 아래 코드로 교체합니다.
    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (!Object.HasStateAuthority) return;

        // 도둑질 확인
        if (ItemData.IsAvailable)
        {
            Debug.LogWarning($"Host: 도둑질 감지! 미구매 아이템 '{ItemData.ItemName}' 사용 시도.");

            var shopManager = FindFirstObjectByType<ShopManager>();
            if (shopManager != null)
            {
                // ⭐️ 중요: NetworkObject 대신 ItemData 구조체를 복사해서 새 RPC로 신고합니다.
                shopManager.Rpc_ReportTheftByData(this.ItemData);
            }

            // 여기서 return 할 필요는 없습니다. 어차피 아이템 사용 시스템이
            // 이 메서드 호출 후에 아이템을 Despawn 시킬 것이기 때문입니다.
        }
        else
        {
            // 정상적인 아이템 사용 로직
            Debug.Log($"Host: 구매한 아이템 '{ItemData.ItemName}'을 사용했습니다.");
            // 여기에 실제 아이템 효과 구현...
        }
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

    public void ApplyKnockback(Vector2 force, float duration = 0)
    {

    }
}