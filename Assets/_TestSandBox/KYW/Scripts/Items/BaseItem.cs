using Fusion;
using UnityEngine;

// 🏺 스펠렁키 아이템 기본 클래스
// 모든 아이템이 상속받는 단순한 베이스 클래스
public class BaseItem : NetworkBehaviour
{
    [Header("Item Info")]
    [SerializeField] protected string itemName = "Unknown Item";
    [SerializeField] protected Sprite itemIcon;
    [SerializeField] protected string description = "";
    
    [Header("Item Capabilities")]
    [SerializeField] protected bool canBeThrown = true; // 던질 수 있는지
    [SerializeField] protected bool canBeEquipped = false; // 장착 가능한지
    [SerializeField] protected bool isConsumable = false; // 소모품인지
    
    [Header("Physics Settings")]
    [SerializeField] protected float throwForce = 10f; // 던지는 힘
    [SerializeField] protected float mass = 1f; // 질량
    
    [Header("Pickup Settings")]
    [SerializeField] protected float pickupRange = 1.5f;
    [SerializeField] protected LayerMask playerLayer = 1;
    
    [Networked] public bool IsPickedUp { get; private set; } = false;
    [Networked] public PlayerRef CurrentHolder { get; private set; }
    [Networked] public bool IsEquipped { get; set; } = false;
    
    // 컴포넌트 참조
    protected SpriteRenderer spriteRenderer;
    protected Collider2D itemCollider;
    protected Rigidbody2D itemRigidbody;
    
    // 프로퍼티들
    public string ItemName => itemName;
    public Sprite ItemIcon => itemIcon;
    public string Description => description;
    public bool CanBeThrown => canBeThrown;
    public bool CanBeEquipped => canBeEquipped;
    public bool IsConsumable => isConsumable;
    
    public override void Spawned()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        itemCollider = GetComponent<Collider2D>();
        itemRigidbody = GetComponent<Rigidbody2D>();
        
        // 리지드바디가 없으면 추가
        if (itemRigidbody == null)
        {
            itemRigidbody = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // 질량 설정
        itemRigidbody.mass = mass;
        
        // 아이템 초기 상태 설정
        SetPickupState(false);
    }
    
    // 아이템 수집 가능한 플레이어 찾기
    public SpelunkyPlayerController FindNearbyPlayer()
    {
        if (IsPickedUp) return null;
        
        Collider2D[] nearbyPlayers = Physics2D.OverlapCircleAll(transform.position, pickupRange, playerLayer);
        
        foreach (var collider in nearbyPlayers)
        {
            var player = collider.GetComponent<SpelunkyPlayerController>();
            if (player != null && player.IsAlive)
            {
                return player;
            }
        }
        
        return null;
    }
    
    // 아이템 수집 처리
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void PickupItemRpc(PlayerRef player)
    {
        if (IsPickedUp) return;
        
        IsPickedUp = true;
        CurrentHolder = player;
        SetPickupState(true);
        
        OnPickup(player);
    }
    
    // 아이템 사용 처리 (기본: 던지기)
    public virtual void UseItem(SpelunkyPlayerController user, Vector2 targetPosition)
    {
        if (!IsPickedUp || CurrentHolder != user.Object.InputAuthority) return;
        
        // 기본적으로는 던지기 동작
        if (canBeThrown)
        {
            Vector2 throwDirection = (targetPosition - (Vector2)transform.position).normalized;
            ThrowItem(throwDirection);
        }
    }
    
    // 아이템 던지기 처리
    public virtual void ThrowItem(Vector2 direction)
    {
        if (!IsPickedUp) return;
        
        // 장착 상태면 해제
        if (IsEquipped)
        {
            UnEquip();
        }
        
        // 상태 초기화
        IsPickedUp = false;
        CurrentHolder = PlayerRef.None;
        SetPickupState(false);
        
        // 물리 적용
        if (itemRigidbody != null)
        {
            itemRigidbody.AddForce(direction * throwForce, ForceMode2D.Impulse);
        }
        
        OnThrow(direction);
    }
    
    // 아이템 드롭 처리
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void DropItemRpc(Vector3 position)
    {
        if (!IsPickedUp) return;
        
        // 장착 상태면 해제
        if (IsEquipped)
        {
            UnEquip();
        }
        
        IsPickedUp = false;
        CurrentHolder = PlayerRef.None;
        
        transform.position = position;
        SetPickupState(false);
        
        OnDrop();
    }
    
    // 장착 시도 (기본: 불가능)
    public virtual bool TryEquip(SpelunkyPlayerController player)
    {
        if (!canBeEquipped) return false;
        if (!IsPickedUp || CurrentHolder != player.Object.InputAuthority) return false;
        
        if (IsEquipped)
        {
            // 이미 장착된 경우 해제
            UnEquip();
            return false;
        }
        else
        {
            // 장착
            IsEquipped = true;
            OnEquip(player);
            return true;
        }
    }
    
    // 장착 해제
    public virtual void UnEquip()
    {
        if (!IsEquipped) return;
        
        IsEquipped = false;
        OnUnEquip();
    }
    
    // 아이템 파괴
    [Rpc(RpcSources.All, RpcTargets.All)]
    protected void DestroyItemRpc()
    {
        OnDestroy();
        
        if (Object && Object.IsValid)
        {
            Runner.Despawn(Object);
        }
    }
    
    // 수집 상태에 따른 렌더링 설정
    private void SetPickupState(bool pickedUp)
    {
        if (spriteRenderer) spriteRenderer.enabled = !pickedUp;
        if (itemCollider) itemCollider.enabled = !pickedUp;
        if (itemRigidbody) itemRigidbody.isKinematic = pickedUp;
    }
    
    // 🎯 각 아이템에서 필요시 오버라이드할 메서드들
    protected virtual void OnPickup(PlayerRef player) 
    {
        Debug.Log($"🤲 {itemName}이(가) {player}에게 수집됨");
    }
    
    protected virtual void OnThrow(Vector2 direction) 
    {
        Debug.Log($"🗑️ {itemName}이(가) 던져짐");
    }
    
    protected virtual void OnDrop() 
    {
        Debug.Log($"📦 {itemName}이(가) 떨어뜨려짐");
    }
    
    protected virtual void OnEquip(SpelunkyPlayerController player)
    {
        Debug.Log($"⚔️ {itemName} 장착!");
    }
    
    protected virtual void OnUnEquip()
    {
        Debug.Log($"⚔️ {itemName} 장착 해제!");
    }
    
    protected virtual void OnDestroy() 
    {
        Debug.Log($"💥 {itemName}이(가) 파괴됨");
    }
    
    // 🎨 에디터에서 수집 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
} 