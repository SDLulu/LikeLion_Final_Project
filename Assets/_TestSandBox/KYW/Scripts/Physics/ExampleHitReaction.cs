using UnityEngine;
using Fusion;

// 피격 반응 테스트용 예제 클래스 (네트워크 동기화)
public class ExampleHitReaction : NetworkBehaviour, IHitReaction
{
    [Networked]
    private NetworkBool IsStunnedNetworked { get; set; }

    [Networked]
    private NetworkBool IsKnockbackNetworked { get; set; }

    [Networked]
    private NetworkBool IsInvincibleNetworked { get; set; }

    [Networked]
    private TickTimer StunTimer { get; set; }

    [Networked]
    private TickTimer KnockbackTimer { get; set; }

    [Networked]
    private TickTimer InvincibleTimer { get; set; }

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D 컴포넌트가 필요합니다!");
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void ApplyHit(Vector2 knockbackForce, float knockbackDuration, float stunDuration, float invincibleDuration)
    {
        // 호스트에서만 상태 변경 처리
        if (!Object.HasStateAuthority) return;

        // 무적 상태면 피격 무시
        if (IsInvincibleNetworked) return;

        // 타이머 설정
        StunTimer = TickTimer.CreateFromSeconds(Runner, stunDuration);
        KnockbackTimer = TickTimer.CreateFromSeconds(Runner, knockbackDuration);
        InvincibleTimer = TickTimer.CreateFromSeconds(Runner, invincibleDuration);

        // 상태 설정
        IsStunnedNetworked = true;
        IsKnockbackNetworked = true;
        IsInvincibleNetworked = true;

        // 넉백 적용 (일반 Rigidbody2D 사용)
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;  // 기존 속도 초기화
            rb.AddForce(knockbackForce, ForceMode2D.Impulse);
        }

        // 시각 효과 RPC 호출
        RPC_ShowHitEffect();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            // 타이머 체크 및 상태 업데이트
            if (IsStunnedNetworked && StunTimer.Expired(Runner))
            {
                IsStunnedNetworked = false;
                RPC_OnStunEnd();
            }

            if (IsKnockbackNetworked && KnockbackTimer.Expired(Runner))
            {
                IsKnockbackNetworked = false;
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }

            if (IsInvincibleNetworked && InvincibleTimer.Expired(Runner))
            {
                IsInvincibleNetworked = false;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowHitEffect()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            Invoke(nameof(ResetColor), 0.1f);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnStunEnd()
    {
        Debug.Log("스턴 상태 종료");
    }

    private void ResetColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    // IHitReaction 인터페이스 구현
    public bool IsStunned => IsStunnedNetworked;
    public bool IsKnockback => IsKnockbackNetworked;
    public bool IsInvincible => IsInvincibleNetworked;

    public void OnHitEnd()
    {
        Debug.Log("피격 상태 종료");
    }

    // 디버그용 시각화 (로컬에서만 표시)
    private void OnGUI()
    {
        if (Runner == null || Object == null) return;

        Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        float y = Screen.height - screenPos.y;

        if (IsStunnedNetworked)
            GUI.Label(new Rect(screenPos.x - 50, y - 60, 100, 20), "스턴!");
        if (IsKnockbackNetworked)
            GUI.Label(new Rect(screenPos.x - 50, y - 40, 100, 20), "넉백!");
        if (IsInvincibleNetworked)
            GUI.Label(new Rect(screenPos.x - 50, y - 20, 100, 20), "무적!");
    }
} 