using UnityEngine;

// 🔨 사용 가능한 아이템 인터페이스 (간소화 버전)
// 3가지 입력 상태만 전달하고 아이템이 알아서 처리
public interface IUsableItem
{
    // 아이템이 현재 사용 가능한지
    bool CanUse { get; }
    
    // 🔨 클릭 시작 (GetPressed)
    void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition);
    
    // 🔄 클릭 유지 중 (IsSet)
    void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition);
    
    // 🔄 클릭 종료 (GetReleased)
    void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition);
}

// 🎯 던지기 가능한 아이템 인터페이스
public interface IThrowableItem
{
    // 던지기 데미지 설정값들
    float ThrowDamage { get; }
    float MinDamageSpeed { get; }  // 데미지를 주기 위한 최소 속도
    
    // 아이템이 던져질 때 호출
    void OnThrown(GameObject thrower, Vector2 throwVelocity);
    
    // 던져졌을 때 충돌 처리
    void OnThrownCollision(Collision2D collision, float impactSpeed, GameObject thrower);
}

// 🔨 기본 아이템 베이스 클래스 (간소화 버전 + 던지기 데미지)
public abstract class UsableItemBase : MonoBehaviour, IUsableItem, IThrowableItem
{
    [Header("Item Usage Settings")]
    [SerializeField] protected bool canUse = true;
    
    [Header("Throw Damage Settings")]
    [SerializeField] protected float throwDamage = 1f;         // 던지기 데미지 (고정)
    [SerializeField] protected float minDamageSpeed = 3f;      // 데미지를 주기 위한 최소 속도 (고정)
    [SerializeField] protected LayerMask damageableLayers = -1; // 데미지를 줄 수 있는 레이어
    
    // 🎯 던지기 상태 추적
    private bool isBeingThrown = false;
    private GameObject currentThrower = null;
    private Rigidbody2D rb;
    
    public virtual bool CanUse => canUse;
    public virtual float ThrowDamage => throwDamage;
    public virtual float MinDamageSpeed => minDamageSpeed;
    
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Rigidbody2D가 없으면 추가
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
        }
    }
    
    // 🎯 아이템이 던져질 때 호출 (PlayerItemPickup에서 호출)
    public virtual void OnThrown(GameObject thrower, Vector2 throwVelocity)
    {
        isBeingThrown = true;
        currentThrower = thrower;
        
        Debug.Log($"🎯 {name} 던져짐 - 던진 사람: {thrower.name}, 속도: {throwVelocity.magnitude:F1}");
        
        // 2초 후 던져진 상태 해제 (바닥에 떨어진 것으로 간주)
        Invoke(nameof(ResetThrowState), 2f);
    }
    
    private void ResetThrowState()
    {
        isBeingThrown = false;
        currentThrower = null;
        Debug.Log($"🎯 {name} 던지기 상태 해제");
    }
    
    // 🎯 충돌 처리 (데미지 판정)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isBeingThrown || currentThrower == null) return;
        
        // 충돌 속도 계산
        float impactSpeed = rb.linearVelocity.magnitude;
        
        // 최소 속도 이상이어야 데미지
        if (impactSpeed >= minDamageSpeed)
        {
            OnThrownCollision(collision, impactSpeed, currentThrower);
        }
    }
    
    // 🎯 던져진 아이템 충돌 처리 (오버라이드 가능)
    public virtual void OnThrownCollision(Collision2D collision, float impactSpeed, GameObject thrower)
    {
        GameObject target = collision.gameObject;
        
        // 자기 자신이나 던진 사람에게는 데미지 없음
        if (target == gameObject || target == thrower) return;
        
        // 레이어 체크
        if (!IsInDamageableLayer(target)) return;
        
        // 고정 데미지 (테스트용 간소화)
        float finalDamage = throwDamage; // 항상 고정 데미지
        
        Debug.Log($"🎯 던지기 데미지! {name} → {target.name} (속도: {impactSpeed:F1}, 데미지: {finalDamage})");
        
        // 실제 데미지 적용
        ApplyThrowDamage(target, finalDamage, thrower);
        
        // 던지기 상태 즉시 해제 (충돌했으니)
        CancelInvoke(nameof(ResetThrowState));
        ResetThrowState();
    }
    
    // 🎯 실제 데미지 적용 (오버라이드하여 다양한 데미지 시스템 지원)
    protected virtual void ApplyThrowDamage(GameObject target, float damage, GameObject thrower)
    {
        Debug.Log($"💥 {target.name}이(가) {damage} 데미지를 받았습니다! (던진 사람: {thrower.name})");
        
        // TestEnemy 시스템과 연동
        TestEnemy enemy = target.GetComponent<TestEnemy>();
        if (enemy != null)
        {
            Vector2 knockbackDirection = (target.transform.position - transform.position).normalized;
            enemy.TakeDamage(damage, knockbackDirection, 2f); // 고정 넉백 (2f)
        }
        else
        {
            // 다른 체력 시스템이 있다면 여기서 처리
            Debug.Log($"⚠️ {target.name}에게 데미지 시스템이 없습니다.");
            
            // 임시 피드백 (타겟을 살짝 밀어내기)
            var targetRb = target.GetComponent<Rigidbody2D>();
            if (targetRb != null)
            {
                Vector2 knockbackDirection = (target.transform.position - transform.position).normalized;
                targetRb.AddForce(knockbackDirection * 2f, ForceMode2D.Impulse);
            }
        }
    }
    
    // 🔍 데미지 가능한 레이어인지 체크
    private bool IsInDamageableLayer(GameObject obj)
    {
        return (damageableLayers.value & (1 << obj.layer)) != 0;
    }
    
    // 기본 구현들 (필요한 것만 오버라이드)
    public virtual void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 기본적으로는 아무것도 하지 않음
    }
    
    public virtual void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 기본적으로는 아무것도 하지 않음
    }
    
    public virtual void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 기본적으로는 아무것도 하지 않음
    }
} 