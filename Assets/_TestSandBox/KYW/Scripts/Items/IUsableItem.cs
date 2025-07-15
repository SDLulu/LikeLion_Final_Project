using UnityEngine;

/// <summary>
/// 🧭 아이템 기본 방향 (룰타일 스타일 8방향)
/// </summary>
[System.Serializable]
public enum ItemDirection
{
    Right = 0,        // 오른쪽 →
    RightUp = 1,      // 오른쪽 위 ↗
    Up = 2,           // 위 ↑
    LeftUp = 3,       // 왼쪽 위 ↖
    Left = 4,         // 왼쪽 ←
    LeftDown = 5,     // 왼쪽 아래 ↙
    Down = 6,         // 아래 ↓
    RightDown = 7     // 오른쪽 아래 ↘
}

/// <summary>
/// 🧭 ItemDirection 확장 메서드
/// </summary>
public static class ItemDirectionExtensions
{
    /// <summary>
    /// ItemDirection을 Vector2로 변환
    /// </summary>
    public static Vector2 ToVector2(this ItemDirection direction)
    {
        switch (direction)
        {
            case ItemDirection.Right:     return Vector2.right;           // (1, 0)
            case ItemDirection.RightUp:   return new Vector2(1, 1).normalized;    // (0.707, 0.707)
            case ItemDirection.Up:        return Vector2.up;              // (0, 1)
            case ItemDirection.LeftUp:    return new Vector2(-1, 1).normalized;   // (-0.707, 0.707)
            case ItemDirection.Left:      return Vector2.left;            // (-1, 0)
            case ItemDirection.LeftDown:  return new Vector2(-1, -1).normalized;  // (-0.707, -0.707)
            case ItemDirection.Down:      return Vector2.down;            // (0, -1)
            case ItemDirection.RightDown: return new Vector2(1, -1).normalized;   // (0.707, -0.707)
            default: return Vector2.right;
        }
    }
    
    /// <summary>
    /// ItemDirection을 각도(도)로 변환
    /// </summary>
    public static float ToAngle(this ItemDirection direction)
    {
        switch (direction)
        {
            case ItemDirection.Right:     return 0f;
            case ItemDirection.RightUp:   return 45f;
            case ItemDirection.Up:        return 90f;
            case ItemDirection.LeftUp:    return 135f;
            case ItemDirection.Left:      return 180f;
            case ItemDirection.LeftDown:  return 225f;
            case ItemDirection.Down:      return 270f;
            case ItemDirection.RightDown: return 315f;
            default: return 0f;
        }
    }
    
    /// <summary>
    /// ItemDirection을 한글 설명으로 변환
    /// </summary>
    public static string ToKorean(this ItemDirection direction)
    {
        switch (direction)
        {
            case ItemDirection.Right:     return "오른쪽 →";
            case ItemDirection.RightUp:   return "오른쪽 위 ↗";
            case ItemDirection.Up:        return "위 ↑";
            case ItemDirection.LeftUp:    return "왼쪽 위 ↖";
            case ItemDirection.Left:      return "왼쪽 ←";
            case ItemDirection.LeftDown:  return "왼쪽 아래 ↙";
            case ItemDirection.Down:      return "아래 ↓";
            case ItemDirection.RightDown: return "오른쪽 아래 ↘";
            default: return "오른쪽 →";
        }
    }
}

/*
 * ===============================================
 * 🛠️ 아이템 시스템 사용 가이드 (개발자용)
 * ===============================================
 * 
 * 📋 새 아이템 만드는 방법:
 * 
 * 1️⃣ 스크립트 생성:
 *    - UsableItemBase를 상속받는 클래스 생성
 *    - OnUsePress, OnUseHold, OnUseRelease 중 필요한 것만 오버라이드
 * 
 * 2️⃣ 프리팹 설정:
 *    - GameObject 생성
 *    - SpriteRenderer 추가 (아이템 모양)
 *    - Collider2D 추가 (IsTrigger = false)
 *    - Rigidbody2D 추가 (자동 생성됨)
 *    - 만든 아이템 스크립트 추가
 * 
 * 3️⃣ 방향 설정:
 *    - Inspector에서 Item Direction을 룰타일 스타일로 선택
 *    - 8방향 중 아이템 스프라이트가 향하는 기본 방향 설정
 *    - 런타임에 SetDirection()으로 동적 변경 가능
 * 
 * 4️⃣ 레이어 설정:
 *    - 아이템을 적절한 레이어에 배치
 *    - PlayerItemPickup의 itemLayerMask에 해당 레이어 체크
 * 
 * 💡 사용 패턴 예시:
 *    - 즉시 사용: OnUsePress만 구현 (폭탄, 물약 등)
 *    - 연속 사용: OnUsePress + OnUseHold (드릴, 기관총 등)
 *    - 충전 사용: OnUsePress + OnUseHold + OnUseRelease (활, 마법 등)
 * 
 * 🧭 방향 활용 예시:
 *    - 총: 오른쪽 방향, 마우스 방향으로 총알 발사
 *    - 칼: 위쪽 방향, 휘두르기 애니메이션 적용
 *    - 드릴: 왼쪽 방향, 회전 이펙트와 함께 사용
 *    - 방패: 왼쪽 방향, 플레이어 앞쪽 방어
 * 
 * 🎯 던지기 데미지:
 *    - UsableItemBase 상속 시 자동으로 던지기 데미지 지원
 *    - throwDamage, minDamageSpeed 값 조정 가능
 *    - ApplyThrowDamage 오버라이드로 커스텀 데미지 로직 가능
 * 
 * ⚠️ Fusion 2 주의사항:
 *    - 실제 게임에서는 NetworkBehaviour 상속 권장
 *    - Time.time 대신 Runner.SimulationTime 사용
 *    - Invoke 대신 TickTimer 사용
 *    - 상태 동기화가 필요한 경우 [Networked] 속성 추가
 */

// 🔨 사용 가능한 아이템 인터페이스 (간소화 버전)
// 3가지 입력 상태만 전달하고 아이템이 알아서 처리
public interface IUsableItem
{
    /// <summary>
    /// 아이템이 현재 사용 가능한 상태인지 반환 (쿨다운, 조건 등 체크)
    /// </summary>
    bool CanUse { get; }
    
    /// <summary>
    /// 🔄 아이템 이미지의 기본 방향 (회전 기준점)
    /// 예: 기관총(오른쪽), 활(위쪽), 드릴(왼쪽) 등
    /// </summary>
    Vector2 DefaultDirection { get; }
    
    /// <summary>
    /// 🔨 마우스 클릭 시작 시 호출 (GetPressed 패턴)
    /// 즉시 발동 아이템들은 이 메서드만 구현하면 됨
    /// </summary>
    /// <param name="mouseWorldPosition">월드 좌표계에서의 마우스 위치</param>
    /// <param name="playerPosition">플레이어의 현재 위치</param>
    void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition);
    
    /// <summary>
    /// 🔄 마우스 클릭 유지 중 매 프레임 호출 (IsSet 패턴)
    /// 연속 사용 아이템들이 구현 (드릴, 기관총 등)
    /// </summary>
    /// <param name="mouseWorldPosition">월드 좌표계에서의 마우스 위치</param>
    /// <param name="playerPosition">플레이어의 현재 위치</param>
    void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition);
    
    /// <summary>
    /// 🔄 마우스 클릭 종료 시 호출 (GetReleased 패턴)
    /// 충전형 아이템들이 구현 (활, 마법 등)
    /// </summary>
    /// <param name="mouseWorldPosition">월드 좌표계에서의 마우스 위치</param>
    /// <param name="playerPosition">플레이어의 현재 위치</param>
    void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition);
}

/// <summary>
/// 🎯 던지기 가능한 아이템 인터페이스
/// UsableItemBase가 자동으로 구현하므로 직접 구현할 필요 없음
/// </summary>
public interface IThrowableItem
{
    /// <summary>던지기 데미지 값</summary>
    float ThrowDamage { get; }
    
    /// <summary>데미지를 주기 위한 최소 충돌 속도</summary>
    float MinDamageSpeed { get; }
    
    /// <summary>
    /// 아이템이 던져질 때 호출 (PlayerItemPickup에서 자동 호출)
    /// </summary>
    /// <param name="thrower">던진 플레이어 GameObject</param>
    /// <param name="throwVelocity">던지기 속도</param>
    void OnThrown(GameObject thrower, Vector2 throwVelocity);
    
    /// <summary>
    /// 던져진 아이템이 충돌했을 때 호출 (베이스 클래스에서 자동 처리)
    /// </summary>
    /// <param name="collision">충돌 정보</param>
    /// <param name="impactSpeed">충돌 속도</param>
    /// <param name="thrower">던진 플레이어</param>
    void OnThrownCollision(Collision2D collision, float impactSpeed, GameObject thrower);
}

/// <summary>
/// 🔨 기본 아이템 베이스 클래스 (간소화 버전 + 던지기 데미지)
/// 
/// 📖 사용법:
/// 1. 이 클래스를 상속받아 새 아이템 클래스 생성
/// 2. OnUsePress, OnUseHold, OnUseRelease 중 필요한 것만 오버라이드
/// 3. 던지기 데미지는 자동으로 지원됨 (설정값만 조정)
/// 4. 커스텀 데미지 로직이 필요하면 ApplyThrowDamage 오버라이드
/// 
/// 💡 예시:
/// public class MyItem : UsableItemBase
/// {
///     public override void OnUsePress(Vector2 mousePos, Vector2 playerPos)
///     {
///         // 클릭 시 동작 구현
///     }
/// }
/// </summary>
public abstract class UsableItemBase : MonoBehaviour, IUsableItem, IThrowableItem
{
    [Header("Item Usage Settings")]
    [SerializeField] protected bool canUse = true; // 아이템 사용 가능 여부
    
    [Header("Rotation Settings - 아이템 회전 설정")]
    [Tooltip("아이템 이미지가 기본적으로 향하는 방향 (룰타일 스타일 8방향)")]
    [SerializeField] protected ItemDirection itemDirection = ItemDirection.Right;
    
    [Header("Throw Damage Settings - 던지기 데미지 설정 (테스트용 간소화)")]
    [Tooltip("던져서 맞았을 때 주는 데미지 (현재 1로 고정)")]
    [SerializeField] protected float throwDamage = 1f;
    
    [Tooltip("데미지를 주기 위한 최소 충돌 속도 (현재 3으로 고정)")]
    [SerializeField] protected float minDamageSpeed = 3f;
    
    [Tooltip("던지기 데미지를 받을 수 있는 레이어 (보통 Enemy 레이어)")]
    [SerializeField] protected LayerMask damageableLayers = -1;
    
    // 🎯 던지기 상태 추적 (자동 관리됨 - 건드리지 마세요)
    private bool isBeingThrown = false;
    private GameObject currentThrower = null;
    private Rigidbody2D rb;
    
    public virtual bool CanUse => canUse;
    public virtual Vector2 DefaultDirection => itemDirection.ToVector2();
    public virtual float ThrowDamage => throwDamage;
    public virtual float MinDamageSpeed => minDamageSpeed;
    
    /// <summary>
    /// 🧭 아이템 방향을 각도로 반환 (0~360도)
    /// </summary>
    public virtual float DefaultAngle => itemDirection.ToAngle();
    
    /// <summary>
    /// 🧭 아이템 방향을 한글로 반환 (디버그용)
    /// </summary>
    public virtual string DirectionDescription => itemDirection.ToKorean();
    
    /// <summary>
    /// 🧭 런타임에 아이템 방향 변경 (스크립트에서 사용)
    /// </summary>
    public virtual void SetDirection(ItemDirection newDirection)
    {
        itemDirection = newDirection;
        Debug.Log($"🧭 {name} 방향 변경: {itemDirection.ToKorean()}");
    }
    
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Rigidbody2D가 없으면 추가
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // 기본 물리 설정 (밀림 방지)
        rb.gravityScale = 1f;
        rb.linearDamping = 2f; // 공기 저항으로 미끄러짐 방지
        rb.angularDamping = 5f; // 회전 저항
        rb.freezeRotation = false; // 회전은 허용 (던질 때 자연스러움)
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 부드러운 움직임
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 정확한 충돌 감지
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
    
    // ===============================================
    // 🔨 기본 사용 메서드들 (상속받은 클래스에서 오버라이드)
    // ===============================================
    
    /// <summary>
    /// 클릭 시작 시 호출 - 즉시 발동 아이템은 이것만 구현
    /// 예: 폭탄 터뜨리기, 물약 마시기, 즉시 힐링 등
    /// </summary>
    public virtual void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 기본적으로는 아무것도 하지 않음 - 상속받아서 구현하세요
    }
    
    /// <summary>
    /// 클릭 유지 중 매 프레임 호출 - 연속 사용 아이템이 구현
    /// 예: 드릴 돌리기, 기관총 연사, 레이저 발사 등
    /// </summary>
    public virtual void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 기본적으로는 아무것도 하지 않음 - 상속받아서 구현하세요
    }
    
    /// <summary>
    /// 클릭 종료 시 호출 - 충전형 아이템이 구현
    /// 예: 활 발사, 마법 시전, 파워 어택 등
    /// </summary>
    public virtual void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 기본적으로는 아무것도 하지 않음 - 상속받아서 구현하세요
    }
} 