using Fusion;
using UnityEngine;

// 아이템 속도 기반 공격판정 컨트롤러 (던지기/떨어지기만)
public class SpeedCollisionHandler : NetworkBehaviour
{
    [Header("Settings (SO 우선)")]
    [SerializeField] private SpeedCollisionSettingsSO settings; // Inspector 비우면 Provider → Resources 순으로 자동 로드
    [Header("Speed Attack Settings")]
    [SerializeField] private float attackSpeedThreshold = 3f;  // 공격 콜라이더 활성화 속도 임계값 (더 낮춤)
    [SerializeField] private float normalSpeedThreshold = 1f;  // 일반 콜라이더로 되돌릴 속도 임계값 (더 낮춤)
    
    [Header("Collision Settings")]
    [SerializeField] private int speedAttackDamage = 1;        // 속도 공격 데미지
    [SerializeField] private float speedAttackKnockbackForce = 5f; // 속도 공격 넉백 힘
    [SerializeField] private float speedAttackKnockbackDuration = 0.5f; // 속도 공격 넉백/무적 지속시간
    
    [Header("Collider References")]
    [SerializeField] private Collider2D[] speedAttackColliders;    // SpeedAttack 레이어 콜라이더 배열 (속도 기반 공격용)
    
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

        // 설정 자동 로드 (Inspector 비어있을 때)
        if (settings == null)
        {
            settings = Resources.Load<SpeedCollisionSettingsSO>("SpeedCollisionSettingsSO");
        }
    }
    
    private void InitializeColliders()
    {
        if (speedAttackColliders != null)
        {
            foreach (Collider2D collider in speedAttackColliders)
            {
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }
    }
    
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        // 아이템이 아닌 총알 등에서도 동작해야 하므로 null은 허용
        // 단, 아이템인 경우 들린 상태(IsHeld)일 때만 무시
        if (itemInteraction != null && itemInteraction.IsHeld) return;
        

        
        UpdateSpeedAttackColliderBasedOnSpeed();
    }
    
    private void UpdateSpeedAttackColliderBasedOnSpeed()
    {
        float currentSpeed = rb.linearVelocity.magnitude;
        
        float attackThreshold = settings != null ? settings.AttackSpeedThreshold : attackSpeedThreshold;
        float normalThreshold = settings != null ? settings.NormalSpeedThreshold : normalSpeedThreshold;

        bool shouldBeSpeedAttackMode = currentSpeed >= attackThreshold;
        bool shouldBeNormalMode = currentSpeed <= normalThreshold;
        
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
        if (speedAttackColliders != null)
        {
            foreach (Collider2D collider in speedAttackColliders)
            {
                if (collider != null)
                {
                    collider.enabled = enabled;
                }
            }
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
        // 아이템이 아닌 총알 등에서도 동작해야 하므로 null은 허용
        // 단, 아이템인 경우 들린 상태(IsHeld)일 때만 무시
        if (itemInteraction != null && itemInteraction.IsHeld) return;
        if (!IsInSpeedAttackMode) return;
        
        // 자기 자신 또는 같은 아이템의 다른 콜라이더와의 충돌 방지
        if (IsSameItem(other.gameObject)) return;
        
        HandleCollision(other.gameObject);
    }
    
    private void HandleCollision(GameObject target)
    {
        bool didHit = false;
        
        // 보스 충돌 처리 (우선순위 높음)
        var boss = target.GetComponent<BossBase>();
        if (boss != null)
        {
            boss.TakeDamage(speedAttackDamage);
            didHit = true;
            // 타격 효과 재생
            RPC_PlayHitEffect(target.transform.position);
        }
        
        // 플레이어, 적, NPC는 모두 IPlayerInteraction 사용 (나중에 적/NPC 처리를 다르게 할 수 있음)
        var playerInteraction = target.GetComponent<IPlayerInteraction>();
        if (playerInteraction != null)
        {
            ApplyDamageAndKnockback(target, playerInteraction);
            didHit = true;
            // 타격 효과 재생
            RPC_PlayHitEffect(target.transform.position);
            // return; -> 아이템 충돌도 함께 체크할 수 있도록 반환 제거
        }
        
        // 아이템 충돌 처리
        var itemInteraction = target.GetComponent<IItemInteraction>();
        if (itemInteraction != null)
        {
            ApplyKnockbackOnly(target, itemInteraction);
            // 아이템도 데미지를 받을 수 있다면 체력 감소 처리
            var damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(speedAttackDamage);
            }
            didHit = true;
            // 타격 효과 재생
            RPC_PlayHitEffect(target.transform.position);
        }

        // 총알에 부착된 스피드콜라이더인 경우, 유효한 히트가 있었다면 다음 틱에 소멸 요청
        if (didHit)
        {
            var bullet = GetComponentInParent<Bullets>();
            bullet?.RequestDespawn();
        }
    }
    
    private void ApplyDamageAndKnockback(GameObject target, IPlayerInteraction interaction)
    {
        // 콜라이더의 실제 중심점을 기준으로 넉백 방향 계산 (오프셋 고려)
        Vector2 knockbackDirection = ((Vector2)target.transform.position - (Vector2)speedAttackColliders[0].bounds.center).normalized; // 첫 번째 콜라이더를 기준으로 넉백 방향 계산
        Vector2 knockbackForceVector = knockbackDirection * speedAttackKnockbackForce;
        
        interaction.ApplyKnockback(knockbackForceVector, speedAttackKnockbackDuration);
        interaction.TakeDamage(speedAttackDamage);
        interaction.SetInvincible(true, speedAttackKnockbackDuration);
    }
    
    private void ApplyKnockbackOnly(GameObject target, IItemInteraction interaction)
    {
        // 콜라이더의 실제 중심점을 기준으로 넉백 방향 계산 (오프셋 고려)
        Vector2 knockbackDirection = ((Vector2)target.transform.position - (Vector2)speedAttackColliders[0].bounds.center).normalized; // 첫 번째 콜라이더를 기준으로 넉백 방향 계산
        Vector2 knockbackForceVector = knockbackDirection * speedAttackKnockbackForce;
        
        interaction.ApplyKnockback(knockbackForceVector, speedAttackKnockbackDuration);
    }
    
    // --- RPC 메서드들 ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayHitEffect(Vector3 hitPosition)
    {
        // 타격음과 타격 이펙트 재생
        // AudioManager.Inst.PlaySound("충돌", hitPosition);
        // EffectManager.Inst.PlayEffect("충돌", hitPosition);
    }
} 