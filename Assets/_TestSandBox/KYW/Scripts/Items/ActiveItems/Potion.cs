using Fusion;
using UnityEngine;

// 🧪 포션 아이템 - 클릭해서 사용하면 체력 5회복
public class Potion : NetworkBehaviour, IItemInteraction
{
    [Header("🧪 포션 설정")]
    [SerializeField] private int healAmount = 5; // 회복할 체력량
    
    // 🌐 네트워크 동기화
    [Networked] private NetworkBool IsHeld { get; set; }
    bool IItemInteraction.IsHeld => IsHeld;
    
    public override void Spawned()
    {
        Runner.SetIsSimulated(Object, true);
        Debug.Log($"🧪 포션 스폰 완료! 회복량: {healAmount}");
    }
    
    // 🎯 IItemInteraction 인터페이스 구현 - 클릭 시 호출
    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (!Object.HasStateAuthority) return;
        
        // 자신을 들고 있는 플레이어의 체력 컴포넌트 찾기
        var playerHealth = transform.parent.parent.GetComponentInChildren<PlayerHealth>();
        if (playerHealth != null)
        {
            // 체력이 이미 최대인지 확인
            if (playerHealth.Health >= playerHealth.MaxHealth)
            {
                Debug.Log("[Potion] 체력이 이미 최대입니다!");
                return;
            }
            
            // 체력 회복
            playerHealth.Heal(healAmount);
            Debug.Log($"[Potion] 체력 {healAmount} 회복! 현재 체력: {playerHealth.Health}/{playerHealth.MaxHealth}");
            
            // 수집 소리와 이펙트 재생
            RPC_PlayCollectFeedback(transform.position);
        }
        else
        {
            Debug.LogError("[Potion] PlayerHealth를 찾을 수 없습니다!");
        }
        
        // 손에서 제거 및 Despawn
        RemoveFromHand();
    }
    
    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 홀드 기능 없음 (포션은 클릭 한 번으로 사용)
    }
    
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 릴리즈 기능 없음
    }
    
    public void OnPickedUp()
    {
        if (!Object.HasStateAuthority) return;
        
        IsHeld = true;
        Debug.Log("[Potion] 포션 픽업됨");
    }
    
    public void OnReleased()
    {
        if (!Object.HasStateAuthority) return;
        
        IsHeld = false;
        Debug.Log("[Potion] 포션 해제됨");
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
            Debug.Log("[Potion] 기존 던지기 시스템으로 손에서 제거됨");
        }
        else
        {
            Debug.LogError("[Potion] PlayerObjectThrower를 찾을 수 없습니다!");
        }
        
        // 물리 활성화는 상관없음 (어차피 바로 Despawn)
        Runner.Despawn(Object);
        Debug.Log("[Potion] 포션 Despawn됨");
    }
    
    // 🎮 IItemInteraction 인터페이스 구현
    public bool CanBeHeld => true;
    public bool CanBeThrown => true;
    
    // --- RPC 메서드들 ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayCollectFeedback(Vector3 pos)
    {
        AudioManager.Inst.PlaySound("힐", pos);
        EffectManager.Inst.PlayEffect("힐하트", pos);
    }
}
