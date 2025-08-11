using Fusion;
using UnityEngine;

public class PMK_ArrowTrapObj : NetworkBehaviour, IItemInteraction
{
    [Header("튕김 설정")]
    [SerializeField] private float detectRadius = 0.3f; // 감지 반지름
    [SerializeField] private LayerMask playerLayerMask; // 플레이어만 감지할 마스크
    [SerializeField] private float groundedVelocityThreshold = 7f; // 바닥에 닿았는지 판단할 속도 임계값

    // 🌐 네트워크 동기화
    [Networked] private NetworkBool IsHeld { get; set; }
    bool IItemInteraction.IsHeld => IsHeld;

    private Rigidbody2D rb;
    private bool isGrounded = false;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || rb == null)
            return;

        // 이동 방향 회전
        if (rb.linearVelocity.sqrMagnitude > 0.01f && rb.linearVelocity.sqrMagnitude > 2.5 * 2.5)
        {
            RotateInDirection();
        }

        if (isGrounded)
            return;

        // 수동 충돌 감지 (정지 조건)
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectRadius, playerLayerMask);
        if (hit != null)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Player") && rb.linearVelocity.sqrMagnitude > groundedVelocityThreshold * groundedVelocityThreshold)
            {
                rb.linearVelocity = Vector2.zero;
            }
            else if (hit.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                isGrounded = true; // 일단 맞았으면 정지 처리
            }
        }
    }

    private void RotateInDirection()
    {
        transform.right = rb.linearVelocity.normalized;
    }

    // 감지 범위 시각화 (에디터에서 확인용)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }

    public void ApplyKnockback(Vector2 force, float duration = 0)
    {
        if (!HasStateAuthority) return;

        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }

    public void OnPickedUp()
    {
        if (!Object.HasStateAuthority) return;
        
        IsHeld = true;
        Debug.Log("[PMK_ArrowTrapObj] 화살 함정 픽업됨");
    }

    public void OnReleased()
    {
        if (!Object.HasStateAuthority) return;
        
        IsHeld = false;
        Debug.Log("[PMK_ArrowTrapObj] 화살 함정 해제됨");
    }

    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
    }

    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
    }

    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
    }
}