using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// 아이템 감지 및 줍기 담당 컴포넌트
// Hand 하위 오브젝트에 위치하며, Trigger 방식으로 아이템 감지
public class PlayerItemPickup : NetworkBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private LayerMask itemLayerMask = -1;     // 아이템 레이어 마스크
    [SerializeField] private CircleCollider2D pickupTrigger;   // 감지용 트리거 (Hand에 위치)
    [SerializeField] private float defaultTriggerRadius = 1.5f; // 기본 감지 범위
    
    // 🌐 네트워크 동기화 상태
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    [Networked] private NetworkBool HasItemNetworked { get; set; }
    [Networked] private NetworkId CurrentItemNetworkId { get; set; }
    
    // 아이템 상태 (로컬 캐시)
    private GameObject currentItem;
    public GameObject CurrentItem 
    { 
        get => currentItem;
        private set
        {
            currentItem = value;
            // StateAuthority(권한자)에서만 네트워크 동기화 변수 갱신
            // 클라/서버 모두 StateAuthority가 아니면 이 블록은 실행되지 않음
            if (Object.HasStateAuthority)
            {
                HasItemNetworked = value != null;
                CurrentItemNetworkId = value?.GetComponent<NetworkObject>()?.Id ?? default;
            }
        }
    }
    public bool HasItem => HasItemNetworked;
    public string CurrentItemName => CurrentItem?.name ?? "없음";
    public int NearbyItemsCount => nearbyItems.Count;
    
    // 감지된 아이템 목록 (Trigger 방식, 로컬에서만 관리)
    private HashSet<GameObject> nearbyItems = new HashSet<GameObject>();
    
    // 참조 컴포넌트들
    private SpelunkyPlayerController playerController;
    private PlayerMovement playerMovement;
    
    public override void Spawned()
    {
        SetupReferences();
        SetupTriggerCollider();

        // [서버/클라 공통] 네트워크 오브젝트 ID로 아이템 찾기
        // (늦게 참가한 플레이어나 재접속 시 동기화 복원)
        if (CurrentItemNetworkId != default)
        {
            var networkObject = Runner.FindObject(CurrentItemNetworkId);
            if (networkObject != null)
            {
                currentItem = networkObject.gameObject;
            }
        }
    }
    
    private void SetupReferences()
    {
        // [로컬] 부모 Player 오브젝트에서 컨트롤러 참조
        Transform parentPlayer = transform.parent;
        if (parentPlayer != null)
        {
            playerController = parentPlayer.GetComponent<SpelunkyPlayerController>();
            playerMovement = parentPlayer.GetComponent<PlayerMovement>();
        }
        else
        {
            Debug.LogWarning($"[{name}] PlayerItemPickup이 Player 오브젝트의 하위가 아닙니다!");
        }
    }
    
    private void SetupTriggerCollider()
    {
        // [로컬] 감지용 트리거 콜라이더 설정
        if (pickupTrigger == null)
        {
            pickupTrigger = GetComponent<CircleCollider2D>();
            
            if (pickupTrigger == null)
            {
                pickupTrigger = gameObject.AddComponent<CircleCollider2D>();
                pickupTrigger.radius = defaultTriggerRadius;
            }
        }
        
        pickupTrigger.isTrigger = true;
    }
    
    public void ProcessInput(SpelunkyPlayerData input)
    {
        // [로컬] 입력 처리: 네트워크 버튼 래칭
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;
        
        // 아이템 픽업 (Space + 웅크린 상태)
        if (pressed.IsSet(SpelunkyInputButtons.PickupItem))
        {
            if (!HasItem && playerMovement != null && playerMovement.IsDucking)
            {
                // [로컬] TryPickupItemRpc() 호출 → 네트워크 전체에 픽업 시도 알림
                TryPickupItemRpc();
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"OnTriggerEnter2D: {other.gameObject.name}");
        // [로컬] 트리거 진입 시 아이템 감지 목록에 추가
        if (IsValidItem(other.gameObject))
        {
            nearbyItems.Add(other.gameObject);
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"OnTriggerExit2D: {other.gameObject.name}");
        // [로컬] 트리거 이탈 시 아이템 감지 목록에서 제거
        if (nearbyItems.Contains(other.gameObject))
        {
            nearbyItems.Remove(other.gameObject);
        }
    }
    
    private bool IsValidItem(GameObject obj)
    {
        // [로컬] 자기 자신/이미 들고 있는 아이템/레이어 체크
        return obj != gameObject && 
               obj != CurrentItem && 
               IsInItemLayer(obj);
    }
    
    private bool IsInItemLayer(GameObject obj)
    {
        // [로컬] 레이어 마스크 체크
        return (itemLayerMask.value & (1 << obj.layer)) != 0;
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void TryPickupItemRpc()
    {
        GameObject nearestItem = FindNearestItem();
        if (nearestItem != null)
        {
            // StateAuthority 획득 후에만 아이템 조작
            StartCoroutine(PickupItemWithAuthority(nearestItem));
        }
    }

    private IEnumerator PickupItemWithAuthority(GameObject item)
    {
        var networkObject = item.GetComponent<NetworkObject>();
        if (networkObject == null) yield break;

        // 권한 요청
        if (!networkObject.HasStateAuthority && !Runner.IsServer)
            networkObject.RequestStateAuthority();

        // 권한이 넘어올 때까지 대기 (최대 1초)
        float timeout = 1f;
        while (!networkObject.HasStateAuthority && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (networkObject.HasStateAuthority)
        {
            // StateAuthority에서만 아이템 조작
            CurrentItem = item;
            nearbyItems.Remove(item);
            item.transform.SetParent(transform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            DisableItemPhysics(item);
            Debug.Log("StateAuthority 획득 후 아이템 조작 완료");
        }
        else
        {
            Debug.LogWarning("StateAuthority 획득 실패");
        }
    }
    
    private GameObject FindNearestItem()
    {
        // [로컬] 감지된 아이템 중 가장 가까운 것 반환
        GameObject nearestItem = null;
        float shortestDistance = float.MaxValue;
        
        nearbyItems.RemoveWhere(item => item == null);
        
        foreach (var item in nearbyItems)
        {
            float distance = Vector2.Distance(transform.position, item.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestItem = item;
            }
        }
        
        return nearestItem;
    }
    
    private void PickupItem(GameObject item)
    {
        Debug.Log("PickupItem 호출됨: " + item?.name);
        // [StateAuthority에서만 의미 있음] 실제 아이템 소유권 이전 및 상태 갱신
        if (!item.TryGetComponent<NetworkObject>(out var networkObject))
        {
            Debug.LogError($"아이템 {item.name}에 NetworkObject가 없습니다!");
            return;
        }

        // [클라] StateAuthority가 아니면 권한 요청 (서버는 이미 권한자)
        if (!networkObject.HasStateAuthority && !Runner.IsServer)
        {
            networkObject.RequestStateAuthority();
        }

        // [StateAuthority에서만] 아이템 상태 갱신 및 transform 조작
        if (networkObject.HasStateAuthority)
        {
            CurrentItem = item;
            nearbyItems.Remove(item);
            // 아이템을 Hand 위치로 이동
            item.transform.SetParent(transform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            DisableItemPhysics(item);
        }
    }
    
    private void DisableItemPhysics(GameObject item)
    {
        // [StateAuthority에서만] 아이템의 물리 비활성화
        var rigidbody = item.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            rigidbody.isKinematic = true;
            rigidbody.linearVelocity = Vector2.zero;
            rigidbody.angularVelocity = 0f;
            rigidbody.simulated = false;
        }
        
        var collider = item.GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;
    }
    
    // PlayerItemThrower에서 호출
    public void ClearItem()
    {
        // [StateAuthority에서만] 아이템 참조 해제 및 네트워크 동기화
        if (Object.HasStateAuthority)
        {
            CurrentItem = null;
        }
    }
}