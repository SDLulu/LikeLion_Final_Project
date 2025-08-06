using Fusion;
using UnityEngine;

// 로켓 패시브 아이템
// 📍 위치: PassiveItems 폴더
// 🎯 목적: 사용 시 플레이어에게 연료를 소모하여 지속 점프할 수 있는 능력 부여
public class Rocket : NetworkBehaviour, IItemInteraction
{
    [Header("Rocket Settings")]
    [SerializeField] private string itemName = "Rocket";
    [SerializeField] private Sprite itemSprite;
    
    // 🌐 네트워크 동기화
    [Networked] private NetworkBool IsHeld { get; set; }
    bool IItemInteraction.IsHeld => IsHeld;
    
    // 🎮 IItemInteraction 인터페이스 구현
    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (!Object.HasStateAuthority) return;
        
        // 자신을 들고 있는 플레이어의 인벤토리 찾기
        var playerInventory = transform.parent.parent.GetComponentInChildren<PlayerInventory>();
        if (playerInventory != null)
        {
            // 로켓 효과 적용 (인벤토리에 패시브 아이템 추가)
            playerInventory.AddPassiveItem(gameObject);
            Debug.Log($"[Rocket] 플레이어 인벤토리에 로켓 효과 적용됨");
        }
        else
        {
            Debug.LogError("[Rocket] PlayerInventory를 찾을 수 없습니다!");
        }
        
        // 손에서 제거 및 Despawn
        RemoveFromHand();
    }
    
    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 홀드 기능 없음 (패시브 아이템은 클릭 한 번으로 사용)
    }
    
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 릴리즈 기능 없음
    }
    
    public void OnPickedUp()
    {
        if (!Object.HasStateAuthority) return;
        
        IsHeld = true;
        Debug.Log($"[Rocket] {itemName} 픽업됨");
    }
    
    public void OnReleased()
    {
        if (!Object.HasStateAuthority) return;
        
        IsHeld = false;
        Debug.Log($"[Rocket] {itemName} 해제됨");
    }
    
    public void ApplyKnockback(Vector2 force, float duration = 0f)
    {
        if (!HasStateAuthority) return;
        
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }
    
    // 🎯 손에서 제거하는 메서드 (기존 던지기 시스템 활용)
    private void RemoveFromHand()
    {
        if (!Object.HasStateAuthority) return;
        
        // 자신을 들고 있는 플레이어의 던지기 컴포넌트 찾기
        var playerThrower = GetComponentInParent<PlayerObjectThrower>();
        if (playerThrower != null)
        {
            playerThrower.ReleaseObject(gameObject, false); // applyForce = false
            Debug.Log($"[Rocket] 기존 던지기 시스템으로 손에서 제거됨");
        }
        else
        {
            Debug.LogError("[Rocket] PlayerObjectThrower를 찾을 수 없습니다!");
        }
        
        // 물리 활성화는 상관없음 (어차피 바로 Despawn)
        Runner.Despawn(Object);
        Debug.Log($"[Rocket] {itemName} Despawn됨");
    }
} 