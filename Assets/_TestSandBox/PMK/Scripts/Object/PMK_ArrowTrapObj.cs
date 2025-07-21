using UnityEngine;

public class PMK_ArrowTrapObj : MonoBehaviour
{
    [Header("화살 발사 설정")]
    [SerializeField] private float bounceForce = 3f;

    private Rigidbody2D rb;
    private bool isGrounded = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 땅에 닿지 않았고, 속도가 거의 0이 아니면 회전
        if (!isGrounded && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            RotateInDirection();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 충돌 지점이 나보다 어느 방향에 있는지 판단
        Vector2 bounceDir = (transform.position - collision.transform.position).normalized;

        // 오른쪽이면 양의 x, 왼쪽이면 음의 x
        if (collision.transform.position.x > transform.position.x)
            bounceDir.x = Mathf.Abs(bounceDir.x);  // 오른쪽 튕김
        else
            bounceDir.x = -Mathf.Abs(bounceDir.x); // 왼쪽 튕김

        // 기존 속도 제거하고 튕기기
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(bounceDir * bounceForce, ForceMode2D.Impulse);

        // Ground에 닿았는지 확인
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isGrounded = true;
        }
    }

    /// <summary>
    /// 현재 속도 방향으로 화살 회전
    /// </summary>
    private void RotateInDirection()
    {
        transform.right = rb.linearVelocity.normalized;
    }
}
