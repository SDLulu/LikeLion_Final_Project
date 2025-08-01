using Fusion;
using UnityEngine;
using System.Collections.Generic;

// 👻 유령 아이템 감지 및 줍기 담당 컴포넌트
// 📍 위치: Ghost 하위 오브젝트 (Ghost > GhostObjectPickup)
// 🎯 목적: CircleCollider2D로 주변 아이템 감지 + 우클릭으로 픽업
public class GhostObjectPickup : NetworkBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private LayerMask pickupLayerMask = -1;      // 🎛️ 인식할 레이어들
    [SerializeField] private CircleCollider2D pickupTrigger;    // 🔵 감지용 원형 트리거
    
    [Header("Position Offset")]
    [SerializeField] private Vector3 ghostHoldOffset = new Vector3(0f, 0.5f, 0f);  // 🎯 유령 들 때 위치 오프셋

    // 🌐 네트워크 동기화 변수들
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }        // 🎮 이전 프레임 버튼 상태 (래칭용)
    
    // 🔍 게임 로직에 필요한 속성 (PlayerInventory에서 가져옴)
    public GameObject CurrentHeldObject => inventory != null ? inventory.CurrentHeldObject : null; // 손에 든 오브젝트
    public bool HasHeldObject => CurrentHeldObject != null; // 손에 든 것 보유 여부
    
    // 🎯 트리거로 감지된 주변 오브젝트들 (로컬에서만 관리, 네트워크 동기화 안됨)
    private HashSet<GameObject> nearbyObjects = new HashSet<GameObject>();
    
    // 📎 참조할 다른 컴포넌트들
    private PlayerInventory inventory;
    
    // 🚀 NetworkBehaviour 생성 시 호출 (모든 클라이언트에서 실행)
    public override void Spawned()
    {
        Transform parentGhost = transform.parent;
        if (parentGhost != null)
        {
            inventory = parentGhost.GetComponentInChildren<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogError($"[{name}] PlayerInventory 컴포넌트를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogError($"[{name}] GhostObjectPickup이 Ghost 오브젝트의 하위가 아닙니다!");
        }
    }
    
    // 🎮 입력 처리 (PlayerGhostController에서 호출)
    public void ProcessInput(GhostInputData input)
    {
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;

        // 🎮 오브젝트 픽업 조건: 우클릭 + 아무것도 안 들고 있을 때
        if (pressed.IsSet(GhostInputButtons.RightClick))
        {
            Debug.Log($"[{name}] 픽업 입력 감지됨");
            if (!HasHeldObject)
            {
                Debug.Log($"[{name}] 손에 든 것 없음");
                // 🔍 가장 가까운 픽업 대상 찾기
                var nearest = FindNearestObject();
                if (nearest != null)
                {
                    Debug.Log($"[{name}] 가장 가까운 아이템/오브젝트: {nearest.name}");
                    var netObj = nearest.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        Debug.Log($"[{name}] NetworkObject 있음, RPC 호출: {netObj.Id}");
                        // 📡 Host(StateAuthority)에게 픽업 요청 RPC 전송
                        PickupObjectRpc(netObj.Id);
                    }
                    else
                    {
                        Debug.Log($"[{name}] NetworkObject 없음");
                    }
                }
                else
                {
                    Debug.Log($"[{name}] 주변에 픽업 가능한 아이템/오브젝트 없음");
                }
            }
            else
            {
                Debug.Log($"[{name}] 이미 손에 든 것 있음");
            }
        }
    }
    
    // 📡 RPC: 픽업 요청
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void PickupObjectRpc(NetworkId objectId)
    {
        var networkObject = Runner.FindObject(objectId);
        if (networkObject != null)
        {
            PickupObject(networkObject.gameObject);
        }
        else
        {
            Debug.LogError($"[{name}] NetworkObject를 찾을 수 없습니다: {objectId}");
        }
    }
    
    // 🤲 실제 픽업 처리
    private void PickupObject(GameObject obj)
    {
        var networkObject = obj.GetComponent<NetworkObject>();

        if (Object.HasStateAuthority)
        {
            Debug.Log($"[{name}] StateAuthority에서 PickupObject 시도");
            
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
            
            // 아이템인 경우 IItemInteraction 체크
            if (obj.layer == LayerMask.NameToLayer("Item"))
            {
                var itemInteraction = obj.GetComponent<IItemInteraction>();
                if (itemInteraction != null)
                {
                    // 아이템의 OnPickedUp 호출
                    itemInteraction.OnPickedUp();
                }
            }
            
            // 아이템/캐릭터 구분 없이 무조건 손에 든다
            bool picked = inventory.HoldObject(obj); 
            if (picked)
            {
                Debug.Log($"[{name}] HoldObject 성공: {obj.name}");
                nearbyObjects.Remove(obj);  // 🗑️ 주변 목록에서 제거
                obj.transform.SetParent(transform);        // 🏠 유령의 자식으로 설정
                
                // 🎯 캐릭터(플레이어/적/NPC)인 경우 오프셋 적용, 아이템은 기본 위치
                Vector3 holdPosition = Vector3.zero;
                int layer = obj.layer;
                if (layer == LayerMask.NameToLayer("Player") || 
                    layer == LayerMask.NameToLayer("Enemy") || 
                    layer == LayerMask.NameToLayer("Npc"))
                {
                    holdPosition = ghostHoldOffset;
                    Debug.Log($"[{name}] 캐릭터 오프셋 적용: {obj.name} (레이어: {layer})");
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
    
    // 🚪 트리거 진입: 오브젝트가 감지 범위에 들어왔을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[{name}] OnTriggerEnter2D: {other.gameObject.name}, layer={other.gameObject.layer}");
        
        // 🎯 아이템인 경우 IItemInteraction 체크
        if (other.gameObject.layer == LayerMask.NameToLayer("Item"))
        {
            var itemInteraction = other.GetComponent<IItemInteraction>();
            if (itemInteraction != null && !itemInteraction.IsHeld)
            {
                nearbyObjects.Add(other.gameObject);
                Debug.Log($"[{name}] 들 수 있는 아이템 감지: {other.gameObject.name}");
            }
            return;
        }
        
        // 플레이어 레이어인 경우 특별 처리
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            var playerInteraction = other.GetComponent<PlayerInteractionBase>();
            if (playerInteraction != null && playerInteraction.IsHoldable) // 스턴 상태인지 확인
            {
                nearbyObjects.Add(other.gameObject);
                Debug.Log($"[{name}] 들 수 있는 플레이어 감지: {other.gameObject.name}");
            }
        }
        // 기타 레이어 처리 (Enemy, Npc 등)
        else if (other.gameObject.layer == LayerMask.NameToLayer("Enemy") || 
                 other.gameObject.layer == LayerMask.NameToLayer("Npc"))
        {
            nearbyObjects.Add(other.gameObject);
            Debug.Log($"[{name}] 들 수 있는 캐릭터 감지: {other.gameObject.name}");
        }
    }
    
    // 🚪 트리거 이탈: 오브젝트가 감지 범위에서 나갔을 때
    private void OnTriggerExit2D(Collider2D other)
    {
        if (nearbyObjects.Contains(other.gameObject))
        {
            nearbyObjects.Remove(other.gameObject);
            Debug.Log($"[{name}] 트리거 이탈: {other.gameObject.name} 제거됨");
        }
    }
} 