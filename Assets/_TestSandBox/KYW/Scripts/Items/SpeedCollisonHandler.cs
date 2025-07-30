using Fusion;
using UnityEngine;

// 아이템 속도 기반 공격판정 컨트롤러 (던지기/떨어지기만)
public class SpeedCollisionHandler : NetworkBehaviour
{
    [Header("Speed Attack Settings")]
    [SerializeField] private float attackSpeedThreshold = 5f;  // 공격 콜라이더 활성화 속도 임계값
    [SerializeField] private float normalSpeedThreshold = 2f;  // 일반 콜라이더로 되돌릴 속도 임계값
    
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
        rb = GetComponent<Rigidbody2D>();
        itemInteraction = GetComponent<IItemInteraction>();
        
        // 물리 시뮬레이션 활성화
        Runner.SetIsSimulated(Object, true);
        
        // 콜라이더 초기 상태 설정
        InitializeColliders();
    }
    
    // 🎯 콜라이더 초기 상태 설정
    private void InitializeColliders()
    {
        if (speedAttackCollider != null)
        {
            speedAttackCollider.enabled = false; // SpeedAttack 콜라이더는 기본 비활성화
        }
    }
    
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        
        // 들려있는 상태면 속도 공격판정 무시
        if (IsItemHeld()) return;
        
        // 속도에 따른 공격판정 변경
        UpdateSpeedAttackColliderBasedOnSpeed();
    }
    
    // 🎯 속도에 따른 속도 공격 콜라이더 변경
    private void UpdateSpeedAttackColliderBasedOnSpeed()
    {
        float currentSpeed = rb.linearVelocity.magnitude;
        
        bool shouldBeSpeedAttackMode = currentSpeed >= attackSpeedThreshold;
        bool shouldBeNormalMode = currentSpeed <= normalSpeedThreshold;
        
        // 속도 공격 모드로 변경
        if (shouldBeSpeedAttackMode && !IsInSpeedAttackMode)
        {
            SetSpeedAttackCollider(true);
            IsInSpeedAttackMode = true;
            Debug.Log($"[SpeedCollisionHandler] 속도 공격 콜라이더 활성화: 속도={currentSpeed}");
        }
        // 일반 모드로 변경
        else if (shouldBeNormalMode && IsInSpeedAttackMode)
        {
            SetSpeedAttackCollider(false);
            IsInSpeedAttackMode = false;
            Debug.Log($"[SpeedCollisionHandler] 속도 공격 콜라이더 비활성화: 속도={currentSpeed}");
        }
    }
    
    // 🎯 속도 공격 콜라이더 설정
    private void SetSpeedAttackCollider(bool enabled)
    {
        if (speedAttackCollider != null)
        {
            speedAttackCollider.enabled = enabled;
        }
    }
    
    // 🎯 아이템이 들려있는지 확인
    private bool IsItemHeld()
    {
        if (itemInteraction != null)
        {
            return !itemInteraction.IsHoldable; // 들 수 없으면 들려있는 상태
        }
        return false;
    }
    
    // 🎯 속도 공격 콜라이더 충돌 처리
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;
        
        // 속도 공격 모드가 아니면 무시
        if (!IsInSpeedAttackMode) return;
        
        // 충돌 대상 처리
        HandleSpeedAttackCollision(other.gameObject);
    }
    
    // 🎯 속도 공격 충돌 처리
    private void HandleSpeedAttackCollision(GameObject target)
    {
        // 플레이어 충돌
        if (IsPlayerCollision(target))
        {
            HandlePlayerCollision(target);
        }
        // 적 충돌
        else if (IsEnemyCollision(target))
        {
            HandleEnemyCollision(target);
        }
        // NPC 충돌
        else if (IsNpcCollision(target))
        {
            HandleNpcCollision(target);
        }
        // 아이템 충돌
        else if (IsItemCollision(target))
        {
            HandleItemCollision(target);
        }
    }
    
    // 🎯 플레이어 충돌 처리
    private void HandlePlayerCollision(GameObject player)
    {
        var playerInteraction = player.GetComponent<IPlayerInteraction>();
        if (playerInteraction != null)
        {
            // 충돌 방향으로 넉백 힘 계산
            Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;
            Vector2 knockbackForceVector = knockbackDirection * speedAttackKnockbackForce;
            
            // 플레이어에게 넉백 + 데미지 + 무적 적용
            playerInteraction.ApplyKnockback(knockbackForceVector, speedAttackKnockbackDuration);
            playerInteraction.TakeDamage(speedAttackDamage);
            playerInteraction.SetInvincible(true, speedAttackKnockbackDuration);
            
            Debug.Log($"[SpeedCollisionHandler] 플레이어 속도 공격 명중: 데미지={speedAttackDamage}, 넉백={knockbackForceVector}");
        }
    }
    
    // 🎯 적 충돌 처리
    private void HandleEnemyCollision(GameObject enemy)
    {
        var enemyInteraction = enemy.GetComponent<IPlayerInteraction>(); // 적도 IPlayerInteraction 사용
        if (enemyInteraction != null)
        {
            // 충돌 방향으로 넉백 힘 계산
            Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;
            Vector2 knockbackForceVector = knockbackDirection * speedAttackKnockbackForce;
            
            // 적에게 넉백 + 데미지 + 무적 적용
            enemyInteraction.ApplyKnockback(knockbackForceVector, speedAttackKnockbackDuration);
            enemyInteraction.TakeDamage(speedAttackDamage);
            enemyInteraction.SetInvincible(true, speedAttackKnockbackDuration);
            
            Debug.Log($"[SpeedCollisionHandler] 적 속도 공격 명중: 데미지={speedAttackDamage}, 넉백={knockbackForceVector}");
        }
    }
    
    // 🎯 NPC 충돌 처리
    private void HandleNpcCollision(GameObject npc)
    {
        var npcInteraction = npc.GetComponent<IPlayerInteraction>(); // NPC도 IPlayerInteraction 사용
        if (npcInteraction != null)
        {
            // 충돌 방향으로 넉백 힘 계산
            Vector2 knockbackDirection = (npc.transform.position - transform.position).normalized;
            Vector2 knockbackForceVector = knockbackDirection * speedAttackKnockbackForce;
            
            // NPC에게 넉백 + 데미지 + 무적 적용
            npcInteraction.ApplyKnockback(knockbackForceVector, speedAttackKnockbackDuration);
            npcInteraction.TakeDamage(speedAttackDamage);
            npcInteraction.SetInvincible(true, speedAttackKnockbackDuration);
            
            Debug.Log($"[SpeedCollisionHandler] NPC 속도 공격 명중: 데미지={speedAttackDamage}, 넉백={knockbackForceVector}");
        }
    }
    
    // 🎯 아이템 충돌 처리
    private void HandleItemCollision(GameObject item)
    {
        var itemInteraction = item.GetComponent<IItemInteraction>();
        if (itemInteraction != null)
        {
            // 충돌 방향으로 넉백 힘 계산
            Vector2 knockbackDirection = (item.transform.position - transform.position).normalized;
            Vector2 knockbackForceVector = knockbackDirection * speedAttackKnockbackForce;
            
            // 아이템에게 넉백만 적용 (데미지 없음)
            itemInteraction.ApplyKnockback(knockbackForceVector, speedAttackKnockbackDuration);
            
            Debug.Log($"[SpeedCollisionHandler] 아이템 속도 공격 명중: 넉백={knockbackForceVector}");
        }
    }
    
    // 🎯 충돌 대상 확인 메서드들 (레이어 이름으로 명시적 체크)
    private bool IsPlayerCollision(GameObject obj)
    {
        return obj.layer == LayerMask.NameToLayer("Player");
    }
    
    private bool IsEnemyCollision(GameObject obj)
    {
        return obj.layer == LayerMask.NameToLayer("Enemy");
    }
    
    private bool IsNpcCollision(GameObject obj)
    {
        return obj.layer == LayerMask.NameToLayer("NPC");
    }
    
    private bool IsItemCollision(GameObject obj)
    {
        return obj.layer == LayerMask.NameToLayer("Item");
    }
    
    //  아이템이 놓아질 때 속도 공격판정 초기화
    public void OnItemReleased()
    {
        if (HasStateAuthority)
        {
            SetSpeedAttackCollider(false);
            IsInSpeedAttackMode = false;
        }
    }
} 