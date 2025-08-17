using Fusion;
using UnityEngine;

// 자석 패시브 아이템
// 📍 위치: PassiveItems 폴더
// 🎯 목적: 사용 시 주변 아이템을 자동으로 끌어당기는 능력 부여
public class Magnet : NetworkBehaviour, IItemInteraction
{
    [Header("Magnet Settings")]
    [SerializeField] private string itemName = "Magnet";
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
            // 자석 효과 적용 (인벤토리에 패시브 아이템 추가)
            playerInventory.AddPassiveItem(gameObject);
            var netObj = playerInventory.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                NetworkEventSystem.Inst.TriggerItemCollected(netObj.InputAuthority, 1);
            }
            Debug.Log($"[Magnet] 플레이어 인벤토리에 자석 효과 적용됨");
            
            // 장착 사운드 재생
            RPC_PlayEquipSound();
        }
        else
        {
            Debug.LogError("[Magnet] PlayerInventory를 찾을 수 없습니다!");
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
        Debug.Log($"[Magnet] {itemName} 픽업됨");
    }
    
    public void OnReleased()
    {
        if (!Object.HasStateAuthority) return;
        
        IsHeld = false;
        Debug.Log($"[Magnet] {itemName} 해제됨");
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
        var shopitem = GetComponent<ShopItem>();
        var shopmanager = FindFirstObjectByType<ShopManager>();
        if (shopitem != null)
        {
            if (shopitem.ItemData.IsAvailable)
            {
                shopmanager.Rpc_ReportTheftByData(shopitem.ItemData);
            }
        }
        // 자신을 들고 있는 플레이어의 던지기 컴포넌트 찾기
        var playerThrower = GetComponentInParent<PlayerObjectThrower>();
        if (playerThrower != null)
        {
            playerThrower.ReleaseObject(gameObject, false); // applyForce = false
            Debug.Log($"[Magnet] 기존 던지기 시스템으로 손에서 제거됨");
        }
        else
        {
            Debug.LogError("[Magnet] PlayerObjectThrower를 찾을 수 없습니다!");
        }
        
        // 물리 활성화는 상관없음 (어차피 바로 Despawn)
        Runner.Despawn(Object);
        Debug.Log($"[Magnet] {itemName} Despawn됨");
    }

    // --- RPC 메서드들 ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayEquipSound()
    {
        // 자석 장착 사운드 재생
        AudioManager.Inst.PlaySound("장착", transform.position);
    }
} 