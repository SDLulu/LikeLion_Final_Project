using UnityEngine;
using Fusion;

// 단순화된 총알 - 발사 방향은 MachineGun에서 결정
public class Bullets : NetworkBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float damage = 1f;
    
    private Rigidbody2D rb;

    public override void Spawned()
    {
        // 물리 시뮬레이션 설정
        Runner.SetIsSimulated(Object, true);
        // Rigidbody2D 초기화
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"총알 명중: {other.name}에게 {damage} 데미지!");
        Runner.Despawn(Object);
    }

    //todo: 총알 명중 시 로직 추가
} 