using Fusion;
using UnityEngine;

// 🏺 스펠렁키 아이템 기본 클래스
// 모든 아이템이 상속받는 베이스 클래스
public abstract class BaseItem : NetworkBehaviour
{
    [Header("Item Info")]
    [SerializeField] protected string itemName = "Unknown Item";
    [SerializeField] protected Sprite itemIcon;
    [SerializeField] protected string description = "";
    
    [Header("Pickup Settings")]
    [SerializeField] protected float pickupRange = 1.5f;
    [SerializeField] protected LayerMask playerLayer = 1;
    
    [Networked] public bool IsPickedUp { get; private set; } = false;
    [Networked] public PlayerRef CurrentHolder { get; private set; }
    
    // 컴포넌트 참조
    protected SpriteRenderer spriteRenderer;
    protected Collider2D itemCollider;
    
    public override void Spawned()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        itemCollider = GetComponent<Collider2D>();
        
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
            if (player != null && player.IsAlive && player.IsDucking)
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
    
    // 아이템 사용 처리
    public virtual bool UseItem(SpelunkyPlayerController user)
    {
        if (!IsPickedUp || CurrentHolder != user.Object.InputAuthority) return false;
        
        bool success = OnUse(user);
        
        if (success)
        {
            // 사용 후 아이템 제거
            DestroyItemRpc();
        }
        
        return success;
    }
    
    // 아이템 드롭 처리
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void DropItemRpc(Vector3 position)
    {
        if (!IsPickedUp) return;
        
        IsPickedUp = false;
        CurrentHolder = PlayerRef.None;
        
        transform.position = position;
        SetPickupState(false);
        
        OnDrop();
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
    }
    
    // 📊 아이템 정보 프로퍼티들
    public string ItemName => itemName;
    public Sprite ItemIcon => itemIcon;
    public string Description => description;
    
    // 🎯 상속받은 클래스에서 구현해야 할 추상 메서드들
    protected abstract void OnPickup(PlayerRef player);
    protected abstract bool OnUse(SpelunkyPlayerController user);
    protected virtual void OnDrop() { }
    protected virtual void OnDestroy() { }
    
    // 🎨 에디터에서 수집 범위 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
} 