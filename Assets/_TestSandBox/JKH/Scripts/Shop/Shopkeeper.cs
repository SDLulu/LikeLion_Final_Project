using Fusion;
using Fusion.Addons.Physics;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public enum ShopkeeperState
{
    Passive,    // 평온한 상태 (기본)
    Talking,    // 플레이어와 대화 중
    Aggressive, // 플레이어가 물건을 훔치려 할 때 공격적 상태
    Attacking,
    Defeated    // 쓰러진 상태 (추가적인 구현이 필요할 수 있음)
}
// TextMeshPro를 사용하는 경우

public class Shopkeeper : NetworkBehaviour, IPlayerInteraction
{
    [Header("Shopkeeper Settings")]
    [SerializeField] private TextMeshProUGUI speechBubbleText; // 대화 말풍선 텍스트
    [SerializeField] private GameObject speechBubbleObject; // 대화 말풍선 오브젝트 (활성화/비활성화용)
    [SerializeField] private float speechDisplayDuration = 3f; // 말풍선 표시 시간
    [SerializeField] private float playerDetectionRadius = 10f; // 플레이어 감지 반경 (OverlapCircle용)
    [SerializeField] private LayerMask playerLayerMask; // 플레이어 레이어 마스크

    [Header("Movement & Combat Settings")]
    [SerializeField] private float chaseSpeed = 5.0f; // 쫓아갈 때의 이동 속도
    [SerializeField] private float stopDistance = 0.5f; // 플레이어에게 얼마나 가까이 다가가면 멈출지 (공격 범위 개념)
    private NetworkRigidbody2D _netRigidbody; // Shopkeeper의 물리 이동을 위한 NetworkRigidbody2D
    // private Animator _animator; // 애니메이션이 있다면 추가

    // 🌐 네트워크 동기화 변수들
    [Networked, OnChangedRender(nameof(OnShopkeeperStateChanged))]
    public ShopkeeperState CurrentState { get; set; } = ShopkeeperState.Passive;

    [Networked]
    public PlayerRef LastAggressor { get; set; } = PlayerRef.None; // 마지막으로 상점 주인을 화나게 한 플레이어
                                                                   // ⭐️ 추가: 현재 추적/공격하고 있는 대상을 저장하는 변수
    [Networked]
    private PlayerRef CurrentTarget { get; set; } = PlayerRef.None;

    // 로컬 클라이언트에서만 사용될 변수
    private float _speechTimer;
    private PlayerRef _currentInteractingPlayer = PlayerRef.None; // 현재 상호작용 중인 플레이어 (호스트에서만 설정)
    private ShopManager _shopManager; // ShopManager 참조 (도난 감지 시 필요)
    private Animator _animator;

    // ⚔️ 공격 관련 (예시) - 실제 공격 로직은 별도 컴포넌트나 FixedUpdateNetwork 내에서 구현
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1f;
    private TickTimer _attackTimer;

    // IPlayerInteraction.IsHeld (bool)을 만족하기 위한 래핑
    [Networked] private NetworkBool heldFlag { get; set; }
    public bool IsHeld => heldFlag;
    public override void Spawned()
    {
        // 씬에서 ShopManager를 찾아 참조합니다.
        _shopManager = FindFirstObjectByType<ShopManager>();
        _netRigidbody = GetComponent<NetworkRigidbody2D>();
        _animator = GetComponentInChildren<Animator>();
        if (_netRigidbody == null)
        {
            // 만약 Shopkeeper 프리팹에 NetworkRigidbody2D가 없다면 경고를 띄웁니다.
            Debug.LogError("Shopkeeper: NetworkRigidbody2D component not found! Movement will not work.");
        }
        if (_shopManager == null)
        {
            Debug.LogError("Shopkeeper: ShopManager not found in scene!");
        }

        // 초기 상태 설정
        if (Object.HasStateAuthority)
        {
            CurrentState = ShopkeeperState.Passive;
            LastAggressor = PlayerRef.None;
        }

        // 말풍선 초기 비활성화
        if (speechBubbleObject != null)
        {
            speechBubbleObject.SetActive(false);
        }
    }

    // ⭐️ 수정: 로직과 비주얼이 분리된 FixedUpdateNetwork
    public override void FixedUpdateNetwork()
    {
        // 호스트(StateAuthority)에서만 로직 처리
        if (Object.HasStateAuthority)
        {
            switch (CurrentState)
            {
                case ShopkeeperState.Passive:
                    // Passive 상태가 되면 타겟을 초기화합니다.
                    if (CurrentTarget.IsNone == false)
                    {
                        CurrentTarget = PlayerRef.None;
                    }
                    DetectAndGreetPlayer();
                    break;

                case ShopkeeperState.Aggressive:
                    // "누구를 쫓을지" 결정하고 CurrentTarget에 저장합니다.
                    if (LastAggressor.IsNone)
                    {
                        // 모두에게 화가 난 경우, 가장 가까운 플레이어를 대상으로 설정
                        CurrentTarget = FindNearestPlayer();
                    }
                    else
                    {
                        // 특정 대상에게 화가 난 경우, 그 대상을 추적
                        CurrentTarget = LastAggressor;
                    }
                    // 결정된 타겟을 향해 실제 추적 로직을 실행합니다.
                    ExecuteChaseLogic(CurrentTarget);
                    break;

                case ShopkeeperState.Attacking:
                    // 공격 쿨다운이 끝나면 다시 Aggressive 상태로 돌아가 다음 행동을 결정합니다.
                    if (_attackTimer.ExpiredOrNotRunning(Runner))
                    {
                        SetShopkeeperState(ShopkeeperState.Aggressive);
                    }
                    break;

                case ShopkeeperState.Talking:
                case ShopkeeperState.Defeated:
                    // 이 상태들에서는 특별한 로직이 없습니다.
                    break;
            }
        }
    }

    // 🎨 렌더링 (모든 클라이언트에서 시각적 업데이트)
    public override void Render()
    {
        // Networked 변수가 변경될 때 OnShopkeeperStateChanged 콜백이 호출되므로,
        // Render에서는 주로 애니메이션이나 비주얼 효과를 적용합니다.
        // 현재 상태에 따른 애니메이션 트리거 등
        UpdateVisuals();
    }

    // --- 상태 변경 메서드 (Host에서만 호출) ---
    // ShopManager에서 호출됩니다.
    public void SetShopkeeperState(ShopkeeperState newState)
    {
        if (!Object.HasStateAuthority) return;
        CurrentState = newState;
        Debug.Log($"Host: Shopkeeper state changed to {newState}");
        // 상태 변경 시 필요한 초기화 로직
        if (newState == ShopkeeperState.Talking)
        {
            _speechTimer = speechDisplayDuration; // 대화 타이머 시작
        }
    }

    // ShopManager에서 호출됩니다.
    public void SetLastAggressor(PlayerRef aggressor)
    {
        if (!Object.HasStateAuthority) return;
        LastAggressor = aggressor;
        Debug.Log($"Host: Shopkeeper's last aggressor set to Player {aggressor.PlayerId}");
    }

    // --- 플레이어 감지 및 대화 ---
    private void DetectAndGreetPlayer()
    {
        if (!Object.HasStateAuthority) return;

        if (CurrentState == ShopkeeperState.Aggressive) return;

        Collider2D[] playersInRadius = Physics2D.OverlapCircleAll(transform.position, playerDetectionRadius, playerLayerMask);
        if (playersInRadius.Length > 0)
        {
            foreach (Collider2D playerCollider in playersInRadius)
            {
                // ⭐️ 수정: PlayerController 대신 NetworkObject를 찾아 InputAuthority를 가져옴
                NetworkObject playerNetworkObject = playerCollider.GetComponentInParent<NetworkObject>();

                // NetworkObject가 있고 InputAuthority가 유효하다면 플레이어로 간주
                if (playerNetworkObject != null && playerNetworkObject.InputAuthority.IsNone == false)
                {
                    // 아직 상호작용 중인 플레이어가 없거나 새로운 플레이어가 감지되었을 때
                    if (_currentInteractingPlayer.IsNone || _currentInteractingPlayer != playerNetworkObject.InputAuthority)
                    {
                        _currentInteractingPlayer = playerNetworkObject.InputAuthority;
                        SetShopkeeperState(ShopkeeperState.Talking);
                        Rpc_DisplaySpeechBubble(_currentInteractingPlayer, "환영합니다, 손님!");
                        return; // 한 명의 플레이어만 감지하여 처리
                    }
                }
            }
        }
    }
    // ⭐️ 새로 추가: 실제 추적과 공격 '로직'만 담당하는 메서드
    private void ExecuteChaseLogic(PlayerRef target)
    {
        // 유효한 타겟이 없으면 Passive 상태로 전환합니다.
        if (target.IsNone)
        {
            SetShopkeeperState(ShopkeeperState.Passive);
            return;
        }

        NetworkObject targetObject = Runner.GetPlayerObject(target);
        if (targetObject == null)
        {
            // 대상 플레이어가 사라졌다면 Passive 상태로 전환합니다.
            SetShopkeeperState(ShopkeeperState.Passive);
            if (!LastAggressor.IsNone)
            {
                LastAggressor = PlayerRef.None;
            }
            return;
        }

        float distance = Vector2.Distance(transform.position, targetObject.transform.position);

        // 공격 로직 (기존 ChasePlayer 메서드와 동일)
        if (_attackTimer.ExpiredOrNotRunning(Runner) && distance <= attackRange)
        {
            if (_netRigidbody.Rigidbody.linearVelocity.sqrMagnitude > 0)
            {
                _netRigidbody.Rigidbody.linearVelocity = Vector2.zero;
            }
            Rpc_AttackPlayer(target, attackDamage);
            _attackTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);
            SetShopkeeperState(ShopkeeperState.Attacking);
        }
        // 추적 로직 (기존 ChasePlayer 메서드와 동일)
        else if (distance > stopDistance)
        {
            Vector2 directionToTarget = (targetObject.transform.position - transform.position).normalized;
            _netRigidbody.Rigidbody.linearVelocity = new Vector2(directionToTarget.x * chaseSpeed, _netRigidbody.Rigidbody.linearVelocity.y);
        }
        else
        {
            _netRigidbody.Rigidbody.linearVelocity = Vector2.zero;
        }
    }
    // Shopkeeper.cs 에 붙여넣을 수정된 ChasePlayer 메서드

    private void ChasePlayer(PlayerRef target)
    {
        // ⭐️ 수정: 전달받은 target이 유효하지 않은 경우에만 Passive로 전환합니다.
        // 기존의 LastAggressor 체크는 삭제합니다.
        if (target.IsNone)
        {
            SetShopkeeperState(ShopkeeperState.Passive);
            return;
        }

        NetworkObject aggressorObject = Runner.GetPlayerObject(target);
        if (aggressorObject == null)
        {
            // 대상 플레이어가 사라졌다면 (예: 연결 끊김) 다시 Passive 상태로 돌아갑니다.
            SetShopkeeperState(ShopkeeperState.Passive);

            // 만약 특정 공격 대상이 있었다면 초기화해줍니다.
            if (!LastAggressor.IsNone)
            {
                LastAggressor = PlayerRef.None;
            }
            return;
        }

        float distance = Vector2.Distance(transform.position, aggressorObject.transform.position);

        // 공격 쿨다운이 끝났고, 플레이어가 공격 범위 안에 있다면
        if (_attackTimer.ExpiredOrNotRunning(Runner) && distance <= attackRange)
        {
            // 1. 제자리에 멈춥니다.
            if (_netRigidbody.Rigidbody.linearVelocity.sqrMagnitude > 0)
            {
                _netRigidbody.Rigidbody.linearVelocity = Vector2.zero;
            }

            // 2. 공격 RPC를 호출하고 타이머를 시작합니다.
            Rpc_AttackPlayer(target, attackDamage);
            _attackTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);

            // 3. 상태를 'Attacking'으로 변경하여 이동을 막습니다.
            SetShopkeeperState(ShopkeeperState.Attacking);
            //_animator.SetTrigger("AttackTrigger");
            Debug.Log($"Host: Shopkeeper changing state to Attacking.");
        }
        // 플레이어가 공격 범위 밖에 있다면 추적합니다.
        else if (distance > stopDistance)
        {
            _animator.SetBool("IsRunning", true);
            Vector2 directionToTarget = (aggressorObject.transform.position - transform.position).normalized;
            _netRigidbody.Rigidbody.linearVelocity = new Vector2(directionToTarget.x * chaseSpeed, _netRigidbody.Rigidbody.linearVelocity.y);
        }
        // 공격 범위와 정지 거리 사이에 있다면 멈춥니다.
        else
        {
            _animator.SetBool("IsRunning", false);
            _netRigidbody.Rigidbody.linearVelocity = Vector2.zero;
        }
    }

    // --- RPC: 대화 말풍선 표시 (호스트 -> 특정 클라이언트) ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_DisplaySpeechBubble(PlayerRef targetPlayer, string message)
    {
        if (speechBubbleObject != null && speechBubbleText != null)
        {
            speechBubbleText.text = message;
            speechBubbleObject.SetActive(true);
            // 클라이언트에서 말풍선 표시 타이머 관리 (Render에서 갱신하거나 별도 Coroutine)
            Debug.Log($"Client: Shopkeeper says: {message}");
        }
    }

    // --- RPC: 플레이어 공격 (호스트 -> 특정 클라이언트) ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_AttackPlayer(PlayerRef targetPlayer, float damage)
    {
        // 공격 애니메이션 트리거 (모든 클라이언트)
        if (_animator != null)
        {
            _animator.SetTrigger("AttackTrigger");
        }
        // 예를 들어 Animator.SetTrigger("Attack")
        Debug.Log($"Client: Shopkeeper plays attack animation.");

        // 데미지 적용은 PlayerHealth 컴포넌트에서 RPC를 받아 처리하는 것이 좋습니다.
        // NetworkObject playerObject = Runner.GetPlayerObject(targetPlayer);
        // if (playerObject != null)
        // {
        //     PlayerHealth playerHealth = playerObject.GetComponent<PlayerHealth>();
        //     if (playerHealth != null)
        //     {
        //         playerHealth.Rpc_TakeDamage(damage);
        //     }
        // }
    }

    // --- OnChanged 콜백: ShopkeeperState 변경 감지 ---
    // [Networked, OnChangedRender(nameof(OnShopkeeperStateChanged))] 에 의해 호출됩니다.
    void OnShopkeeperStateChanged()
    {
        // 모든 클라이언트에서 상태 변경에 따른 비주얼/애니메이션 업데이트
        UpdateVisuals();
        Debug.Log($"Client (ID: {Object.Id}): Shopkeeper state changed to {CurrentState}");

        // 대화 상태가 아닐 때 말풍선 숨김
        if (CurrentState != ShopkeeperState.Talking && speechBubbleObject != null && speechBubbleObject.activeSelf)
        {
            speechBubbleObject.SetActive(false);
        }
    }
    // ⭐️ 특정 범인 없이 모두를 적대하는 상태로 변경하는 메서드
    public void EnrageAgainstAllPlayers()
    {
        if (!Object.HasStateAuthority) return;
        SetShopkeeperState(ShopkeeperState.Aggressive);
        LastAggressor = PlayerRef.None; // 특정 공격 대상을 초기화
    }

    private PlayerRef FindNearestPlayer()
    {
        float closestDistSqr = float.MaxValue;
        PlayerRef nearestPlayer = PlayerRef.None;

        foreach (var player in Runner.ActivePlayers)
        {
            var playerObj = Runner.GetPlayerObject(player);
            if (playerObj != null)
            {
                float distSqr = (playerObj.transform.position - transform.position).sqrMagnitude;
                if (distSqr < closestDistSqr)
                {
                    closestDistSqr = distSqr;
                    nearestPlayer = player;
                }
            }
        }
        return nearestPlayer;
    }

    // ⭐️ 수정: 이제 이 메서드는 '시각적인 처리'만 담당합니다.
    private void UpdateVisuals()
    {
        // 애니메이션 업데이트 로직
        bool isRunning = (CurrentState == ShopkeeperState.Aggressive || CurrentState == ShopkeeperState.Attacking) && _netRigidbody.Rigidbody.linearVelocity.x != 0;
        _animator.SetBool("IsRunning", isRunning);

        if (CurrentState == ShopkeeperState.Talking && speechBubbleObject != null)
        {
            speechBubbleObject.SetActive(true);
        }

        // --- 방향 전환(Flip) 로직 ---
        // Aggressive 또는 Attacking 상태이고, 유효한 타겟이 있을 때만 방향을 전환합니다.
        if ((CurrentState == ShopkeeperState.Aggressive || CurrentState == ShopkeeperState.Attacking) && !CurrentTarget.IsNone)
        {
            NetworkObject targetObject = Runner.GetPlayerObject(CurrentTarget);
            if (targetObject != null)
            {
                // 목표가 왼쪽에 있는지 오른쪽에 있는지 판단합니다.
                float directionX = targetObject.transform.position.x - transform.position.x;

                // 로컬 스케일(localScale)의 x값을 조절하여 바라보는 방향을 바꿉니다.
                if (directionX < 0)
                {
                    transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                }
                else if (directionX > 0)
                {
                    transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                }
            }
        }
    }

    // --- 디버그 용 (플레이어 감지 반경 시각화) ---
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRadius);

        if (Application.isPlaying && CurrentState == ShopkeeperState.Aggressive && LastAggressor.IsNone == false)
        {
            NetworkObject aggressorObject = Runner?.GetPlayerObject(LastAggressor);
            if (aggressorObject != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, aggressorObject.transform.position);
                Gizmos.DrawWireSphere(transform.position, attackRange);
            }
        }
    }

    public void ApplyKnockback(Vector2 force, float stunDuration = 0)
    {
        throw new System.NotImplementedException();
    }

    public void TakeDamage(int damage)
    {
        throw new System.NotImplementedException();
    }

    public void ApplyStun(float duration)
    {
        throw new System.NotImplementedException();
    }

    public void SetInvincible(bool value, float duration = 0)
    {
        throw new System.NotImplementedException();
    }

    public void OnPickedUp()
    {
        if (!Object.HasStateAuthority) return;
        heldFlag = true;
    }

    public void OnReleased()
    {
        if (!Object.HasStateAuthority) return;
        heldFlag = false;
    }
}