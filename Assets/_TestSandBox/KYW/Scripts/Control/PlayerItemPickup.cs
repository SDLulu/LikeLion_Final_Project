using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// 아이템 감지 및 줍기 담당 컴포넌트
// 📍 위치: Hand 하위 오브젝트 (Player > Hand > PlayerItemPickup)
// 🎯 목적: CircleCollider2D로 주변 아이템 감지 + Space+웅크리기로 픽업
public class PlayerItemPickup : NetworkBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private LayerMask itemLayerMask = -1;      // 🎛️ 아이템으로 인식할 레이어들
    [SerializeField] private CircleCollider2D pickupTrigger;    // 🔵 감지용 원형 트리거 (Hand에 위치)
    [SerializeField] private float defaultTriggerRadius = 1.5f; // 📏 기본 감지 반경
    
    // 🌐 네트워크 동기화 변수들 (모든 클라이언트가 동일한 값을 가짐)
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }        // 🎮 이전 프레임 버튼 상태 (래칭용)
    
    // 🔍 게임 로직에 필요한 속성 (PlayerInventory에서 가져옴)
    public GameObject CurrentHeldObject => inventory != null ? inventory.CurrentHeldObject : null; // 손에 든 오브젝트
    public bool HasHeldObject => CurrentHeldObject != null; // 손에 든 것 보유 여부
    
    // 🎯 트리거로 감지된 주변 아이템들 (로컬에서만 관리, 네트워크 동기화 안됨)
    private HashSet<GameObject> nearbyItems = new HashSet<GameObject>();
    
    // 📎 참조할 다른 컴포넌트들
    private SpelunkyPlayerController playerController;
    private PlayerMovement playerMovement;
    private PlayerInventory inventory;
    
    // 🚀 NetworkBehaviour 생성 시 호출 (모든 클라이언트에서 실행)
    public override void Spawned()
    {
        // 모든 컴포넌트 참조를 한 번에 설정
        Transform parentPlayer = transform.parent;  // 🏠 부모 = Player 오브젝트
        if (parentPlayer != null)
        {
            playerController = parentPlayer.GetComponent<SpelunkyPlayerController>();
            playerMovement = parentPlayer.GetComponent<PlayerMovement>();
            inventory = parentPlayer.GetComponent<PlayerInventory>();
            // 필수 컴포넌트 검증
            if (playerController == null)
                Debug.LogError($"[{name}] SpelunkyPlayerController 컴포넌트를 찾을 수 없습니다!");
            if (playerMovement == null)
                Debug.LogError($"[{name}] PlayerMovement 컴포넌트를 찾을 수 없습니다!");
            if (inventory == null)
                Debug.LogError($"[{name}] PlayerInventory 컴포넌트를 찾을 수 없습니다!");
        }
        else
        {
            Debug.LogError($"[{name}] PlayerItemPickup이 Player 오브젝트의 하위가 아닙니다!");
        }
        
        SetupTriggerCollider(); // 🔵 트리거 콜라이더 설정
        
        // 🌐 CurrentItem getter가 이제 네트워크 ID로부터 실시간으로 찾기 때문에
        // 👉 별도의 아이템 복원 코드가 불필요함
    }
    
    // 🔵 아이템 감지용 원형 트리거 설정
    private void SetupTriggerCollider()
    {
        if (pickupTrigger == null)
        {
            // 🔍 기존 CircleCollider2D가 있는지 확인
            pickupTrigger = GetComponent<CircleCollider2D>();
            
            if (pickupTrigger == null)
            {
                // 📝 없으면 새로 생성
                pickupTrigger = gameObject.AddComponent<CircleCollider2D>();
                pickupTrigger.radius = defaultTriggerRadius;  // 📏 감지 반경 설정
            }
        }
        
        pickupTrigger.isTrigger = true;  // ⚡ 트리거 모드로 설정 (물리 충돌 X, 감지만 O)
    }
    
    // 🎮 입력 처리 (SpelunkyPlayerController에서 호출)
    // 👉 InputAuthority(로컬 플레이어)에서만 호출됨
    public void ProcessInput(SpelunkyPlayerData input)
    {
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;

        // 🎮 오브젝트 픽업 조건: Space키 + 웅크린 상태 + 아무것도 안 들고 있을 때
        if (pressed.IsSet(SpelunkyInputButtons.PickupItem))
        {
            Debug.Log($"[PlayerItemPickup] Pickup 입력 감지됨");
            if (!HasHeldObject && playerMovement != null && playerMovement.IsDucking)
            {
                Debug.Log($"[PlayerItemPickup] 웅크리기 상태, 손에 든 것 없음");
                // 🔍 가장 가까운 픽업 대상 찾기
                var nearest = FindNearestItem();
                if (nearest != null)
                {
                    Debug.Log($"[PlayerItemPickup] 가장 가까운 아이템/오브젝트: {nearest.name}");
                    var netObj = nearest.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        Debug.Log($"[PlayerItemPickup] NetworkObject 있음, RPC 호출: {netObj.Id}");
                        // 📡 Host(StateAuthority)에게 픽업 요청 RPC 전송
                        PickupObjectRpc(netObj.Id);
                    }
                    else
                    {
                        Debug.Log($"[PlayerItemPickup] NetworkObject 없음");
                    }
                }
                else
                {
                    Debug.Log($"[PlayerItemPickup] 주변에 픽업 가능한 아이템/오브젝트 없음");
                }
            }
            else
            {
                Debug.Log($"[PlayerItemPickup] 웅크리기 상태 아님 또는 이미 손에 든 것 있음");
            }
        }
    }
    
    // ✅ 유효한 픽업 대상인지 확인 (아이템, 스턴/죽은 적/NPC, 죽은 플레이어, 특정 상황의 플레이어)
    private bool IsValidPickupTarget(GameObject obj)
    {
        if (obj == gameObject) return false;
        if (obj == CurrentHeldObject) return false;

        int layer = obj.layer;
        if (layer == LayerMask.NameToLayer("Item"))
            return true;

        if (layer == LayerMask.NameToLayer("Enemy") || layer == LayerMask.NameToLayer("Npc"))
        {
            // TODO: 스턴 또는 죽음 상태 체크 (예: obj.GetComponent<EnemyStatus>().IsStunned || IsDead)
            return false; // 실제 구현 전까지 false
        }

        if (layer == LayerMask.NameToLayer("Player"))
        {
            // TODO: 죽음 상태 또는 픽업 가능 상태 체크 (예: obj.GetComponent<PlayerStatus>().IsDead || CanBePickedUp)
            return false; // 실제 구현 전까지 false
        }

        // 그 외는 모두 false
        return false;
    }
    
    // 🎭 레이어 마스크 체크
    private bool IsInItemLayer(GameObject obj)
    {
        return (itemLayerMask.value & (1 << obj.layer)) != 0;  // 🎯 비트 연산으로 레이어 확인
    }
    
    // 📡 RPC: InputAuthority → StateAuthority로 픽업 요청
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void PickupObjectRpc(NetworkId objectId)
    {
        Debug.Log($"[PlayerItemPickup] PickupObjectRpc 호출됨: {objectId}");
        var netObj = Runner.FindObject(objectId);
        if (netObj != null)
        {
            Debug.Log($"[PlayerItemPickup] Runner.FindObject 성공: {netObj.name}");
            // 최종 유효성 체크
            if (!IsValidPickupTarget(netObj.gameObject))
            {
                Debug.Log($"[PlayerItemPickup] IsValidPickupTarget 실패: {netObj.name}");
                return;
            }
            Debug.Log($"[PlayerItemPickup] IsValidPickupTarget 통과: {netObj.name}");
            PickupObject(netObj.gameObject);  // 🎯 실제 픽업 처리 (StateAuthority에서만 상태 변경)
        }
        else
        {
            Debug.Log($"[PlayerItemPickup] Runner.FindObject 실패");
        }
    }

    // 📦 실제 오브젝트 픽업 처리 (StateAuthority에서만 의미 있음)
    private void PickupObject(GameObject obj)
    {
        var networkObject = obj.GetComponent<NetworkObject>();

        if (Object.HasStateAuthority)
        {
            Debug.Log($"[PlayerItemPickup] StateAuthority에서 PickupObject 시도");
            // 아이템/캐릭터 구분 없이 무조건 손에 든다
            bool picked = inventory.HoldObject(obj); 
            if (picked)
            {
                Debug.Log($"[PlayerItemPickup] HoldObject 성공: {obj.name}");
                nearbyItems.Remove(obj);  // 🗑️ 주변 목록에서 제거
                obj.transform.SetParent(transform);        // 🏠 Hand의 자식으로 설정
                obj.transform.localPosition = Vector3.zero; // 📍 Hand 중심에 위치
                obj.transform.localRotation = Quaternion.identity; // 🔄 회전 초기화
                
                // 🎮 InputAuthority 할당 (던질 수 있도록)
                if (!networkObject.HasInputAuthority)
                {
                    networkObject.AssignInputAuthority(Object.InputAuthority);
                }
                
                DisableItemPhysics(obj);  // ⚡ 물리 시뮬레이션 비활성화
            }
        }
    }
    
    // 🔍 주변 아이템 중 가장 가까운 것 찾기
    private GameObject FindNearestItem()
    {
        GameObject nearestItem = null;
        float shortestDistance = float.MaxValue;
        
        // 🧹 null 참조 정리 (파괴된 오브젝트 제거)
        nearbyItems.RemoveWhere(item => item == null);
        
        // 📏 거리 계산해서 가장 가까운 아이템 찾기
        foreach (var item in nearbyItems)
        {
            float distance = Vector2.Distance(transform.position, item.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestItem = item;
            }
        }
        
        return nearestItem;  // 🎯 가장 가까운 아이템 반환 (없으면 null)
    }
    
    // ⚡ 아이템의 물리 시뮬레이션 비활성화 (들고 있을 때)
    private void DisableItemPhysics(GameObject item)
    {
        // 🌪️ Rigidbody2D 설정: 외력 영향 안받지만 트리거는 작동
        var rigidbody = item.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            rigidbody.isKinematic = true;      // 🔒 키네마틱 모드 (외력 영향 안받음)
            rigidbody.linearVelocity = Vector2.zero;  // 🛑 속도 0으로 설정
            rigidbody.angularVelocity = 0f;    // 🛑 회전 속도 0으로 설정
            rigidbody.simulated = false;        // ✅ 물리 시뮬레이션 활성화 (트리거 이벤트 위해)
        }
        
        // 🚫 Collider2D 트리거 설정: 충돌 반응 없지만 트리거 감지는 가능
        var collider = item.GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = true;
    }
    
    // 🚪 트리거 진입: 아이템이 감지 범위에 들어왔을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[PlayerItemPickup] OnTriggerEnter2D: {other.gameObject.name}, layer={other.gameObject.layer}");
        if (IsValidPickupTarget(other.gameObject))
        {
            nearbyItems.Add(other.gameObject);
            Debug.Log($"[PlayerItemPickup] 트리거 진입: {other.gameObject.name} 추가됨");
        }
        else
        {
            Debug.Log($"[PlayerItemPickup] 유효하지 않은 대상: {other.gameObject.name}");
        }
    }

    // 🚪 트리거 이탈: 아이템이 감지 범위에서 나갔을 때
    private void OnTriggerExit2D(Collider2D other)
    {
        if (nearbyItems.Contains(other.gameObject))
        {
            nearbyItems.Remove(other.gameObject);
            Debug.Log($"[PlayerItemPickup] 트리거 이탈: {other.gameObject.name} 제거됨");
        }
    }
}