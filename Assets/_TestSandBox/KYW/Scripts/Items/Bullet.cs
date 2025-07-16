using UnityEngine;

// 예제 총알 (간단 버전)
public class Bullet : MonoBehaviour
{
    private float damage;
    private Vector2 direction;
    private float speed;
    private float lifetime;
    private GameObject shooter;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }
    public void Initialize(Vector2 dir, float spd, float dmg, float life, GameObject shooterObj)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        lifetime = life;
        shooter = shooterObj;
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == shooter) return;
        Debug.Log($"총알 명중: {other.name}에게 {damage} 데미지!");
        Destroy(gameObject);
    }
} 