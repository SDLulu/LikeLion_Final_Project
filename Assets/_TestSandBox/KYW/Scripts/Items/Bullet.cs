using UnityEngine;
using Fusion;

// 예제 총알 (간단 버전)
public class Bullet : NetworkBehaviour
{
    [Networked] private float damage { get; set; }
    [Networked] private Vector2 direction { get; set; }
    [Networked] private float speed { get; set; }
    [Networked] private float lifetime { get; set; }
    [Networked] private NetworkId shooterId { get; set; }
    private Rigidbody2D rb;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        // StateAuthority(서버/호스트 권한자)에서만 물리값(속도) 설정
        if (Object.HasStateAuthority)
            rb.linearVelocity = direction * speed;
        // StateAuthority(서버/호스트 권한자)에서만 파괴 예약
        if (Object.HasStateAuthority)
            Invoke(nameof(DespawnSelf), lifetime);
    }
    public void Initialize(Vector2 dir, float spd, float dmg, float life, NetworkBehaviour shooter)
    {
        // StateAuthority(서버/호스트 권한자)에서만 총알 정보 초기화
        if (!Object.HasStateAuthority) return;
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        lifetime = life;
        shooterId = shooter.Object.Id;
        rb.linearVelocity = direction * speed;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // StateAuthority(서버/호스트 권한자)에서만 충돌/파괴 처리
        if (!Object.HasStateAuthority) return;
        var shooterObj = Runner.FindObject(shooterId);
        if (other.gameObject == shooterObj?.gameObject) return;
        Debug.Log($"총알 명중: {other.name}에게 {damage} 데미지!");
        Runner.Despawn(Object);
    }

    private void DespawnSelf()
    {
        // StateAuthority(서버/호스트 권한자)에서만 파괴
        if (Object != null && Object.HasStateAuthority)
            Runner.Despawn(Object);
    }
} 