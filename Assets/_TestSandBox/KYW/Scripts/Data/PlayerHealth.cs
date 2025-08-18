using System;
using Fusion;
using UnityEngine;

// 플레이어 체력 관리 컴포넌트 (Fusion 네트워크 동기화)
public class PlayerHealth : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int Health { get; private set; } = 5;

    public int MaxHealth => 99;
    public int StartHealth { get; private set; } = 30;

    public event Action OnHealthChangedEvent; // 인자 없는 알림 (인벤토리 방식과 통일)

    // PlayerState 변경을 위한 상태 관리자 참조
    private PlayerDeathHandler playerDeathHandler;

    public override void Spawned()
    {
        base.Spawned();
        Health = StartHealth;
        playerDeathHandler = GetComponentInParent<PlayerDeathHandler>();
    }

    // 데미지 처리
    public void TakeDamage(int amount)
    {
        if (!HasStateAuthority) return;
        Health = Mathf.Max(Health - amount, 0);
        
        // 별 이펙트와 피 이펙트 재생
        RPC_PlayDamageEffects();
        
        if (Health == 0)
        {
            Death();
        }
    }

    // 힐 처리
    public void Heal(int amount)
    {
        if (!HasStateAuthority) return;
        Health = Mathf.Min(Health + amount, MaxHealth);
    }

    // 체력 변경 시 호출 (UI, 이펙트 등)
    private void OnHealthChanged()
    {
        Debug.Log($"[PlayerHealth] Health changed to: {Health}");
        // UI 갱신 이벤트 알림 (값은 구독자 측에서 조회)
        OnHealthChangedEvent?.Invoke();
    }

    // 사망 처리
    private void Death()
    {
        Debug.Log("[PlayerHealth] Player died!");
        if (playerDeathHandler != null)
        {
            playerDeathHandler.Die();
        }
    }
    
    // --- RPC 메서드들 ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayDamageEffects()
    {
        // 별 이펙트와 피 이펙트 재생 (+0.5y 높이에서 스폰)
        Vector3 effectPosition = transform.position + Vector3.up * 0.5f;
        AudioManager.Inst.PlaySound("별", effectPosition);
        EffectManager.Inst.PlayEffect("별", effectPosition);
        EffectManager.Inst.PlayEffect("피", effectPosition);
        AudioManager.Inst.PlaySound("피폭발", effectPosition);
    }
} 