using Fusion;
using UnityEngine;

// 🎒 플레이어 아이템 픽업/드롭 컨트롤러
// 아이템 들기(Space+웅크림), 던지기(우클릭) 담당
public class PlayerItemPickup : NetworkBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 1.5f;         // 아이템 감지 범위
    [SerializeField] private LayerMask itemLayerMask = -1;     // 아이템 레이어 마스크
    [SerializeField] private bool showDebugInfo = true;        // 디버그 정보 표시
    
    [Header("Hand Position")]
    [SerializeField] private Transform handTransform;          // 손 위치 (아이템이 붙을 곳)
    
    // 🎒 현재 들고 있는 아이템 (로컬 변수 - GameObject는 Fusion에서 직접 동기화 불가)
    private GameObject currentItem = null;
    public GameObject CurrentItem => currentItem;
    
    // Fusion 2 공식 패턴: 이전 버튼 상태 추적
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    
    // 📦 컴포넌트 참조들
    private SpelunkyPlayerController playerController;
    
    public override void Spawned()
    {
        // 컴포넌트 찾기
        playerController = GetComponent<SpelunkyPlayerController>();
        
        // 손 위치가 없으면 자식에서 찾기
        if (handTransform == null)
            handTransform = transform.Find("Hand") ?? transform;
        
        Debug.Log($"🎒 PlayerItemPickup 생성 - HasInputAuthority: {Object.HasInputAuthority}");
    }
    
    // 아이템 픽업/드롭 관련 모든 처리를 통합한 메서드 (Fusion 2 공식 패턴)
    public void ProcessInput(SpelunkyPlayerData input)
    {
        // Fusion 2 공식 패턴: GetPressed로 버튼 눌림 감지
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        
        // 이전 상태 업데이트 (공식 패턴)
        ButtonsPrevious = input.NetworkButtons;
        
        // 아이템 픽업 (Space + 웅크린 상태)
        if (pressed.IsSet(SpelunkyInputButtons.PickupItem))
        {
            if (CurrentItem == null)
            {
                Debug.Log("🎒 아이템 픽업 시도 (GetPressed)");
                TryPickupItemRpc();
            }
        }
        
        // 아이템 던지기 (우클릭)
        if (pressed.IsSet(SpelunkyInputButtons.EquipItem))
        {
            if (CurrentItem != null)
            {
                Debug.Log("🎒 아이템 던지기 시도 (GetPressed)");
                ThrowItemRpc(input.MouseWorldPosition);
            }
        }
    }
    
    // 🎒 아이템 픽업 시도 RPC
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void TryPickupItemRpc()
    {
        // 주변 아이템 감지
        Collider2D nearestItem = FindNearestItem();
        
        if (nearestItem != null)
        {
            // 아이템 픽업
            PickupItem(nearestItem.gameObject);
            Debug.Log($"🎒 아이템 픽업 성공: {nearestItem.name}");
        }
        else
        {
            Debug.Log("🎒 주변에 픽업할 아이템이 없음");
        }
    }
    
    // 🎯 던지기 RPC
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void ThrowItemRpc(Vector2 mouseWorldPosition)
    {
        if (CurrentItem == null) return;
        
        // 던지기 방향 계산
        Vector2 throwDirection = (mouseWorldPosition - (Vector2)transform.position).normalized;
        
        // 아이템 던지기
        ThrowItem(throwDirection);
        Debug.Log($"🎒 아이템 던지기: {CurrentItem.name}");
    }
    
    // 🔍 가장 가까운 아이템 찾기
    private Collider2D FindNearestItem()
    {
        Collider2D[] items = Physics2D.OverlapCircleAll(transform.position, pickupRange, itemLayerMask);
        
        Collider2D nearestItem = null;
        float shortestDistance = float.MaxValue;
        
        foreach (var item in items)
        {
            // 자기 자신이 아니고, 아이템 태그를 가진 경우
            if (item.gameObject != gameObject && item.CompareTag("Item"))
            {
                float distance = Vector2.Distance(transform.position, item.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestItem = item;
                }
            }
        }
        
        return nearestItem;
    }
    
    // 📦 아이템 픽업 처리
    private void PickupItem(GameObject item)
    {
        currentItem = item;
        
        // 아이템을 손 위치로 이동
        if (handTransform != null)
        {
            item.transform.SetParent(handTransform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
        }
        
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
        itemToThrow.transform.position = transform.position + (Vector3)direction * 0.5f;
        
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
    
    // 🔍 디버그 정보 표시
    private void OnGUI()
    {
        if (!showDebugInfo || !Object.HasInputAuthority) return;
        
        GUILayout.BeginArea(new Rect(10, 250, 300, 120));
        GUILayout.Box("🎒 아이템 픽업/드롭");
        GUILayout.Label($"현재 아이템: {CurrentItemName}");
        GUILayout.Label($"픽업 범위: {pickupRange}m");
        GUILayout.Label("");
        GUILayout.Label("조작법:");
        GUILayout.Label("Space(웅크린 상태) - 픽업");
        GUILayout.Label("우클릭 - 던지기");
        GUILayout.EndArea();
    }
    
    // 🎯 기즈모 그리기 (픽업 범위)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}