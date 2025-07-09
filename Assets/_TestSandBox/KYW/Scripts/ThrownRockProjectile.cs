using Fusion;
using UnityEngine;

// 🚀 던져진 돌 투사체
// 물리 기반으로 날아가는 돌의 동작 처리
public class ThrownRockProjectile : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float lifetime = 5f; // 자동 소멸 시간
    [SerializeField] private float bounceForce = 0.5f; // 바운스 강도
    [SerializeField] private LayerMask groundLayer = 1; // 땅 레이어
    
    [Networked] private TickTimer LifetimeTimer { get; set; }
    [Networked] public Vector2 Velocity { get; private set; }
    
    // 컴포넌트 참조
    private Rigidbody2D rb;
    private Collider2D projectileCollider;
    
    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        projectileCollider = GetComponent<Collider2D>();
        
        // 리지드바디가 없으면 추가
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // 콜라이더가 없으면 추가
        if (projectileCollider == null)
        {
            projectileCollider = gameObject.AddComponent<CircleCollider2D>();
            ((CircleCollider2D)projectileCollider).radius = 0.1f;
        }
        
        // 수명 타이머 설정
        LifetimeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
        
        // 물리 설정
        rb.gravityScale = 1f;
        rb.linearDamping = 0.1f;
    }
    
    public override void FixedUpdateNetwork()
    {
        // 수명 확인
        if (LifetimeTimer.Expired(Runner))
        {
            DestroyProjectileRpc();
            return;
        }
        
        // 속도 동기화
        if (rb != null)
        {
            Velocity = rb.linearVelocity;
        }
    }
    
    // 투사체 발사
    public void Launch(Vector2 force)
    {
        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
            Velocity = rb.linearVelocity;
        }
    }
    
    // 충돌 처리
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 땅에 닿으면 바운스
        if (IsInLayerMask(collision.gameObject.layer, groundLayer))
        {
            HandleGroundCollision(collision);
        }
        
        // 다른 플레이어나 적과의 충돌 처리
        var player = collision.gameObject.GetComponent<SpelunkyPlayerController>();
        if (player != null)
        {
            HandlePlayerHit(player);
        }
    }
    
    // 땅 충돌 처리
    private void HandleGroundCollision(Collision2D collision)
    {
        // 바운스 효과
        if (rb != null && rb.linearVelocity.magnitude > 1f)
        {
            Vector2 reflection = Vector2.Reflect(rb.linearVelocity, collision.contacts[0].normal);
            rb.linearVelocity = reflection * bounceForce;
        }
        else
        {
            // 속도가 느리면 정지
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
    
    // 플레이어 충돌 처리
    private void HandlePlayerHit(SpelunkyPlayerController player)
    {
        Debug.Log($"🪨 돌이 {player.Object.InputAuthority}를 맞췄습니다!");
        
        // 플레이어에게 넉백 적용
        var playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null && rb != null)
        {
            Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;
            playerRb.AddForce(knockbackDirection * 5f, ForceMode2D.Impulse);
        }
        
        // 돌 파괴
        DestroyProjectileRpc();
    }
    
    // 투사체 파괴
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void DestroyProjectileRpc()
    {
        if (Object && Object.IsValid)
        {
            Runner.Despawn(Object);
        }
    }
    
    // 레이어 마스크 확인 유틸리티
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) > 0;
    }
} 