using Fusion;
using UnityEngine;

// 🧪 포션 아이템 - 클릭해서 사용하면 체력 회복
public class Potion : NetworkBehaviour, IItemInteraction
{
    [Header("🧪 포션 설정")]
    [SerializeField] private int healAmount = 2; // 회복할 체력량
    [SerializeField] private GameObject useEffect; // 사용 효과 (선택사항)
    [SerializeField] private AudioClip useSound; // 사용 사운드 (선택사항)
    
    [Header("🎯 상호작용 설정")]
    [SerializeField] private bool canBeHeld = true; // 들 수 있는지 여부
    [SerializeField] private bool canBeThrown = true; // 던질 수 있는지 여부
    
    // 🌐 네트워크 동기화
    [Networked] private NetworkBool IsHeld { get; set; }
    bool IItemInteraction.IsHeld => IsHeld;
    
    // 🎮 사용 가능 여부
    private bool isUsed = false;
    
    public override void Spawned()
    {
        Runner.SetIsSimulated(Object, true);
        Debug.Log($"🧪 포션 스폰 완료! 회복량: {healAmount}");
    }
    
    // 🎯 IItemInteraction 인터페이스 구현 - 클릭 시 호출
    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (isUsed) return; // 이미 사용된 포션
        if (!HasStateAuthority) return; // 권한 확인
        
        // 플레이어 찾기 (PlayerInteractionBase를 통해)
        var playerInteraction = FindPlayerInteraction();
        if (playerInteraction == null)
        {
            Debug.LogError("[Potion] PlayerInteractionBase를 찾을 수 없습니다!");
            return;
        }
        
        var playerHealth = playerInteraction.GetComponentInChildren<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("[Potion] PlayerHealth 컴포넌트를 찾을 수 없습니다!");
            return;
        }
        
        // 체력이 이미 최대인지 확인
        if (playerHealth.Health >= playerHealth.MaxHealth)
        {
            Debug.Log("[Potion] 체력이 이미 최대입니다!");
            return;
        }
        
        // 체력 회복
        playerHealth.Heal(healAmount);
        
        // 사용 완료 표시
        isUsed = true;
        
        // 효과 재생
        PlayUseEffect();
        
        // 사운드 재생
        PlayUseSound();
        
        Debug.Log($"[Potion] 체력 {healAmount} 회복! 현재 체력: {playerHealth.Health}/{playerHealth.MaxHealth}");
        
        // 포션 소멸
        Runner.Despawn(Object);
    }
    
    // 🔄 IItemInteraction 인터페이스 구현 - Hold 중
    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 포션은 Hold 기능이 없으므로 빈 구현
    }
    
    // 🔄 IItemInteraction 인터페이스 구현 - Release
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 포션은 Release 기능이 없으므로 빈 구현
    }
    
    // 🎯 플레이어 상호작용 컴포넌트 찾기 (개선된 버전)
    private PlayerInteractionBase FindPlayerInteraction()
    {
        // 현재 포션을 들고 있는 플레이어 찾기
        var allPlayers = FindObjectsByType<PlayerInteractionBase>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            var inventory = player.GetComponentInChildren<PlayerInventory>();
            if (inventory != null && inventory.CurrentHeldObject == this.gameObject)
            {
                return player;
            }
        }
        
        // 만약 찾지 못했다면, 포션의 부모 오브젝트를 통해 찾기 시도
        var parent = transform.parent;
        while (parent != null)
        {
            var playerInteraction = parent.GetComponent<PlayerInteractionBase>();
            if (playerInteraction != null)
            {
                return playerInteraction;
            }
            parent = parent.parent;
        }
        
        return null;
    }
    
    // 🎨 사용 효과 재생
    private void PlayUseEffect()
    {
        if (useEffect != null)
        {
            var effect = Instantiate(useEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    // 🔊 사용 사운드 재생
    private void PlayUseSound()
    {
        if (useSound != null)
        {
            AudioSource.PlayClipAtPoint(useSound, transform.position);
        }
    }
    
    // 🎮 IItemInteraction 인터페이스 구현
    public bool CanBeHeld => canBeHeld;
    public bool CanBeThrown => canBeThrown;
    
    public void OnPickedUp()
    {
        if (Object.HasStateAuthority) IsHeld = true;
    }
    
    public void OnReleased()
    {
        if (Object.HasStateAuthority) IsHeld = false;
    }
    
    // 🥊 넉백 적용 (IItemInteraction 인터페이스 구현)
    public void ApplyKnockback(Vector2 force, float duration = 0f)
    {
        // 포션은 넉백을 받지 않으므로 빈 구현
        // 필요하다면 Rigidbody2D를 통해 넉백을 적용할 수 있음
    }
    
    // 🎯 사용 가능 여부 확인
    public bool CanUse(PlayerInteractionBase playerInteraction)
    {
        if (isUsed) return false;
        
        var playerHealth = playerInteraction.GetComponentInChildren<PlayerHealth>();
        if (playerHealth == null) return false;
        
        // 체력이 최대가 아닐 때만 사용 가능
        return playerHealth.Health < playerHealth.MaxHealth;
    }
    
    // 📝 인스펙터에서 설정 변경 시 호출
    private void OnValidate()
    {
        // 회복량이 음수가 되지 않도록 제한
        healAmount = Mathf.Max(1, healAmount);
    }
}
