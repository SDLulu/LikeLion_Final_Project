using Fusion;
using UnityEngine;
using Unity.Cinemachine;

// 💀 플레이어 죽음 처리 전용 컴포넌트
// 📍 위치: Player 하위 오브젝트 (Player > PlayerDeathHandler)
// 🎯 목적: 플레이어 죽음 시 아이템 드롭, 시체/유령 스폰, 카메라 전환 등
public class PlayerDeathHandler : NetworkBehaviour
{
    [Header("💀 죽음 처리 설정")]
    [SerializeField] public Vector3 deathPosition = new Vector3(0, 10, 0); // 플레이어가 이동할 죽음 위치
    [SerializeField] public NetworkPrefabRef corpsePrefabRef = NetworkPrefabRef.Empty; // 시체 프리팹 참조
    [SerializeField] public NetworkPrefabRef ghostPrefabRef = NetworkPrefabRef.Empty; // 유령 프리팹 참조
    
    [Header("💰 아이템 드롭 설정")]
    [SerializeField] public float dropForce = 5f; // 아이템 드롭 시 힘
    [SerializeField] public float dropRadius = 2f; // 드롭 반경
    
    [Header("🧩 패시브 아이템 드롭 프리팹")]
    [SerializeField] private NetworkPrefabRef rocketPrefabRef = NetworkPrefabRef.Empty;
    [SerializeField] private NetworkPrefabRef wingsPrefabRef = NetworkPrefabRef.Empty;
    [SerializeField] private NetworkPrefabRef speedShoesPrefabRef = NetworkPrefabRef.Empty;
    [SerializeField] private NetworkPrefabRef jumpShoesPrefabRef = NetworkPrefabRef.Empty;
    [SerializeField] private NetworkPrefabRef magnetPrefabRef = NetworkPrefabRef.Empty;
    [SerializeField] private NetworkPrefabRef headsetPrefabRef = NetworkPrefabRef.Empty;
    [SerializeField] private NetworkPrefabRef sunglassesPrefabRef = NetworkPrefabRef.Empty;

    [Header("🪙 코인 드롭 프리팹(권종)")]
    [SerializeField] private NetworkPrefabRef coin1000PrefabRef = NetworkPrefabRef.Empty;
    [SerializeField] private NetworkPrefabRef coin500PrefabRef = NetworkPrefabRef.Empty;
    [SerializeField] private NetworkPrefabRef coin100PrefabRef = NetworkPrefabRef.Empty;
    
    // 💀 죽음 처리 관련 변수들
    [Networked] public bool IsDead { get; private set; } // 죽음 상태
    [Networked] public Vector3 DeathSpawnPosition { get; set; } // 시체/유령이 스폰될 원래 위치
    [Networked] public bool HasSpawnedDeathObjects { get; set; } // 죽음 오브젝트 스폰 여부
    [Networked] public NetworkObject GhostObject { get; set; } // 스폰된 유령 오브젝트 참조
    
    // 📎 참조할 다른 컴포넌트들
    private PlayerInventory playerInventory;
    private PlayerObjectThrower playerThrower;
    private PlayerStunInvincibleDie stunInvincibleDie;
    
    // DeadPos 태그 지점 캐시
    private Transform _deadPos;
    // 플레이어 루트 캐시
    private Transform _playerRoot;
    private Rigidbody2D _playerRootRb;
    
    public override void Spawned()
    {
        // 컴포넌트 참조 찾기
        playerInventory = GetComponentInChildren<PlayerInventory>();
        playerThrower = GetComponentInChildren<PlayerObjectThrower>();
        stunInvincibleDie = GetComponent<PlayerStunInvincibleDie>();
        // DeadPos 태그된 허브 지점 캐시 시도
        _deadPos = GameObject.FindWithTag("DeadPos")?.transform;
        // 플레이어 루트/리짓바디 캐시
        _playerRoot = transform.root;
        _playerRootRb = _playerRoot != null ? _playerRoot.GetComponent<Rigidbody2D>() : null;
        
        if (playerInventory == null)
        {
            Debug.LogError($"[{name}] PlayerInventory 컴포넌트를 찾을 수 없습니다!");
        }
        
        if (playerThrower == null)
        {
            Debug.LogError($"[{name}] PlayerObjectThrower 컴포넌트를 찾을 수 없습니다!");
        }
        
        if (stunInvincibleDie == null)
        {
            Debug.LogError($"[{name}] PlayerStunInvincibleDie 컴포넌트를 찾을 수 없습니다!");
        }
        
        Debug.Log($"[{name}] PlayerDeathHandler 초기화 완료!");
    }
    
    // 💀 죽음 처리 (외부에서 호출)
    public void Die()
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 이미 사망 상태라면 중복 처리 방지
        if (IsDead) return;
        
        // 죽음 상태 설정
        IsDead = true;
        
        // 다른 상태들 초기화
        if (stunInvincibleDie != null)
        {
            stunInvincibleDie.SetHeld(false); // 들림 상태 해제
            stunInvincibleDie.SetInvincible(false, 0f); // 무적 상태 해제
            stunInvincibleDie.SetDead(true); // 죽음 상태 설정 (외부 참조용)
        }
        
        // 원래 위치 저장 (플레이어 루트 기준)
        DeathSpawnPosition = _playerRoot != null ? _playerRoot.position : transform.position;
        
        // 💰 죽을 때 아이템/패시브/돈 드롭 처리 (원래 위치에서)
        DropHandObject();
        DropPassiveItems();
        DropMoney();
        
        // 시체 프리팹 스폰 (원래 위치에서)
        // SpawnCorpse();
        
        // 유령 플레이어 스폰 (원래 위치에서, 입력권한과 함께)
        SpawnGhostPlayer();
        
        // 카메라 전환 (RPC로 클라이언트에 알림)
        RPC_TransferCameraToGhost(GhostObject);
        
        // 플레이어를 DeadPos(허브)로 이동 (마지막에)
        Vector3 hubPos = GetDeadPosOrFallback();
        TeleportPlayer(hubPos);
        RPC_TeleportPlayer(hubPos);
        
        // 처리 완료 표시
        HasSpawnedDeathObjects = true;
        
        Debug.Log($"[{name}] 플레이어 죽음 처리 완료! 위치: {DeathSpawnPosition}");
    }
    
    // 🔄 부활 처리 (외부에서 호출)
    public void Resurrect()
    {
        // 기본 부활은 DeadPos(허브) 위치로 이동
        ResurrectAt(GetDeadPosOrFallback());
    }

    // 🔄 부활 처리 (지정 위치로)
    public void ResurrectAt(Vector3 respawnPosition)
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 사망 상태가 아니라면 처리 불필요
        if (!IsDead) return;
        
        // 전달받은 위치로 부활 (유령 위치로 강제 덮어쓰지 않음)
        TeleportPlayer(respawnPosition);
        RPC_TeleportPlayer(respawnPosition);
        
        // 유령 제거(이동 후)
        if (GhostObject != null)
        {
            var ghostController = GhostObject.GetComponent<PlayerGhostController>();
            if (ghostController != null)
            {
                ghostController.DespawnGhost();
            }
            else
            {
                Runner.Despawn(GhostObject);
            }
            GhostObject = null;
        }
        
        // 카메라를 원래 플레이어로 복귀
        RPC_TransferCameraToPlayer();
        
        // 상태 초기화
        HasSpawnedDeathObjects = false;
        IsDead = false;
        
        // PlayerStunInvincibleDie의 죽음 상태도 해제
        if (stunInvincibleDie != null)
        {
            stunInvincibleDie.SetDead(false);
        }

        // 기본 체력 회복 (StartHealth 기준 5)
        var health = GetComponentInChildren<PlayerHealth>();
        if (health != null)
        {
            int target = health.StartHealth;
            int diff = Mathf.Max(0, target - health.Health);
            if (diff > 0) health.Heal(diff);
        }
        
        Debug.Log($"[{name}] 플레이어 부활 처리 완료! 위치: {respawnPosition}");
    }
    
    // 💰 손에 든것 드롭
    private void DropHandObject()
    {
        if (playerInventory == null) return;
        
        // 들고 있는 아이템 드롭
        var heldObject = playerInventory.CurrentHeldObject;
        if (heldObject != null)
        {
            // 랜덤 방향으로 던지기
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (playerThrower != null)
            {
                playerThrower.ReleaseObject(heldObject, true, randomDirection * dropForce);
                Debug.Log($"[{name}] 들고 있던 아이템 드롭: {heldObject.name}");
            }
        }
        
    }
    
    // 💀 시체 프리팹 스폰
    private void SpawnCorpse()
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 시체 프리팹 스폰
        if (corpsePrefabRef != NetworkPrefabRef.Empty)
        {
            var corpse = Runner.Spawn(corpsePrefabRef, DeathSpawnPosition, Quaternion.identity);
            Debug.Log($"[{name}] 시체 프리팹 스폰됨: {corpse?.name ?? "null"}");
        }
        else
        {
            Debug.LogWarning($"[{name}] 시체 프리팹이 설정되지 않았습니다!");
        }
    }
    
    // 👻 유령 플레이어 스폰
    private void SpawnGhostPlayer()
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 유령 플레이어 스폰 (입력권한과 함께)
        if (ghostPrefabRef != NetworkPrefabRef.Empty)
        {
            Vector3 ghostPosition = DeathSpawnPosition + Vector3.up * 0.5f; // 시체 위 0.5f 높이
            
            // PlayerRef를 직접 전달 (DevAutoStarter와 동일한 방식)
            var ghost = Runner.Spawn(ghostPrefabRef, ghostPosition, Quaternion.identity, Object.InputAuthority);
            GhostObject = ghost;
            
            // 유령에 원래 플레이어 참조 전달
            var ghostController = ghost.GetComponent<PlayerGhostController>();
            if (ghostController != null)
            {
                ghostController.SetOriginalPlayer(Object);
            }

            // 유령 스킨을 플레이어 스킨과 동일하게 설정 (애니메이터 스왑)
            var playerAppearance = GetComponentInChildren<PlayerAppearance>();
            var ghostAppearance = ghost.GetComponent<PlayerAppearance>();
            if (playerAppearance != null && ghostAppearance != null && HasStateAuthority)
            {
                ghostAppearance.SkinKey = playerAppearance.SkinKey;
            }
            
            Debug.Log($"[{name}] 유령 플레이어 스폰됨: {ghost?.name ?? "null"} (PlayerRef: {Object.InputAuthority})");
        }
        else
        {
            Debug.LogWarning($"[{name}] 유령 프리팹이 설정되지 않았습니다!");
        }
    }
    
    // 📷 카메라를 유령으로 전환
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TransferCameraToGhost(NetworkObject ghost)
    {
        if (Object.HasInputAuthority && ghost != null)
        {
            var mover = CameraMover.Inst;
            if (mover != null)
            {
                mover.SetFollowAndLookAtForAll(ghost.transform);
                Debug.Log($"[{name}] 모든 카메라 Follow/LookAt을 유령으로 전환");
            }
            else
            {
                // 폴백: 기존 단일 카메라 탐색
                var cinemachineCamera = UnityEngine.Object.FindFirstObjectByType<CinemachineCamera>();
                if (cinemachineCamera != null)
                {
                    cinemachineCamera.Follow = ghost.transform;
                    cinemachineCamera.LookAt = ghost.transform;
                }
                Debug.LogWarning($"[{name}] CameraMover가 없어 폴백 경로 사용");
            }
        }
    }
    
    // 📷 카메라를 원래 플레이어로 복귀
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TransferCameraToPlayer()
    {
        if (Object.HasInputAuthority)
        {
            var mover = CameraMover.Inst;
            if (mover != null)
            {
                var target = _playerRoot != null ? _playerRoot : transform;
                mover.SetFollowAndLookAtForAll(target);
                Debug.Log($"[{name}] 모든 카메라 Follow/LookAt을 플레이어로 복귀");
            }
            else
            {
                var cinemachineCamera = UnityEngine.Object.FindFirstObjectByType<CinemachineCamera>();
                if (cinemachineCamera != null)
                {
                    var target = _playerRoot != null ? _playerRoot : transform;
                    cinemachineCamera.Follow = target;
                    cinemachineCamera.LookAt = target;
                }
                Debug.LogWarning($"[{name}] CameraMover가 없어 폴백 경로 사용");
            }
        }
    }
    
    // 🔄 부활 시 정리 작업 (외부에서 호출 가능한 메서드 - 호환성용)
    public void OnResurrect()
    {
        // Resurrect() 메서드를 호출
        Resurrect();
    }
    
    // 🧩 패시브 아이템 드롭
    private void DropPassiveItems()
    {
        if (!HasStateAuthority) return;
        if (playerInventory == null) return;

        Vector3 center = DeathSpawnPosition;

        void TryDrop(bool hasItem, NetworkPrefabRef prefabRef)
        {
            if (!hasItem) return;
            if (prefabRef == NetworkPrefabRef.Empty) return;
            var spawned = SpawnAndImpulse(prefabRef, center, dropForce, dropRadius);
            if (spawned != null)
            {
                // 보유 상태 해제 (스폰된 프리팹 타입으로 판별)
                playerInventory.RemovePassiveItem(spawned.gameObject);
            }
        }

        TryDrop(playerInventory.hasRocket, rocketPrefabRef);
        TryDrop(playerInventory.hasWings, wingsPrefabRef);
        TryDrop(playerInventory.hasSpeedShoes, speedShoesPrefabRef);
        TryDrop(playerInventory.hasJumpShoes, jumpShoesPrefabRef);
        TryDrop(playerInventory.hasMagnet, magnetPrefabRef);
        TryDrop(playerInventory.hasHeadset, headsetPrefabRef);
        TryDrop(playerInventory.hasSunglasses, sunglassesPrefabRef);
    }

    // 🪙 돈 드롭 (권종 분해: 1000/500/100)
    private void DropMoney()
    {
        if (!HasStateAuthority) return;
        if (playerInventory == null) return;

        int total = playerInventory.CurrentMoney;
        if (total <= 0) return;

        int tmp = total;
        int thousands = tmp / 1000; tmp %= 1000;
        int fiveHundreds = tmp / 500; tmp %= 500;
        int hundreds = tmp / 100; tmp %= 100;

        Debug.Log($"[DropMoney] total={total} -> 1000x{thousands}, 500x{fiveHundreds}, 100x{hundreds}, remainder(calc)={tmp}");
        if (thousands > 0 && coin1000PrefabRef == NetworkPrefabRef.Empty)
        {
            Debug.LogWarning("[DropMoney] coin1000PrefabRef is EMPTY. 1000원 권종이 스폰되지 않습니다.");
        }
        if (fiveHundreds > 0 && coin500PrefabRef == NetworkPrefabRef.Empty)
        {
            Debug.LogWarning("[DropMoney] coin500PrefabRef is EMPTY. 500원 권종이 스폰되지 않습니다.");
        }
        if (hundreds > 0 && coin100PrefabRef == NetworkPrefabRef.Empty)
        {
            Debug.LogWarning("[DropMoney] coin100PrefabRef is EMPTY. 100원 권종이 스폰되지 않습니다.");
        }

        Vector3 center = DeathSpawnPosition;

        int SpawnMany(NetworkPrefabRef prefabRef, int count)
        {
            if (prefabRef == NetworkPrefabRef.Empty) return 0;
            int success = 0;
            for (int i = 0; i < count; i++)
            {
                var spawned = SpawnAndImpulse(prefabRef, center, dropForce, dropRadius);
                if (spawned == null)
                {
                    Debug.LogWarning($"[DropMoney] Spawn failed for coin prefab (i={i})");
                }
                else
                {
                    Debug.Log($"[DropMoney] Spawned coin prefab={spawned.name} at {spawned.transform.position}");
                    success++;
                }
            }
            return success;
        }

        int success1000 = SpawnMany(coin1000PrefabRef, thousands);
        int success500 = SpawnMany(coin500PrefabRef, fiveHundreds);
        int success100 = SpawnMany(coin100PrefabRef, hundreds);

        int droppedAmount = success1000 * 1000 + success500 * 500 + success100 * 100;
        int newBalance = Mathf.Max(0, playerInventory.CurrentMoney - droppedAmount);
        playerInventory.CurrentMoney = newBalance;
        Debug.Log($"[DropMoney] dropped={droppedAmount}, newBalance={newBalance}");
    }

    // 공통: 네트워크 스폰 + 랜덤 임펄스
    private NetworkObject SpawnAndImpulse(NetworkPrefabRef prefabRef, Vector3 center, float force, float radius)
    {
        if (!HasStateAuthority) return null;
        Vector2 offset2D = Random.insideUnitCircle * Mathf.Max(0.1f, radius);
        Vector3 spawnPos = center + new Vector3(offset2D.x, offset2D.y, 0f);

        var spawned = Runner.Spawn(prefabRef, spawnPos, Quaternion.identity);
        if (spawned == null) return null;

        var rb = spawned.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dir = (Vector2)(spawnPos - center);
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Random.insideUnitCircle.normalized;
            }
            else
            {
                dir = dir.normalized;
            }
            rb.AddForce(dir * force, ForceMode2D.Impulse);
        }

        return spawned;
    }

    // DeadPos 허브 위치 반환 (없으면 기존 deathPosition 사용)
    private Vector3 GetDeadPosOrFallback()
    {
        if (_deadPos == null)
        {
            _deadPos = GameObject.FindWithTag("DeadPos")?.transform;
        }
        return _deadPos != null ? _deadPos.position : deathPosition;
    }

    // 플레이어 텔레포트 (권한 측에서만)
    private void TeleportPlayer(Vector3 targetPosition)
    {
        var target = _playerRoot != null ? _playerRoot : transform;
        NetworkMoveUtil.Teleport(this, target, targetPosition);
    }

    // 시각 위치를 모든 클라이언트에 즉시 반영 (NetworkTransform이 없을 때 보정)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TeleportPlayer(Vector3 targetPosition)
    {
        if (HasStateAuthority) return;
        var target = _playerRoot != null ? _playerRoot : transform;
        NetworkMoveUtil.Teleport(this, target, targetPosition);
    }
} 