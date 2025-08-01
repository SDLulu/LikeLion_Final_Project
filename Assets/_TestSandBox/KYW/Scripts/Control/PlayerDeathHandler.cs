using Fusion;
using UnityEngine;
using Unity.Cinemachine;

// 💀 플레이어 죽음 처리 전용 컴포넌트
// 📍 위치: Player 하위 오브젝트 (Player > PlayerDeathHandler)
// 🎯 목적: 플레이어 죽음 시 아이템 드롭, 시체/유령 스폰, 카메라 전환 등
public class PlayerDeathHandler : NetworkBehaviour
{
    [Header("💀 죽음 처리 설정")]
    [SerializeField] private Vector3 deathPosition = new Vector3(0, 10, 0); // 플레이어가 이동할 죽음 위치
    [SerializeField] private NetworkPrefabId corpsePrefabId = NetworkPrefabId.FromRaw(100001); // 시체 프리팹 ID
    [SerializeField] private NetworkPrefabId ghostPrefabId = NetworkPrefabId.FromRaw(100002); // 유령 프리팹 ID
    
    [Header("💰 아이템 드롭 설정")]
    [SerializeField] private float dropForce = 5f; // 아이템 드롭 시 힘
    [SerializeField] private float dropRadius = 2f; // 드롭 반경
    
    // 💀 죽음 처리 관련 변수들
    [Networked] private Vector3 DeathSpawnPosition { get; set; } // 시체/유령이 스폰될 원래 위치
    [Networked] private bool HasSpawnedDeathObjects { get; set; } // 죽음 오브젝트 스폰 여부
    [Networked] private NetworkObject GhostObject { get; set; } // 스폰된 유령 오브젝트 참조
    
    // 📎 참조할 다른 컴포넌트들
    private PlayerInventory playerInventory;
    private PlayerObjectThrower playerThrower;
    private PlayerStunInvincibleDie stunInvincibleDie;
    
    public override void Spawned()
    {
        // 컴포넌트 참조 찾기
        playerInventory = GetComponent<PlayerInventory>();
        playerThrower = GetComponentInChildren<PlayerObjectThrower>();
        stunInvincibleDie = GetComponent<PlayerStunInvincibleDie>();
        
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
    
    public override void FixedUpdateNetwork()
    {
        // 💀 죽음 처리 로직 (한 번만 실행)
        if (stunInvincibleDie != null && stunInvincibleDie.IsDead && !HasSpawnedDeathObjects && HasStateAuthority)
        {
            HandlePlayerDeath();
        }
    }
    
    // 💀 플레이어 죽음 처리 (한 번만 실행)
    private void HandlePlayerDeath()
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 이미 처리되었다면 중복 실행 방지
        if (HasSpawnedDeathObjects) return;
        
        // 원래 위치 저장
        DeathSpawnPosition = transform.position;
        
        // 💰 죽을 때 아이템 드롭 처리 (원래 위치에서)
        DropAllItems();
        
        // 시체 프리팹 스폰 (원래 위치에서)
        SpawnCorpse();
        
        // 유령 플레이어 스폰 (원래 위치에서, 입력권한과 함께)
        SpawnGhostPlayer();
        
        // 카메라 전환 (RPC로 클라이언트에 알림)
        RPC_TransferCameraToGhost(GhostObject);
        
        // 플레이어를 죽음 위치로 이동 (마지막에)
        transform.position = deathPosition;
        
        // 처리 완료 표시
        HasSpawnedDeathObjects = true;
        
        Debug.Log($"[{name}] 플레이어 죽음 처리 완료! 위치: {DeathSpawnPosition}");
    }
    
    // 💰 모든 아이템 드롭
    private void DropAllItems()
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
        
        // TODO: 패시브 아이템, 돈 등도 드롭 처리
        // DropPassiveItems();
        // DropMoney();
    }
    
    // 💀 시체 프리팹 스폰
    private void SpawnCorpse()
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 시체 프리팹 스폰
        if (corpsePrefabId.IsValid)
        {
            var corpse = Runner.Spawn(corpsePrefabId, DeathSpawnPosition, Quaternion.identity);
            Debug.Log($"[{name}] 시체 프리팹 스폰됨: {corpse?.name ?? "null"}");
        }
        else
        {
            Debug.LogWarning($"[{name}] 시체 프리팹 ID가 설정되지 않았습니다!");
        }
    }
    
    // 👻 유령 플레이어 스폰
    private void SpawnGhostPlayer()
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 유령 플레이어 스폰 (입력권한과 함께)
        if (ghostPrefabId.IsValid)
        {
            Vector3 ghostPosition = DeathSpawnPosition + Vector3.up * 0.5f; // 시체 위 0.5f 높이
            var ghost = Runner.Spawn(ghostPrefabId, ghostPosition, Quaternion.identity, Object.InputAuthority);
            GhostObject = ghost;
            
            // 유령에 원래 플레이어 참조 전달
            var ghostController = ghost.GetComponent<PlayerGhostController>();
            if (ghostController != null)
            {
                ghostController.SetOriginalPlayer(Object);
            }
            
            Debug.Log($"[{name}] 유령 플레이어 스폰됨: {ghost?.name ?? "null"}");
        }
        else
        {
            Debug.LogWarning($"[{name}] 유령 프리팹 ID가 설정되지 않았습니다!");
        }
    }
    
    // 📷 카메라를 유령으로 전환
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TransferCameraToGhost(NetworkObject ghost)
    {
        if (Object.HasInputAuthority && ghost != null)
        {
            // 시네머신 카메라 찾기
            var cinemachineCamera = UnityEngine.Object.FindFirstObjectByType<CinemachineCamera>();
            if (cinemachineCamera != null)
            {
                // 시네머신 카메라의 Follow/LookAt을 유령으로 변경
                cinemachineCamera.Follow = ghost.transform;
                cinemachineCamera.LookAt = ghost.transform;
                Debug.Log($"[{name}] 시네머신 카메라가 유령으로 전환됨!");
            }
            else
            {
                Debug.LogWarning($"[{name}] 시네머신 카메라를 찾을 수 없습니다!");
            }
        }
    }
    
    // 📷 카메라를 원래 플레이어로 복귀
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TransferCameraToPlayer()
    {
        if (Object.HasInputAuthority)
        {
            // 시네머신 카메라 찾기
            var cinemachineCamera = UnityEngine.Object.FindFirstObjectByType<CinemachineCamera>();
            if (cinemachineCamera != null)
            {
                // 시네머신 카메라의 Follow/LookAt을 원래 플레이어로 복귀
                cinemachineCamera.Follow = transform;
                cinemachineCamera.LookAt = transform;
                Debug.Log($"[{name}] 시네머신 카메라가 원래 플레이어로 복귀됨!");
            }
            else
            {
                Debug.LogWarning($"[{name}] 시네머신 카메라를 찾을 수 없습니다!");
            }
        }
    }
    
    // 🔄 부활 시 정리 작업
    public void OnResurrect()
    {
        // 유령 제거
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
        
        Debug.Log($"[{name}] 부활 시 정리 작업 완료!");
    }
    
    // TODO: 패시브 아이템 드롭 (추후 구현)
    private void DropPassiveItems()
    {
        // 패시브 아이템 드롭 로직
        Debug.Log($"[{name}] 패시브 아이템 드롭 (구현 예정)");
    }
    
    // TODO: 돈 드롭 (추후 구현)
    private void DropMoney()
    {
        // 돈 드롭 로직
        Debug.Log($"[{name}] 돈 드롭 (구현 예정)");
    }
} 