using Fusion;
using UnityEngine;

// 아이템 속도 기반 공격판정 컨트롤러 (던지기/떨어지기만)
public class SpeedCollisionHandler : NetworkBehaviour
{
    [Header("Speed Attack Settings")]
    [SerializeField] private float attackSpeedThreshold = 0.3f;  // 공격 콜라이더 활성화 속도 임계값 (더 낮춤)
    [SerializeField] private float normalSpeedThreshold = 0.05f;  // 일반 콜라이더로 되돌릴 속도 임계값 (더 낮춤)
    
    [Header("Collision Settings")]
    [SerializeField] private int speedAttackDamage = 1;        // 속도 공격 데미지
    [SerializeField] private float speedAttackKnockbackForce = 5f; // 속도 공격 넉백 힘
    [SerializeField] private float speedAttackKnockbackDuration = 0.5f; // 속도 공격 넉백/무적 지속시간
    
    [Header("Collider References")]
    [SerializeField] private Collider2D speedAttackCollider;    // SpeedAttack 레이어 콜라이더 (속도 기반 공격용)
    
    private Rigidbody2D rb;
    private IItemInteraction itemInteraction;
    
    [Networked] private NetworkBool IsInSpeedAttackMode { get; set; } // 속도 공격 모드 상태
    
    public override void Spawned()
    {
        // 부모 오브젝트(곡괭이)의 Rigidbody2D 참조
        rb = GetComponentInParent<Rigidbody2D>();
        itemInteraction = GetComponentInParent<IItemInteraction>();
        
        Runner.SetIsSimulated(Object, true);
        InitializeColliders();
    }
    
    private void InitializeColliders()
    {
        if (speedAttackCollider != null)
        {
            speedAttackCollider.enabled = false;
        }
    }
    
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (itemInteraction == null || itemInteraction.IsHeld) return; // 들린 상태면 무시 (IsHeld = true일 때)
        

        
        UpdateSpeedAttackColliderBasedOnSpeed();
    }
    
    private void UpdateSpeedAttackColliderBasedOnSpeed()
    {
        float currentSpeed = rb.linearVelocity.magnitude;
        
        bool shouldBeSpeedAttackMode = currentSpeed >= attackSpeedThreshold;
        bool shouldBeNormalMode = currentSpeed <= normalSpeedThreshold;
        
        if (shouldBeSpeedAttackMode && !IsInSpeedAttackMode)
        {
            SetSpeedAttackCollider(true);
            IsInSpeedAttackMode = true;
        }
        else if (shouldBeNormalMode && IsInSpeedAttackMode)
        {
            SetSpeedAttackCollider(false);
            IsInSpeedAttackMode = false;
        }
    }
    
    private void SetSpeedAttackCollider(bool enabled)
    {
        if (speedAttackCollider != null)
        {
            speedAttackCollider.enabled = enabled;
        }
    }
    
    // 같은 아이템인지 확인 (자기 자신, 부모, 자식 모두 체크)
    private bool IsSameItem(GameObject other)
    {
        // 자기 자신
        if (other == gameObject) return true;
        
        // 부모가 같은지 확인 (같은 아이템의 다른 콜라이더)
        if (itemInteraction != null)
        {
            var otherItem = other.GetComponentInParent<IItemInteraction>();
            if (otherItem != null && otherItem == itemInteraction) return true;
        }
        
        // 자식인지 확인
        if (other.transform.IsChildOf(transform)) return true;
        
        // 부모인지 확인
        if (transform.IsChildOf(other.transform)) return true;
        
        return false;
    }
    

    

    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;
        if (itemInteraction == null || itemInteraction.IsHeld) return; // 들린 상태면 무시 (IsHeld = true일 때)
        if (!IsInSpeedAttackMode) return;
        
        // 자기 자신 또는 같은 아이템의 다른 콜라이더와의 충돌 방지
        if (IsSameItem(other.gameObject)) return;
        
        HandleCollision(other.gameObject);
    }
    
    private void HandleCollision(GameObject target)
    {
        // 플레이어, 적, NPC는 모두 IPlayerInteraction 사용 (나중에 적/NPC 처리를 다르게 할 수 있음)
        var playerInteraction = target.GetComponent<IPlayerInteraction>();
        if (playerInteraction != null)
        {
            ApplyDamageAndKnockback(target, playerInteraction);
            return;
        }
        
        // 아이템 충돌 처리
        var itemInteraction = target.GetComponent<IItemInteraction>();
        if (itemInteraction != null)
        {
            ApplyKnockbackOnly(target, itemInteraction);
        }
    }
    
    private void ApplyDamageAndKnockback(GameObject target, IPlayerInteraction interaction)
    {
        // 콜라이더의 실제 중심점을 기준으로 넉백 방향 계산 (오프셋 고려)
        Vector2 knockbackDirection = ((Vector2)target.transform.position - (Vector2)speedAttackCollider.bounds.center).normalized;
        Vector2 knockbackForceVector = knockbackDirection * speedAttackKnockbackForce;
        
        interaction.ApplyKnockback(knockbackForceVector, speedAttackKnockbackDuration);
        interaction.TakeDamage(speedAttackDamage);
        interaction.SetInvincible(true, speedAttackKnockbackDuration);
    }
    
    private void ApplyKnockbackOnly(GameObject target, IItemInteraction interaction)
    {
        // 콜라이더의 실제 중심점을 기준으로 넉백 방향 계산 (오프셋 고려)
        Vector2 knockbackDirection = ((Vector2)target.transform.position - (Vector2)speedAttackCollider.bounds.center).normalized;
        Vector2 knockbackForceVector = knockbackDirection * speedAttackKnockbackForce;
        
        interaction.ApplyKnockback(knockbackForceVector, speedAttackKnockbackDuration);
    }
    
} 