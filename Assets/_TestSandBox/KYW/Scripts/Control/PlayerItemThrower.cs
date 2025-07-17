using Fusion;
using UnityEngine;

// 아이템 던지기 담당 컴포넌트
// Hand 하위 오브젝트에 위치
public class PlayerItemThrower : NetworkBehaviour
{
    [Header("Throw Settings")]
    [SerializeField] private float throwForce = 10f;      // 던지기 힘
    [SerializeField] private float throwOffset = 0.5f;    // 던지기 시작 위치 오프셋
    
    // 참조 컴포넌트
    private PlayerItemPickup itemPickup;

    [Networked]
    private NetworkButtons ButtonsPrevious { get; set; }
    
    public override void Spawned()
    {
        itemPickup = GetComponent<PlayerItemPickup>();
    }
    
    public void ProcessInput(SpelunkyPlayerData input)
    {
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;
        
        // 아이템 던지기 (우클릭)
        if (pressed.IsSet(SpelunkyInputButtons.ThrowItem) && itemPickup.HasItem)
        {
            ThrowItemRpc(input.MouseWorldPosition);
        }
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void ThrowItemRpc(Vector2 mouseWorldPosition)
    {
        if (!itemPickup.HasItem) return;
        
        Vector2 playerPosition = transform.parent != null ? transform.parent.position : transform.position;
        Vector2 throwDirection = (mouseWorldPosition - playerPosition).normalized;
        
        ThrowItem(throwDirection);
    }
    
    private void ThrowItem(Vector2 direction)
    {
        GameObject itemToThrow = itemPickup.CurrentItem;
        if (itemToThrow == null) return;
        
        // 아이템 상태 초기화
        itemPickup.ClearItem();
        
        // 던지기 위치 설정
        Vector2 playerPosition = transform.parent != null ? transform.parent.position : transform.position;
        itemToThrow.transform.SetParent(null);
        itemToThrow.transform.position = playerPosition + direction * throwOffset;
        
        EnableItemPhysics(itemToThrow, direction);
    }
    
    private void EnableItemPhysics(GameObject item, Vector2 throwDirection)
    {
        var rigidbody = item.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            rigidbody.simulated = true;
            rigidbody.isKinematic = false;
            rigidbody.linearVelocity = throwDirection * throwForce;
            rigidbody.angularVelocity = 0f;
        }
        
        var collider = item.GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = true;
    }
} 