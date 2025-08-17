using Fusion;
using UnityEngine;

// 💀 플레이어 시체 프리팹 컴포넌트 - 아이템으로 처리
public class PlayerCorpse : NetworkBehaviour, IItemInteraction
{
    
    // 🌐 네트워크 동기화
    [Networked] private NetworkBool IsHeld { get; set; }
    bool IItemInteraction.IsHeld => IsHeld;
    
    public override void Spawned()
    {
        Runner.SetIsSimulated(Object, true);
        Debug.Log($"💀 시체 스폰 완료!");
    }
    
    // 🎯 IItemInteraction 인터페이스 구현 - 클릭 시 아무 동작 안함 (포션 먹기 제외)
    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 시체는 클릭해도 아무 동작 안함 (포션 먹기 기능 제외)
        Debug.Log($"[PlayerCorpse] 시체는 사용할 수 없습니다.");
    }
    
    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 홀드 기능 없음
    }
    
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 릴리즈 기능 없음
    }
    
    public void OnPickedUp()
    {
        if (!Object.HasStateAuthority) return;
        
        IsHeld = true;
        Debug.Log($"[PlayerCorpse] 시체 픽업됨");
    }
    
    public void OnReleased()
    {
        if (!Object.HasStateAuthority) return;
        
        IsHeld = false;
        Debug.Log($"[PlayerCorpse] 시체 해제됨");
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
    
    // 🎮 IItemInteraction 인터페이스 구현
    public bool CanBeHeld => true;
    public bool CanBeThrown => true;
} 