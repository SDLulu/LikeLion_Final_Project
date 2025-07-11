using Fusion;
using UnityEngine;
using System.Collections.Generic;

// 🎒 플레이어 아이템 픽업/드롭 컨트롤러
// Hand 하위 오브젝트에 위치하며, Trigger 방식으로 아이템 감지
// 아이템 들기(Space+웅크림), 던지기(우클릭) 담당
public class PlayerItemPickup : NetworkBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private LayerMask itemLayerMask = -1;     // 아이템 레이어 마스크
    [SerializeField] private bool showDebugInfo = true;        // 디버그 정보 표시
    
    [Header("Required Components")]
    [SerializeField] private CircleCollider2D pickupTrigger;   // 감지용 트리거 (Hand에 위치)
    
    // 🎒 현재 들고 있는 아이템
    private GameObject currentItem = null;
    public GameObject CurrentItem => currentItem;
    
    // 📋 감지된 아이템 목록 (Trigger 방식)
    private HashSet<GameObject> nearbyItems = new HashSet<GameObject>();
    
    // Fusion 2 공식 패턴: 이전 버튼 상태 추적
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    
    // 📦 부모 컴포넌트 참조들 (Player 오브젝트에서)
    private SpelunkyPlayerController playerController;
    private PlayerMovement playerMovement;
    
    public override void Spawned()
    {
        // 부모(Player)에서 컴포넌트들 찾기
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
        
        // 트리거 컴포넌트 설정
        SetupTriggerCollider();
        
        Debug.Log($"🎒 PlayerItemPickup 생성 (Hand) - HasInputAuthority: {Object.HasInputAuthority}");
    }
    
    // 🔧 트리거 콜라이더 자동 설정
    private void SetupTriggerCollider()
    {
        if (pickupTrigger == null)
        {
            pickupTrigger = GetComponent<CircleCollider2D>();
            
            // 트리거가 없으면 자동 생성
            if (pickupTrigger == null)
            {
                pickupTrigger = gameObject.AddComponent<CircleCollider2D>();
                pickupTrigger.radius = 1.5f; // 기본 감지 범위
                Debug.Log("🔧 Hand에 픽업용 CircleCollider2D 자동 생성");
            }
        }
        
        // 트리거 설정 강제 적용
        pickupTrigger.isTrigger = true;
        Debug.Log($"🎯 픽업 감지 범위: {pickupTrigger.radius}");
    }
    
    // 아이템 픽업/드롭 관련 모든 처리를 통합한 메서드
    public void ProcessInput(SpelunkyPlayerData input)
    {
        // Fusion 2 공식 패턴: GetPressed로 버튼 눌림 감지
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        
        // 이전 상태 업데이트 (공식 패턴)
        ButtonsPrevious = input.NetworkButtons;
        
        // 아이템 픽업 (Space + 웅크린 상태)
        if (pressed.IsSet(SpelunkyInputButtons.PickupItem))
        {
            if (CurrentItem == null && playerMovement != null && playerMovement.IsDucking)
            {
                Debug.Log("🎒 아이템 픽업 시도 (Trigger)");
                TryPickupItemRpc();
            }
        }
        
        // 아이템 던지기 (우클릭)
        if (pressed.IsSet(SpelunkyInputButtons.EquipItem))
        {
            if (CurrentItem != null)
            {
                Debug.Log("🎒 아이템 던지기 시도");
                ThrowItemRpc(input.MouseWorldPosition);
            }
        }
    }
    
    // 🎯 트리거 진입 (아이템 감지)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsValidItem(other.gameObject))
        {
            nearbyItems.Add(other.gameObject);
            Debug.Log($"🎯 아이템 감지: {other.name} (총 {nearbyItems.Count}개)");
        }
    }
    
    // 🚪 트리거 이탈 (아이템 감지 해제)
    private void OnTriggerExit2D(Collider2D other)
    {
        if (nearbyItems.Contains(other.gameObject))
        {
            nearbyItems.Remove(other.gameObject);
            Debug.Log($"🚪 아이템 감지 해제: {other.name} (남은 {nearbyItems.Count}개)");
        }
    }
    
    // 🔍 유효한 아이템인지 확인
    private bool IsValidItem(GameObject obj)
    {
        // 자기 자신이 아니고, 아이템 레이어에 있고, 현재 들고 있지 않은 경우
        return obj != gameObject && 
               obj != CurrentItem && 
               IsInItemLayer(obj);
    }
    
    // 🔍 아이템 레이어 체크
    private bool IsInItemLayer(GameObject obj)
    {
        return (itemLayerMask.value & (1 << obj.layer)) != 0;
    }
    
    // 🎒 아이템 픽업 시도 RPC
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void TryPickupItemRpc()
    {
        // 가장 가까운 아이템 찾기
        GameObject nearestItem = FindNearestItem();
        
        if (nearestItem != null)
        {
            PickupItem(nearestItem);
            Debug.Log($"🎒 아이템 픽업 성공: {nearestItem.name}");
        }
        else
        {
            Debug.Log("🎒 범위 내에 픽업할 아이템이 없음");
        }
    }
    
    // 🔍 가장 가까운 아이템 찾기 (Trigger 기반)
    private GameObject FindNearestItem()
    {
        GameObject nearestItem = null;
        float shortestDistance = float.MaxValue;
        
        // null 아이템들 정리
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
    
    // 🎯 던지기 RPC
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void ThrowItemRpc(Vector2 mouseWorldPosition)
    {
        if (CurrentItem == null) return;
        
        // 던지기 방향 계산 (부모 위치 기준)
        Vector2 playerPosition = transform.parent != null ? transform.parent.position : transform.position;
        Vector2 throwDirection = (mouseWorldPosition - playerPosition).normalized;
        
        // 아이템 던지기
        ThrowItem(throwDirection);
        Debug.Log($"🎒 아이템 던지기: {CurrentItem.name}");
    }
    
    // 📦 아이템 픽업 처리
    private void PickupItem(GameObject item)
    {
        currentItem = item;
        
        // 감지 목록에서 제거
        nearbyItems.Remove(item);
        
        // 아이템을 Hand 위치로 이동
        item.transform.SetParent(transform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        
        // 아이템의 물리 비활성화 (들고 있는 동안)
        var rigidbody = item.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
            rigidbody.isKinematic = true;
        
        var collider = item.GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;
    }
    
    // 🎯 아이템 던지기 처리
    private void ThrowItem(Vector2 direction)
    {
        if (CurrentItem == null) return;
        
        GameObject itemToThrow = currentItem;
        currentItem = null;
        
        // 부모 해제
        itemToThrow.transform.SetParent(null);
        
        // 던지기 위치 설정 (플레이어 앞쪽)
        Vector2 playerPosition = transform.parent != null ? transform.parent.position : transform.position;
        itemToThrow.transform.position = playerPosition + direction * 0.5f;
        
        // 물리 활성화
        var rigidbody = itemToThrow.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            rigidbody.isKinematic = false;
            rigidbody.linearVelocity = direction * 10f; // 던지기 힘
        }
        
        var collider = itemToThrow.GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = true;
    }
    
    // 📊 상태 확인 프로퍼티들
    public bool HasItem => CurrentItem != null;
    public string CurrentItemName => CurrentItem?.name ?? "없음";
    public int NearbyItemsCount => nearbyItems.Count;
    
    // 🔍 디버그 정보 표시
    private void OnGUI()
    {
        if (!showDebugInfo || !Object.HasInputAuthority) return;
        
        GUILayout.BeginArea(new Rect(10, 250, 300, 140));
        GUILayout.Box("🎒 아이템 픽업/드롭 (Trigger)");
        GUILayout.Label($"현재 아이템: {CurrentItemName}");
        GUILayout.Label($"감지된 아이템: {NearbyItemsCount}개");
        GUILayout.Label($"감지 범위: {pickupTrigger?.radius:F1}");
        GUILayout.Label("");
        GUILayout.Label("조작법:");
        GUILayout.Label("Space(웅크린 상태) - 픽업");
        GUILayout.Label("우클릭 - 던지기");
        GUILayout.EndArea();
    }
}