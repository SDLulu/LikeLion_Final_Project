using System;
using Fusion;
using UnityEngine;

// 플레이어 체력 관리 컴포넌트 (Fusion 네트워크 동기화)
public class PlayerHealth : NetworkBehaviour, ISoftReset
{
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int Health { get; private set; } = 5;

    public int MaxHealth => 99;
    public int StartHealth { get; private set; } = 5;

    public event Action OnHealthChangedEvent; // 인자 없는 알림 (인벤토리 방식과 통일)

    // PlayerState 변경을 위한 상태 관리자 참조
    private PlayerDeathHandler playerDeathHandler;

    public override void Spawned()
    {
        base.Spawned();
        Health = StartHealth;
        playerDeathHandler = GetComponentInParent<PlayerDeathHandler>();
    }

    /// <summary>
    /// 소프트 리셋: 체력을 시작 체력으로 되돌립니다.
    /// </summary>
    public void SoftResetStats()
    {
        if (HasStateAuthority == false)
        {
            return;
        }
        Health = StartHealth;
    }

    /// <summary>
    /// ISoftReset 구현: 체력을 시작 체력으로 복원합니다.
    /// </summary>
    public void SoftReset()
    {
        SoftResetStats();
    }

    // 데미지 처리
    public void TakeDamage(int amount)
    {
        if (!HasStateAuthority) return;
        Health = Mathf.Max(Health - amount, 0);
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
} 