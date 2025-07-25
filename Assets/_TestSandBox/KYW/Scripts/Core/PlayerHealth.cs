using Fusion;
using UnityEngine;

// 플레이어 체력 관리 컴포넌트 (Fusion 네트워크 동기화)
public class PlayerHealth : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int Health { get; private set; } = 100;

    public int MaxHealth => 100;

    // 데미지 처리
    public void TakeDamage(int amount)
    {
        if (!HasStateAuthority) return;
        Health = Mathf.Max(Health - amount, 0);
        if (Health == 0)
        {
            OnDeath();
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
        // TODO: UI 갱신, 이펙트 등
    }

    // 사망 처리
    private void OnDeath()
    {
        Debug.Log("[PlayerHealth] Player died!");
        // TODO: 사망 애니메이션, 리스폰 등
    }
} 