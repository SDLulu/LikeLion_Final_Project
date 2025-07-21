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
    [Networked] private NetworkObject CurrentItemNetworkObject { get; set; } // 📦 현재 아이템의 NetworkObject (직접 동기화)
    
    // 🎯 간단한 CurrentItem 프로퍼티 - NetworkObject에서 GameObject 반환
    public GameObject CurrentItem 
    { 
        get => CurrentItemNetworkObject?.gameObject;
        private set
        {
            // ⚡ StateAuthority(보통 Host)에서만 네트워크 변수 업데이트
            // 👉 이렇게 하면 Host가 변경하고 → 모든 클라이언트에게 자동 동기화됨
            if (Object.HasStateAuthority)
            {
                CurrentItemNetworkObject = value?.GetComponent<NetworkObject>();
            }
        }
    }
    
    // 🔍 게임 로직에 필요한 속성
    public bool HasItem => CurrentItem != null;          // 📦 현재 아이템 보유 여부
    
    // 🎯 트리거로 감지된 주변 아이템들 (로컬에서만 관리, 네트워크 동기화 안됨)
    private HashSet<GameObject> nearbyItems = new HashSet<GameObject>();
    
    // 📎 참조할 다른 컴포넌트들
    private SpelunkyPlayerController playerController;
    private PlayerMovement playerMovement;
    
    // 🚀 NetworkBehaviour 생성 시 호출 (모든 클라이언트에서 실행)
    public override void Spawned()
    {
        SetupReferences();     // 📎 다른 컴포넌트 참조 설정
        SetupTriggerCollider(); // 🔵 트리거 콜라이더 설정
        
        // 🌐 CurrentItem getter가 이제 네트워크 ID로부터 실시간으로 찾기 때문에
        // 👉 별도의 아이템 복원 코드가 불필요함
    }
    
    // 📎 부모 Player에서 필요한 컴포넌트들 찾기
    private void SetupReferences()
    {
        Transform parentPlayer = transform.parent;  // 🏠 부모 = Player 오브젝트
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
        // 🎯 버튼 래칭: 이전 프레임과 비교해서 새로 눌린 버튼만 감지
        // 👉 이렇게 하면 "한 번 클릭"이 여러 프레임에 걸쳐 씹히지 않음
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;  // 🔄 다음 프레임을 위해 현재 상태 저장
        
        // 🎮 아이템 픽업 조건: Space키 + 웅크린 상태 + 아이템 없음
        if (pressed.IsSet(SpelunkyInputButtons.PickupItem))
        {
            if (!HasItem && playerMovement != null && playerMovement.IsDucking)
            {
                // 🔍 가장 가까운 아이템 찾기
                var nearest = FindNearestItem();
                if (nearest != null)
                {
                    var netObj = nearest.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        // 📡 Host(StateAuthority)에게 픽업 요청 RPC 전송
                        // 👉 InputAuthority → StateAuthority로 요청
                        PickupItemRpc(netObj.Id);
                    }
                }
            }
        }
    }
    
    // 🚪 트리거 진입: 아이템이 감지 범위에 들어왔을 때
    // 👉 물리 시스템에서 자동 호출, 모든 클라이언트에서 개별 실행
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsValidItem(other.gameObject))
        {
            nearbyItems.Add(other.gameObject);  // 📝 주변 아이템 목록에 추가
        }
    }
    
    // 🚪 트리거 이탈: 아이템이 감지 범위에서 나갔을 때
    private void OnTriggerExit2D(Collider2D other)
    {
        if (nearbyItems.Contains(other.gameObject))
        {
            nearbyItems.Remove(other.gameObject);  // 🗑️ 주변 아이템 목록에서 제거
        }
    }
    
    // ✅ 유효한 아이템인지 확인
    private bool IsValidItem(GameObject obj)
    {
        return obj != gameObject &&        // 🚫 자기 자신 제외
               obj != CurrentItem &&       // 🚫 이미 들고 있는 아이템 제외
               IsInItemLayer(obj);         // ✅ 아이템 레이어인지 확인
    }
    
    // 🎭 레이어 마스크 체크
    private bool IsInItemLayer(GameObject obj)
    {
        return (itemLayerMask.value & (1 << obj.layer)) != 0;  // 🎯 비트 연산으로 레이어 확인
    }
    
    // 📡 RPC: InputAuthority → StateAuthority로 픽업 요청
    // 👉 로컬 플레이어가 호출 → Host가 받아서 처리
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void PickupItemRpc(NetworkId itemId)
    {
        // 🔍 네트워크 ID로 아이템 오브젝트 찾기
        var netObj = Runner.FindObject(itemId);
        if (netObj != null)
        {
            PickupItem(netObj.gameObject);  // 🎯 실제 픽업 처리 (StateAuthority에서만 상태 변경)
        }
    }
    
    // 📦 실제 아이템 픽업 처리 (StateAuthority에서만 의미 있음)
    private void PickupItem(GameObject item)
    {
        var networkObject = item.GetComponent<NetworkObject>();
        if (networkObject == null) return;
        
        // ⚡ StateAuthority(보통 Host)에서만 아이템 상태 변경
        // 👉 이렇게 하면 Host가 변경하고 → 모든 클라이언트에게 자동 동기화
        if (Object.HasStateAuthority)
        {
            CurrentItem = item;  // 📦 아이템 소유권 설정 (네트워크 변수도 자동 업데이트)
            nearbyItems.Remove(item);  // 🗑️ 주변 목록에서 제거
            
            // 🎯 아이템을 Hand 위치로 이동시키기
            item.transform.SetParent(transform);        // 🏠 Hand의 자식으로 설정
            item.transform.localPosition = Vector3.zero; // 📍 Hand 중심에 위치
            item.transform.localRotation = Quaternion.identity; // 🔄 회전 초기화
            
            // 🔑 중요: 아이템의 InputAuthority를 현재 플레이어에게 전송
            // 👉 이래야 플레이어가 아이템의 RPC를 호출할 수 있음
            if (networkObject.HasInputAuthority == false)
            {
                networkObject.AssignInputAuthority(Object.InputAuthority);
            }
            
            DisableItemPhysics(item);  // ⚡ 물리 시뮬레이션 비활성화
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
        // 🌪️ Rigidbody2D 설정: 물리 법칙 적용 안함
        var rigidbody = item.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            rigidbody.isKinematic = true;      // 🔒 키네마틱 모드 (외력 영향 안받음)
            rigidbody.linearVelocity = Vector2.zero;  // 🛑 속도 0으로 설정
            rigidbody.angularVelocity = 0f;    // 🛑 회전 속도 0으로 설정
            rigidbody.simulated = false;       // ⏸️ 물리 시뮬레이션 완전 중단
        }
        
        // 🚫 Collider2D 비활성화: 다른 오브젝트와 충돌 안함
        var collider = item.GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = true;
    }
    
    // 🗑️ 아이템 참조 해제 (PlayerItemThrower에서 호출)
    public void ClearItem()
    {
        // ⚡ StateAuthority에서만 네트워크 변수 변경
        if (Object.HasStateAuthority)
        {
            // 🔑 아이템의 InputAuthority도 제거 (다른 플레이어가 주울 수 있도록)
            if (CurrentItem != null)
            {
                var networkObject = CurrentItem.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.HasInputAuthority)
                {
                    networkObject.RemoveInputAuthority();
                }
            }
            
            CurrentItem = null;  // 📦 아이템 참조 해제 (네트워크 변수도 자동 업데이트)
        }
    }
}