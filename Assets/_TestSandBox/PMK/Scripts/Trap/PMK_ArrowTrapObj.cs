using Fusion;
using UnityEngine;

public class PMK_ArrowTrapObj : NetworkBehaviour
{
    [Header("튕김 설정")]
    [SerializeField] private float bounceForce = 3f; // 튕김 힘

    private Rigidbody2D rb;
    private bool isGrounded = false;

    public override void Spawned()
    {
        // 네트워크 오브젝트가 스폰될 때 Rigidbody2D 컴포넌트를 가져옴
        rb = GetComponent<Rigidbody2D>();
    }

    public override void FixedUpdateNetwork()
    {
        // 권한이 없는 오브젝트는 처리하지 않음
        if (!Object.HasStateAuthority || isGrounded || rb == null)
            return;

        // 화살이 움직이는 방향으로 속도 설정
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            RotateInDirection();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 권한 없는 오브젝트가 처리하지 않도록 방어
        if (!Object.HasStateAuthority || rb == null) return;

        // 충돌한 오브젝트와 거리비교하여 튕길 방향 결정
        Vector2 bounceDir = (transform.position - collision.transform.position).normalized;

        if (collision.transform.position.x > transform.position.x)
            bounceDir.x = Mathf.Abs(bounceDir.x);  // 오른쪽
        else
            bounceDir.x = -Mathf.Abs(bounceDir.x); // 왼쪽

        // 속도 초기화
        rb.linearVelocity = Vector2.zero;
        // 튕길 방향으로 힘을 가함
        rb.AddForce(bounceDir * bounceForce, ForceMode2D.Impulse);

        // 지면에 닿으면 튕김 멈춤
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            rb.AddForce(bounceDir * bounceForce, ForceMode2D.Impulse); // 한번 더 튕김
            isGrounded = true;
        }
    }

    // 화살이 이동하는 방향으로 회전
    private void RotateInDirection()
    {
        transform.right = rb.linearVelocity.normalized;
    }
}
