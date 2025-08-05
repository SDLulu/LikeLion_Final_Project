using UnityEngine;

public class PMK_TileTrap : MonoBehaviour, IItemInteraction
{
    [Header("Attack Collision Handler")]
    [SerializeField] private AttackCollisionHandler AttackCollisionHandler;
    private Collider2D attackCollider;
    private Rigidbody2D rb;
    private PMK_TileRPC_Manager tileRPCManager => PMK_TileRPC_Manager.Instance;

    public bool IsHeld => true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (AttackCollisionHandler != null)
        {
            attackCollider = AttackCollisionHandler.AttackCollider;
        }

        if (attackCollider != null)
            attackCollider.enabled = false;
    }

    private void Update()
    {
        if (rb.linearVelocity.y == 0)
        {
            attackCollider.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && rb != null && rb.linearVelocity.y < 0)
        {
            // 낙하 중일 때만 공격 처리
            attackCollider.enabled = true;
        }
    }

    public void ApplyKnockback(Vector2 force, float duration = 0)
    {
        if (!tileRPCManager.HasStateAuthority) return;

        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }

    public void OnPickedUp() { }

    public void OnReleased() { }

    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
}
