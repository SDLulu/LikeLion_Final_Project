using Fusion;
using UnityEngine;

public class PMK_ArrowTrapObj : NetworkBehaviour
{
    [Header("튕김 설정")]
    [SerializeField] private float detectRadius = 0.3f; // 감지 반지름
    [SerializeField] private LayerMask playerLayerMask; // 플레이어만 감지할 마스크
    [SerializeField] private float groundedVelocityThreshold = 7f; // 바닥에 닿았는지 판단할 속도 임계값


    [Header("Attack Collision Handler")]

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

        // 수동 충돌 감지
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectRadius, playerLayerMask);
        if (hit != null)
        {
            Debug.Log($"[ArrowTrap] Overlap 감지됨: {hit.gameObject.name}");

            if (hit.gameObject.layer == LayerMask.NameToLayer("Player") && rb.linearVelocity.sqrMagnitude > groundedVelocityThreshold * groundedVelocityThreshold)
            {
                Debug.Log("피격됨");
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
}