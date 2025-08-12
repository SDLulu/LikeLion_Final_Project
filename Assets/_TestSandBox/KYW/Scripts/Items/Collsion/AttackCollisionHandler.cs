using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class AttackCollisionHandler : NetworkBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.5f;
    
    [Header("Collider Reference")]
    [SerializeField] public Collider2D AttackCollider;
    
    private IItemInteraction weaponItem;
    
    [Header("Statue Hit Control")]
    [SerializeField] private float statueSwapCooldownSeconds = 0.15f;
    [Networked] private TickTimer StatueSwapCooldown { get; set; }
    
    public override void Spawned()
    {
        weaponItem = GetComponentInParent<IItemInteraction>();
        Runner.SetIsSimulated(Object, true);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority) return;
        
        if (IsSameItem(other.gameObject)) return;
        
        if (weaponItem == null) return;
        if (!weaponItem.IsHeld) return;
        
        HandleCollision(other.gameObject);
    }
    
    private bool IsSameItem(GameObject other)
    {
        if (other == gameObject) return true;
        
        if (weaponItem != null)
        {
            var otherItem = other.GetComponentInParent<IItemInteraction>();
            if (otherItem != null && otherItem == weaponItem) return true;
        }
        
        if (other.transform.IsChildOf(transform)) return true;
        
        if (transform.IsChildOf(other.transform)) return true;
        
        return false;
    }
    
    private void HandleCollision(GameObject target)
    {
        var playerInteraction = target.GetComponent<IPlayerInteraction>();
        if (playerInteraction != null)
        {
            ApplyDamageAndKnockback(target, playerInteraction);
            return;
        }
        
        var itemInteraction = target.GetComponent<IItemInteraction>();
        if (itemInteraction != null)
        {
            ApplyKnockbackOnly(target, itemInteraction);

            // 동상 추가 처리: 타겟이 동상이면 스킨 변경 + 위치 스왑 (쿨다운 중이면 무시)
            var statue = target.GetComponent<CharacterStatue>();
            // TickTimer는 만료되어도 자동으로 None으로 바뀌지 않으므로 ExpiredOrNotRunning(Runner)로 체크해야 재적용이 가능합니다.
            if (statue != null && StatueSwapCooldown.ExpiredOrNotRunning(Runner))
            {
                // 공격자 플레이어의 외형 컴포넌트 찾기 (Player 프리팹의 하위 back에 위치하므로 루트에서 탐색)
                var attackerRoot = transform.root;
                var appearance = attackerRoot != null ? attackerRoot.GetComponentInChildren<PlayerAppearance>() : null;
                if (appearance != null && appearance.HasStateAuthority)
                {
                    // 1) 스킨 변경 (네트워크 값만 변경)
                    appearance.ChangeSkin(statue.SkinKey);

                    // 2) 위치 스왑 (StateAuthority에서만 적용)
                    var playerTransform = attackerRoot;
                    var statueTransform = (statue.RootToMove != null ? statue.RootToMove : statue.transform);

                    Vector3 playerPos = playerTransform.position;
                    Vector3 statuePos = statueTransform.position;

                    var playerRb = playerTransform.GetComponent<Rigidbody2D>();
                    var statueRb = statueTransform.GetComponent<Rigidbody2D>();

                    // 속도 정지
                    if (playerRb != null)
                    {
                        playerRb.linearVelocity = Vector2.zero;
                        playerRb.angularVelocity = 0f;
                    }
                    if (statueRb != null)
                    {
                        statueRb.linearVelocity = Vector2.zero;
                        statueRb.angularVelocity = 0f;
                    }

                    // 위치 교환 (Z는 각자 유지)
                    if (playerRb != null)
                        playerRb.position = new Vector2(statuePos.x, statuePos.y);
                    else
                        playerTransform.position = new Vector3(statuePos.x, statuePos.y, playerPos.z);

                    if (statueRb != null)
                        statueRb.position = new Vector2(playerPos.x, playerPos.y);
                    else
                        statueTransform.position = new Vector3(playerPos.x, playerPos.y, statuePos.z);

                    // 추가 트리거 방지용 쿨다운 시작
                    StatueSwapCooldown = TickTimer.CreateFromSeconds(Runner, statueSwapCooldownSeconds);
                }
            }
        }
    }
    
    private void ApplyDamageAndKnockback(GameObject target, IPlayerInteraction interaction)
    {
        Vector2 knockbackDirection = ((Vector2)target.transform.position - (Vector2)AttackCollider.bounds.center).normalized;
        Vector2 knockbackForceVector = knockbackDirection * knockbackForce;
        
        interaction.ApplyKnockback(knockbackForceVector, knockbackDuration);
        interaction.TakeDamage(attackDamage);
        interaction.SetInvincible(true, knockbackDuration);
    }
    
    private void ApplyKnockbackOnly(GameObject target, IItemInteraction interaction)
    {
        Vector2 knockbackDirection = ((Vector2)target.transform.position - (Vector2)AttackCollider.bounds.center).normalized;
        Vector2 knockbackForceVector = knockbackDirection * knockbackForce;
        
        interaction.ApplyKnockback(knockbackForceVector, knockbackDuration);
    }
} 