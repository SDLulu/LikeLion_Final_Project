using Fusion;
using UnityEngine;
using System.Collections.Generic;

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
    
    // 아이템 상태
    public GameObject CurrentItem { get; private set; }
    public bool HasItem { get; private set; }
    public string CurrentItemName { get { return CurrentItem?.name ?? "없음"; } }
    public int NearbyItemsCount { get { return nearbyItems.Count; } }
    
    // 감지된 아이템 목록 (Trigger 방식)
    private HashSet<GameObject> nearbyItems = new HashSet<GameObject>();
    
    // 참조 컴포넌트들
    private SpelunkyPlayerController playerController;
    private PlayerMovement playerMovement;
    
    public override void Spawned()
    {
        SetupReferences();
        SetupTriggerCollider();
    }
    
    private void SetupReferences()
    {
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
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;
        
        // 아이템 픽업 (Space + 웅크린 상태)
        if (pressed.IsSet(SpelunkyInputButtons.PickupItem))
        {
            if (!HasItem && playerMovement != null && playerMovement.IsDucking)
            {
                TryPickupItemRpc();
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsValidItem(other.gameObject))
        {
            nearbyItems.Add(other.gameObject);
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (nearbyItems.Contains(other.gameObject))
        {
            nearbyItems.Remove(other.gameObject);
        }
    }
    
    private bool IsValidItem(GameObject obj)
    {
        return obj != gameObject && 
               obj != CurrentItem && 
               IsInItemLayer(obj);
    }
    
    private bool IsInItemLayer(GameObject obj)
    {
        return (itemLayerMask.value & (1 << obj.layer)) != 0;
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void TryPickupItemRpc()
    {
        GameObject nearestItem = FindNearestItem();
        
        if (nearestItem != null)
        {
            PickupItem(nearestItem);
        }
    }
    
    private GameObject FindNearestItem()
    {
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
        CurrentItem = item;
        HasItem = true;
        
        nearbyItems.Remove(item);
        
        // 아이템을 Hand 위치로 이동
        item.transform.SetParent(transform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        
        DisableItemPhysics(item);
    }
    
    private void DisableItemPhysics(GameObject item)
    {
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
        CurrentItem = null;
        HasItem = false;
    }
}