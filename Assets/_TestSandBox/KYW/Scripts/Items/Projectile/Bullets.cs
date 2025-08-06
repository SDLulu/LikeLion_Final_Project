using UnityEngine;
using Fusion;

public class Bullets : NetworkBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private int maxCollisions = 5; // 최대 충돌 횟수
    
    [Networked] private int collisionCount { get; set; } // 충돌 횟수 (네트워크 동기화)
    
    private Rigidbody2D rb;

    public override void Spawned()
    {
        // 물리 시뮬레이션 설정
        Runner.SetIsSimulated(Object, true);
        // Rigidbody2D 초기화
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        // 충돌 카운터 초기화
        collisionCount = 0;
    }

    // 물리적 충돌 (땅/벽과의 충돌) - Physics Material 적용됨
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!HasStateAuthority) return;
        
        // 충돌 횟수 증가
        collisionCount++;
        
        // 최대 충돌 횟수에 도달하면 despawn
        if (collisionCount >= maxCollisions)
        {
            Debug.Log($"총알이 {maxCollisions}번 충돌하여 소멸됩니다.");
            Runner.Despawn(Object);
        }
    }
} 