using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// 아이템 감지 및 줍기 담당 컴포넌트
// 📍 위치: Hand 하위 오브젝트 (Player > Hand > PlayerItemPickup)
// 🎯 목적: CircleCollider2D로 주변 아이템 감지 + Space+웅크리기로 픽업
public class PlayerObjectPickup : NetworkBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private LayerMask pickupLayerMask = -1;      // 🎛️ 인식할 레이어들
    [SerializeField] private CircleCollider2D pickupTrigger;    // 🔵 감지용 원형 트리거 (Hand에 위치)
    
    [Header("Position Offset")]
    [SerializeField] private Vector3 playerHoldOffset = new Vector3(0f, 0.5f, 0f);  // 🎯 플레이어 들 때 위치 오프셋

    
    // 🌐 네트워크 동기화 변수들 (모든 클라이언트가 동일한 값을 가짐)
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }        // 🎮 이전 프레임 버튼 상태 (래칭용)
    
    // 🔍 게임 로직에 필요한 속성 (PlayerInventory에서 가져옴)
    public GameObject CurrentHeldObject => inventory != null ? inventory.CurrentHeldObject : null; // 손에 든 오브젝트
    public bool HasHeldObject => CurrentHeldObject != null; // 손에 든 것 보유 여부
    
    // 🎯 트리거로 감지된 주변 오브젝트들 (로컬에서만 관리, 네트워크 동기화 안됨)
    private HashSet<GameObject> nearbyObjects = new HashSet<GameObject>();
    
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
            inventory = parentPlayer.GetComponentInChildren<PlayerInventory>(); // Player 오브젝트의 자식들 중에서 찾기
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
            Debug.LogError($"[{name}] PlayerObjectPickup이 Player 오브젝트의 하위가 아닙니다!");
        }
    }
    
    
    // 🎮 입력 처리 (SpelunkyPlayerController에서 호출)
    // 👉 InputAuthority(로컬 플레이어)에서만 호출됨
    public void ProcessInput(SpelunkyPlayerInputData input)
    {
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;

        // 🎮 오브젝트 픽업 조건: Space키 + 웅크린 상태 + 아무것도 안 들고 있을 때
        if (pressed.IsSet(SpelunkyInputButtons.PickupItem))
        {
            Debug.Log($"[PlayerObjectPickup] Pickup 입력 감지됨");
            if (!HasHeldObject && playerMovement != null && playerMovement.IsDucking && !playerMovement.IsLookingUp)
            {
                Debug.Log($"[PlayerObjectPickup] 웅크리기 상태, 손에 든 것 없음");
                // 🔍 가장 가까운 픽업 대상 찾기
                var nearest = FindNearestObject();
                if (nearest != null)
                {
                    Debug.Log($"[PlayerObjectPickup] 가장 가까운 아이템/오브젝트: {nearest.name}");
                    var netObj = nearest.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        Debug.Log($"[PlayerObjectPickup] NetworkObject 있음, RPC 호출: {netObj.Id}");
                        // 📡 Host(StateAuthority)에게 픽업 요청 RPC 전송
                        PickupObjectRpc(netObj.Id);
                    }
                    else
                    {
                        Debug.Log($"[PlayerObjectPickup] NetworkObject 없음");
                    }
                }
                else
                {
                    Debug.Log($"[PlayerObjectPickup] 주변에 픽업 가능한 아이템/오브젝트 없음");
                }
            }
            else
            {
                Debug.Log($"[PlayerObjectPickup] 웅크리기 상태 아님 또는 이미 손에 든 것 있음");
            }
        }
    }
    
    
    // 📡 RPC: InputAuthority → StateAuthority로 픽업 요청
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void PickupObjectRpc(NetworkId objectId)
    {
        Debug.Log($"[PlayerObjectPickup] PickupObjectRpc 호출됨: {objectId}");
        var netObj = Runner.FindObject(objectId);
        if (netObj != null)
        {
            Debug.Log($"[PlayerObjectPickup] Runner.FindObject 성공: {netObj.name}");
            PickupObject(netObj.gameObject);
        }
        else
        {
            Debug.Log($"[PlayerObjectPickup] Runner.FindObject 실패");
        }
    }

    // 📦 실제 오브젝트 픽업 처리 (StateAuthority에서만 의미 있음)
    private void PickupObject(GameObject obj)
    {
        var networkObject = obj.GetComponent<NetworkObject>();

        if (Object.HasStateAuthority)
        {
            Debug.Log($"[PlayerObjectPickup] StateAuthority에서 PickupObject 시도");
            
            // 플레이어인 경우 특별 처리
            if (obj.layer == LayerMask.NameToLayer("Player"))
            {
                var playerInteraction = obj.GetComponent<PlayerInteractionBase>();
                if (playerInteraction != null)
                {
                    // 들린 플레이어의 상태 설정
                    playerInteraction.OnPickedUp();
                }
            }
            
            // 아이템/캐릭터 구분 없이 무조건 손에 든다
            bool picked = inventory.HoldObject(obj); 
            if (picked)
            {
                Debug.Log($"[PlayerObjectPickup] HoldObject 성공: {obj.name}");
                nearbyObjects.Remove(obj);  // 🗑️ 주변 목록에서 제거
                obj.transform.SetParent(transform);        // 🏠 Hand의 자식으로 설정
                
                // 🎯 캐릭터(플레이어/적/NPC)인 경우 오프셋 적용, 아이템은 기본 위치
                Vector3 holdPosition = Vector3.zero;
                int layer = obj.layer;
                if (layer == LayerMask.NameToLayer("Player") || 
                    layer == LayerMask.NameToLayer("Enemy") || 
                    layer == LayerMask.NameToLayer("Npc"))
                {
                    holdPosition = playerHoldOffset;
                    Debug.Log($"[PlayerObjectPickup] 캐릭터 오프셋 적용: {obj.name} (레이어: {layer})");
                }
                
                obj.transform.localPosition = holdPosition; // 📍 위치 설정
                obj.transform.localRotation = Quaternion.identity; // 🔄 회전 초기화
                
                // 🎮 InputAuthority 할당 (던질 수 있도록 )
                if (layer != LayerMask.NameToLayer("Player") && layer != LayerMask.NameToLayer("Enemy") && layer != LayerMask.NameToLayer("Npc"))
                {
                    if (!networkObject.HasInputAuthority)
                    {
                        networkObject.AssignInputAuthority(Object.InputAuthority);
                    }
                }
                
                // ️ 아이템에 Held 태그 추가 (픽업 대상에서 제외)
                if (layer == LayerMask.NameToLayer("Item"))
                {
                    obj.tag = "Held";
                    Debug.Log($"[PlayerObjectPickup] 아이템에 Held 태그 추가: {obj.name}");
                }
                
                DisableItemPhysics(obj);  // ⚡ 물리 시뮬레이션 비활성화
            }
        }
    }
    
    // 🔍 주변 오브젝트 중 가장 가까운 것 찾기
    private GameObject FindNearestObject()
    {
        GameObject nearestObject = null;
        float shortestDistance = float.MaxValue;
        
        // 🧹 null 참조 정리 (파괴된 오브젝트 제거)
        nearbyObjects.RemoveWhere(obj => obj == null);
        
        // 📏 거리 계산해서 가장 가까운 오브젝트 찾기
        foreach (var obj in nearbyObjects)
        {
            float distance = Vector2.Distance(transform.position, obj.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestObject = obj;
            }
        }
        
        return nearestObject;  // 🎯 가장 가까운 오브젝트 반환 (없으면 null)
    }
    
    // ⚡ 아이템의 물리 시뮬레이션 비활성화 (들고 있을 때)
    private void DisableItemPhysics(GameObject item)
    {
        // 🌪️ Rigidbody2D 설정: 외력 영향 안받지만 트리거는 작동
        var rigidbody = item.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            rigidbody.bodyType = RigidbodyType2D.Kinematic;      // 🔒 키네마틱 모드 (외력 영향 안받음)
            rigidbody.linearVelocity = Vector2.zero;  // 🛑 속도 0으로 설정
            rigidbody.angularVelocity = 0f;    // 🛑 회전 속도 0으로 설정
            rigidbody.simulated = true;        // ✅ 물리 시뮬레이션 활성화 (트리거 이벤트 위해)
            rigidbody.gravityScale = 0f;       // 🛑 중력 영향 제거
        }
        
        // 🚫 Collider2D 트리거 설정: 충돌 반응 없지만 트리거 감지는 가능
        var collider = item.GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = true;
    }
    
    // 🚪 트리거 진입: 오브젝트가가 감지 범위에 들어왔을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[PlayerObjectPickup] OnTriggerEnter2D: {other.gameObject.name}, layer={other.gameObject.layer}");
        
        // 🏷️ Held 태그가 있으면 픽업 대상에서 제외
        if (other.CompareTag("Held"))
        {
            Debug.Log($"[PlayerObjectPickup] Held 태그가 있어서 픽업 대상에서 제외: {other.gameObject.name}");
            return;
        }
        
        // 플레이어 레이어인 경우 특별 처리
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            var playerInteraction = other.GetComponent<PlayerInteractionBase>();
            if (playerInteraction != null && playerInteraction.IsHoldable) // 스턴 상태인지 확인
            {
                nearbyObjects.Add(other.gameObject);
                Debug.Log($"[PlayerObjectPickup] 들 수 있는 플레이어 감지: {other.gameObject.name}");
            }
        }
        // 기존 아이템 처리
        else if ((pickupLayerMask.value & (1 << other.gameObject.layer)) != 0)
        {
            nearbyObjects.Add(other.gameObject);
            Debug.Log($"[PlayerObjectPickup] pickupLayerMask에 포함된 레이어: {other.gameObject.layer}");
        }
    }
    
    // 🚪 트리거 이탈: 오브젝트가가 감지 범위에서 나갔을 때
    private void OnTriggerExit2D(Collider2D other)
    {
        if (nearbyObjects.Contains(other.gameObject))
        {
            nearbyObjects.Remove(other.gameObject);
            Debug.Log($"[PlayerObjectPickup] 트리거 이탈: {other.gameObject.name} 제거됨");
        }
    }
}