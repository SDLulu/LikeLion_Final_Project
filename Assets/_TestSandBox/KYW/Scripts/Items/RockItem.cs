using Fusion;
using UnityEngine;

// 🪨 스펠렁키 돌 아이템
// 주울 수 있고, 던질 수 있으며, 빠른 속도로 날아갈 때 데미지를 주는 아이템
public class RockItem : BaseItem
{
    [Header("Rock Settings")]
    [SerializeField] private float customThrowAngle = 30f; // 던지는 각도
    [SerializeField] private float damageThresholdSpeed = 3f; // 데미지를 주는 최소 속도
    [SerializeField] private float damage = 5f; // 충돌 데미지
    [SerializeField] private float knockbackForce = 8f; // 넉백 힘
    [SerializeField] private LayerMask targetLayers = -1; // 데미지를 줄 대상 레이어
    
    [Header("Physics")]
    [SerializeField] private float bounceForce = 0.3f; // 바운스 감쇠율
    [SerializeField] private float lifetime = 10f; // 자동 소멸 시간
    
    [Networked] private TickTimer LifetimeTimer { get; set; }
    [Networked] private bool IsLifetimeActive { get; set; }
    
    public override void Spawned()
    {
        base.Spawned();
        
        // 기본 설정 (장착 불가능)
        canBeEquipped = false;
        
        // 수명 타이머 설정 (던져진 후에만 적용)
        if (!IsPickedUp)
        {
            LifetimeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
            IsLifetimeActive = true;
        }
    }
    
    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        
        // 수명 확인 (던져진 상태에서만)
        if (!IsPickedUp && IsLifetimeActive && LifetimeTimer.Expired(Runner))
        {
            DestroyItemRpc();
        }
    }
    
    // 돌 사용 (던지기)
    public override void UseItem(SpelunkyPlayerController user, Vector2 targetPosition)
    {
        if (!IsPickedUp || CurrentHolder != user.Object.InputAuthority) return;
        
        Debug.Log($"🪨 {user.Object.InputAuthority}가 돌을 던집니다!");
        
        // 던지는 방향 계산 (마우스 위치 기반)
        Vector2 throwDirection = (targetPosition - (Vector2)transform.position).normalized;
        
        // 각도 적용 (위쪽으로 던지기)
        float angleInRadians = customThrowAngle * Mathf.Deg2Rad;
        throwDirection = new Vector2(
            throwDirection.x * Mathf.Cos(angleInRadians),
            Mathf.Sin(angleInRadians)
        ).normalized;
        
        // 던지기
        ThrowItem(throwDirection);
        
        // 던져진 후 수명 타이머 시작
        LifetimeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
        IsLifetimeActive = true;
    }
    
    // 충돌 처리 (속도 기반 데미지)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 주워진 상태면 충돌 처리 안 함
        if (IsPickedUp) return;
        
        // 현재 속도 확인
        float currentSpeed = itemRigidbody.linearVelocity.magnitude;
        
        // 충돌 대상 확인
        var target = collision.gameObject;
        
        // 플레이어와 충돌
        var player = target.GetComponent<SpelunkyPlayerController>();
        if (player != null && currentSpeed >= damageThresholdSpeed)
        {
            HandlePlayerHit(player, collision);
        }
        
        // 땅과 충돌 (바운스 처리)
        if (IsGroundLayer(collision.gameObject.layer))
        {
            HandleGroundCollision(collision, currentSpeed);
        }
    }
    
    // 플레이어 충돌 처리
    private void HandlePlayerHit(SpelunkyPlayerController player, Collision2D collision)
    {
        Debug.Log($"🪨 돌이 {player.Object.InputAuthority}를 맞췄습니다! (데미지: {damage})");
        
        // 넉백 적용
        var playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;
            playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }
        
        // 돌의 속도 감소 (충돌 후)
        if (itemRigidbody != null)
        {
            itemRigidbody.linearVelocity *= 0.3f;
        }
    }
    
    // 땅 충돌 처리 (바운스)
    private void HandleGroundCollision(Collision2D collision, float speed)
    {
        if (itemRigidbody != null && speed > 1f)
        {
            // 바운스 효과
            Vector2 reflection = Vector2.Reflect(itemRigidbody.linearVelocity, collision.contacts[0].normal);
            itemRigidbody.linearVelocity = reflection * bounceForce;
            
            // 회전 추가 (굴러가는 효과)
            itemRigidbody.angularVelocity = Random.Range(-180f, 180f);
        }
        else if (speed <= 1f)
        {
            // 속도가 느리면 정지
            itemRigidbody.linearVelocity = Vector2.zero;
            itemRigidbody.angularVelocity = 0f;
        }
    }
    
    // 땅 레이어 확인
    private bool IsGroundLayer(int layer)
    {
        // Ground 레이어 확인 (보통 레이어 9번)
        return layer == 9 || LayerMask.LayerToName(layer).ToLower().Contains("ground");
    }
    
    // 수집 시 수명 타이머 무효화
    protected override void OnPickup(PlayerRef player)
    {
        Debug.Log($"🪨 {player}가 돌을 주웠습니다!");
        
        // 수명 타이머 무효화 (주워지면 더 이상 자동 소멸 안 함)
        LifetimeTimer = default;
        IsLifetimeActive = false;
    }
    
    // 던져질 때 물리 설정 최적화
    protected override void OnThrow(Vector2 direction)
    {
        Debug.Log("🪨 돌이 던져졌습니다!");
        
        // 던져진 상태에서는 더 빠른 물리 업데이트
        if (itemRigidbody != null)
        {
            itemRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }
    
    // 떨어뜨릴 때 물리 설정 정상화
    protected override void OnDrop()
    {
        Debug.Log("🪨 돌이 떨어졌습니다!");
        
        // 떨어뜨린 상태에서는 일반 물리 업데이트
        if (itemRigidbody != null)
        {
            itemRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        }
    }
    
    protected override void OnDestroy()
    {
        Debug.Log("🪨 돌이 파괴되었습니다!");
    }
} 