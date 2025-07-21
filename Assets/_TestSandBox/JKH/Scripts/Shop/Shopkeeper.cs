using Fusion;
using UnityEngine;
using TMPro; // TextMeshProUGUI를 위해 필요

// 상점 주인 상태를 정의하는 Enum
public enum ShopkeeperState
{
    Peaceful,   // 평화로운 상태 (기본)
    Warning,    // 경고 상태 (도둑질 시도 감지)
    Aggressive  // 공격 상태 (공격받거나 아이템 훔쳤을 때)
}

public class Shopkeeper : NetworkBehaviour
{
    // --- 유니티 에디터 설정 변수 ---
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D detectionCollider;
    [SerializeField] private GameObject speechBubble;
    [SerializeField] private TextMeshProUGUI speechText;
    [SerializeField] private GameObject alertIcon;
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private Transform weaponSpawnPoint;

    [Header("Shopkeeper Settings")]
    [SerializeField] private float warningDuration = 3f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float detectionRange = 3f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask itemLayer;

    [Header("Dialogues")]
    [SerializeField] private string[] peacefulDialogues;
    [SerializeField] private string[] warningDialogues;
    [SerializeField] private string[] aggressiveDialogues;

    // --- 추가될 부분 ---
    [Networked] private PlayerRef _lastAggressor { get; set; } // 누가 마지막으로 공격/도둑질했는지 기록


    // --- 네트워크 상태 변수 ---
    // Fusion 2.0.4에서는 [Networked] 어트리뷰트 내에 OnChanged 파라미터를 사용합니다.
    // OnChangedRender는 이 버전에서 지원하지 않을 수 있습니다.
    [Networked, OnChangedRender(nameof(OnShopkeeperStateChanged))]
    public ShopkeeperState CurrentState { get; set; } = ShopkeeperState.Peaceful;

    [Networked] private TickTimer StateTimer { get; set; }
    [Networked] private TickTimer AttackTimer { get; set; }

    // --- 내부 변수 ---
    private ShopManager _shopManager;
    //private PlayerRef _lastAggressor;

    public override void Spawned()
    {
        _shopManager = FindFirstObjectByType<ShopManager>();
        if (_shopManager == null)
        {
            Debug.LogError("ShopManager not found in scene!");
        }

        // 스폰 시 초기 비주얼 설정을 위해 현재 상태에 맞춰 한 번 호출합니다.
        // OnShopkeeperStateChanged는 매개변수를 받지 않으므로,
        // 현재 CurrentState 값을 직접 사용하도록 UpdateVisualsForState 함수를 호출합니다.
        UpdateVisualsForState(CurrentState);


        if (detectionCollider == null)
        {
            Debug.LogError("Detection Collider is not assigned!");
        }
        else if (!detectionCollider.isTrigger)
        {
            Debug.LogWarning("Detection Collider should be set as Is Trigger.");
        }

        UpdateSpeechBubble(false);
        if (alertIcon != null) alertIcon.SetActive(false);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        switch (CurrentState)
        {
            case ShopkeeperState.Peaceful:
                CheckForWarningCondition();
                break;
            case ShopkeeperState.Warning:
                if (StateTimer.Expired(Runner))
                {
                    SetShopkeeperState(ShopkeeperState.Peaceful);
                }
                break;
            case ShopkeeperState.Aggressive:
                if (AttackTimer.Expired(Runner))
                {
                    PerformAttack();
                    AttackTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);
                }
                break;
        }
    }

    // --- 네트워크 콜백 (상점 주인 상태 변경 시 모든 클라이언트에서 호출) ---
    // Fusion 2.0.4에서는 매개변수를 받지 않는 void 메서드 형태입니다.
    private void OnShopkeeperStateChanged() // <--- 매개변수가 제거되었습니다.
    {
        Debug.Log($"Shopkeeper state changed to: {CurrentState}");
        // OnShopkeeperStateChanged는 CurrentState의 변경을 감지하므로,
        // 현재 CurrentState 값을 직접 사용합니다.
        UpdateVisualsForState(CurrentState);
    }

    // --- 비주얼 업데이트를 전담하는 헬퍼 함수 ---
    private void UpdateVisualsForState(ShopkeeperState state)
    {
        switch (state)
        {
            case ShopkeeperState.Peaceful:
                UpdateSpeechBubble(false);
                if (alertIcon != null) alertIcon.SetActive(false);
                // 기타 평화로운 상태의 비주얼/애니메이션
                break;
            case ShopkeeperState.Warning:
                ShowDialogue(warningDialogues);
                if (alertIcon != null) alertIcon.SetActive(true);
                // 기타 경고 상태의 비주얼/애니메이션
                break;
            case ShopkeeperState.Aggressive:
                ShowDialogue(aggressiveDialogues);
                if (alertIcon != null) alertIcon.SetActive(true);
                // 기타 공격 상태의 비주얼/애니메이션
                break;
        }
    }

    // --- 플레이어 감지 및 경고 조건 체크 (Host에서 FixedUpdateNetwork에서 호출) ---
    private void CheckForWarningCondition()
    {
        Collider2D[] detectedPlayers = Physics2D.OverlapCircleAll(transform.position, detectionRange, playerLayer);

        bool isPlayerHoldingStolenItem = false;
        foreach (Collider2D playerCollider in detectedPlayers)
        {
            PlayerController playerController = playerCollider.GetComponentInParent<PlayerController>();
            if (playerController != null && playerController.Object.IsValid)
            {
                PlayerInventory playerInventory = playerController.GetComponent<PlayerInventory>();
                if (playerInventory != null && playerInventory.HeldItemNetworkId != default)
                {
                    NetworkObject heldObject = Runner.FindObject(playerInventory.HeldItemNetworkId);
                    if (heldObject != null)
                    {
                        ShopItem heldShopItem = heldObject.GetComponent<ShopItem>();
                        if (heldShopItem != null)
                        {
                            for (int i = 0; i < _shopManager.ShopItems.Length; i++)
                            {
                                var shopItemData = _shopManager.ShopItems.Get(i);
                                if (shopItemData.ItemNetworkId == heldShopItem.Object.Id && shopItemData.IsAvailable && shopItemData.IsPicked)
                                {
                                    if (!_shopManager.IsPositionInShopArea(heldShopItem.transform.position))
                                    {
                                        isPlayerHoldingStolenItem = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (isPlayerHoldingStolenItem) break;
        }

        if (isPlayerHoldingStolenItem && CurrentState == ShopkeeperState.Peaceful)
        {
            SetShopkeeperState(ShopkeeperState.Warning);
            // detectedPlayers 배열이 비어있지 않다고 가정하고 첫 번째 플레이어를 기록
            if (detectedPlayers.Length > 0)
            {
                PlayerController detectedPlayerController = detectedPlayers[0].GetComponentInParent<PlayerController>();
                if (detectedPlayerController != null && detectedPlayerController.Object.IsValid)
                {
                    _lastAggressor = detectedPlayerController.Object.InputAuthority;
                }
            }
        }
    }

    // --- 공격 실행 (Host에서만 호출) ---
    private void PerformAttack()
    {
        PlayerController targetPlayer = FindClosestPlayer();
        if (targetPlayer != null)
        {
            Debug.Log($"Host: Shopkeeper attacking Player {targetPlayer.Object.InputAuthority.PlayerId}");
            RPC_ShootWeapon(targetPlayer.transform.position);
        }
        else
        {
            SetShopkeeperState(ShopkeeperState.Peaceful);
        }
    }

    // --- 무기 발사 RPC (Host -> All Clients) ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShootWeapon(Vector3 targetPosition)
    {
        if (weaponPrefab != null && weaponSpawnPoint != null)
        {
            GameObject bullet = Instantiate(weaponPrefab, weaponSpawnPoint.position, Quaternion.identity);
            Vector2 direction = (targetPosition - weaponSpawnPoint.position).normalized;
            // bullet.GetComponent<Projectile>().Initialize(direction, damage);
            Debug.Log($"Client: Shopkeeper shot at {targetPosition}");
        }
    }

    // --- 가장 가까운 플레이어 찾기 (Host에서만 호출) ---
    private PlayerController FindClosestPlayer()
    {
        PlayerController closestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (var playerRef in Runner.ActivePlayers)
        {
            NetworkObject playerObject = Runner.GetPlayerObject(playerRef);
            if (playerObject != null)
            {
                PlayerController pc = playerObject.GetComponent<PlayerController>();
                if (pc != null)
                {
                    float dist = Vector3.Distance(transform.position, pc.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestPlayer = pc;
                    }
                }
            }
        }
        return closestPlayer;
    }

    // --- 충돌 감지 (피격 또는 플레이어와의 접촉) ---
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!Object.HasStateAuthority) return;

        if (other.CompareTag("PlayerAttack"))
        {
            PlayerController attacker = other.GetComponentInParent<PlayerController>();
            if (attacker != null)
            {
                Debug.Log($"Host: Shopkeeper hit by player {attacker.Object.InputAuthority.PlayerId}");
                _lastAggressor = attacker.Object.InputAuthority;
                SetShopkeeperState(ShopkeeperState.Aggressive);
            }
        }
    }

    // --- 대화창 표시 로직 (클라이언트에서 OnShopkeeperStateChanged에 의해 호출) ---
    private void ShowDialogue(string[] dialogues)
    {
        if (speechBubble != null && speechText != null)
        {
            if (dialogues.Length > 0)
            {
                speechBubble.SetActive(true);
                speechText.text = dialogues[Random.Range(0, dialogues.Length)];
            }
            else
            {
                UpdateSpeechBubble(false);
            }
        }
    }

    private void UpdateSpeechBubble(bool active)
    {
        if (speechBubble != null)
        {
            speechBubble.SetActive(active);
        }
    }

    // --- 디버깅용 Gizmos ---
    private void OnDrawGizmos()
    {
        if (detectionCollider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(detectionCollider.bounds.center, detectionCollider.bounds.size);
        }
    }
    public void SetShopkeeperState(ShopkeeperState newState) // public으로 변경하여 ShopManager에서 호출 가능하도록
    {
        if (Object.HasStateAuthority)
        {
            if (CurrentState != newState)
            {
                CurrentState = newState;
                if (newState == ShopkeeperState.Warning)
                {
                    StateTimer = TickTimer.CreateFromSeconds(Runner, warningDuration);
                }
                Debug.Log($"Host: Shopkeeper state set to {newState}");
            }
        }
    }

    // 마지막으로 상점 주인을 공격/도둑질 시도한 플레이어를 설정하는 메서드 (Host만 호출 가능)
    public void SetLastAggressor(PlayerRef player)
    {
        if (Object.HasStateAuthority)
        {
            _lastAggressor = player;
            Debug.Log($"Host: Last aggressor set to Player {player.PlayerId}");
        }
    }
}