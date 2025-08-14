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

        // 🎮 오브젝트 픽업 조건: 우클릭 + 아무것도 안 들고 있을 때
        if (pressed.IsSet(SpelunkyInputButtons.PickupItem))
        {
            Debug.Log($"[PlayerObjectPickup] Pickup 입력 감지됨");
            if (!HasHeldObject)
            {
                Debug.Log($"[PlayerObjectPickup] 손에 든 것 없음, 픽업 시도");
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
                Debug.Log($"[PlayerObjectPickup] 이미 손에 든 것 있음");
            }
        }
    }
    
    // 📡 픽업 RPC (InputAuthority → StateAuthority)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void PickupObjectRpc(NetworkId objectId)
    {
        Debug.Log($"[PlayerObjectPickup] PickupObjectRpc 호출됨: {objectId}");
        
        // NetworkId로 오브젝트 찾기
        if (Runner.TryFindObject(objectId, out var networkObject))
        {
            var obj = networkObject.gameObject;
            Debug.Log($"[PlayerObjectPickup] 오브젝트 찾음: {obj.name}");
            PickupObject(obj);
        }
        else
        {
            Debug.LogWarning($"[PlayerObjectPickup] NetworkId {objectId}에 해당하는 오브젝트를 찾을 수 없습니다!");
        }
    }
    
    // 🎯 실제 픽업 처리 (StateAuthority에서만 실행)
    private void PickupObject(GameObject obj)
    {
        var networkObject = obj.GetComponent<NetworkObject>();

        if (Object.HasStateAuthority)
        {
            Debug.Log($"[PlayerObjectPickup] StateAuthority에서 PickupObject 시도");
            // 1) 캐릭터(IPlayerInteraction) 우선 처리
            var character = obj.GetComponent<IPlayerInteraction>();
            if (character != null)
            {
                if (character.IsHeld)
                {
                    Debug.Log("[PlayerObjectPickup] 이미 들려있는 캐릭터는 픽업 불가");
                    return;
                }
                character.OnPickedUp();
                if (!FinalizePickup(obj, networkObject, isCharacter: true)) return;
                return;
            }

            // 2) 아이템(IItemInteraction)
            var item = obj.GetComponent<IItemInteraction>();
            if (item != null)
            {
                var itemInteraction = obj.GetComponent<IItemInteraction>();
                var shopitem = obj.GetComponent<ShopItem>();
                if(shopitem != null)
                {
                    shopitem.OnPickedUp();

                }
                if (itemInteraction != null)
                if (item.IsHeld)
                {
                    Debug.Log("[PlayerObjectPickup] 이미 들려있는 아이템은 픽업 불가");
                    return;
                }
                item.OnPickedUp();
                if (!FinalizePickup(obj, networkObject, isCharacter: false)) return;
                return;
            }

            Debug.Log("[PlayerObjectPickup] 지원되지 않는 대상");
        }
    }

    // 최종 픽업 공통 마무리: 보유 등록, 부모/위치, 입력권한, 물리전환
    private bool FinalizePickup(GameObject obj, NetworkObject netObj, bool isCharacter)
    {
        bool picked = inventory.HoldObject(obj);
        if (!picked) return false;

        nearbyObjects.Remove(obj);
        obj.transform.SetParent(transform);

        obj.transform.localPosition = isCharacter ? playerHoldOffset : Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        if (!isCharacter)
        {
            if (!netObj.HasInputAuthority)
            {
                netObj.AssignInputAuthority(Object.InputAuthority);
            }
        }

        DisableItemPhysics(obj);
        Debug.Log($"[PlayerObjectPickup] FinalizePickup 완료: {obj.name} (isCharacter:{isCharacter})");
        return true;
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
        // 🎯 아이템인 경우 IItemInteraction 체크
        if (other.gameObject.layer == LayerMask.NameToLayer("Item"))
        {
            var itemInteraction = other.GetComponent<IItemInteraction>();
            if (itemInteraction != null && !itemInteraction.IsHeld)
            {
                nearbyObjects.Add(other.gameObject);
                Debug.Log($"[PlayerObjectPickup] 들 수 있는 아이템 감지: {other.gameObject.name}");
            }
            return;
        }
        
        // 플레이어/적/NPC 레이어: 인터페이스 + IsHeld 체크로 통일
        if (other.gameObject.layer == LayerMask.NameToLayer("Player") ||
            other.gameObject.layer == LayerMask.NameToLayer("Enemy") ||
            other.gameObject.layer == LayerMask.NameToLayer("Npc"))
        {
            var p = other.GetComponent<IPlayerInteraction>();
            if (p != null && !p.IsHeld)
            {
                nearbyObjects.Add(other.gameObject);
                Debug.Log($"[PlayerObjectPickup] 들 수 있는 대상 감지: {other.gameObject.name} (레이어:{other.gameObject.layer})");
            }
            return;
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