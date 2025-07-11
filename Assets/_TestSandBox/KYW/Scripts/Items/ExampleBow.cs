using UnityEngine;

// 🏹 예제 활 아이템 (간소화 버전 + 던지기 데미지)
// Press=차징시작, Hold=차징진행, Release=화살발사
// 원거리 무기이므로 중간 정도의 던지기 데미지
public class ExampleBow : UsableItemBase
{
    [Header("Bow Settings")]
    [SerializeField] private float maxChargeTime = 2f;     // 최대 차징 시간
    [SerializeField] private float minArrowSpeed = 5f;     // 최소 화살 속도
    [SerializeField] private float maxArrowSpeed = 20f;    // 최대 화살 속도
    [SerializeField] private GameObject arrowPrefab;       // 화살 프리팹
    
    private bool isCharging = false;
    private float chargeStartTime = 0f;
    private Vector2 aimDirection;
    
    protected override void Awake()
    {
        base.Awake();
        // 던지기 설정은 베이스 클래스 기본값 사용 (데미지: 1, 최소속도: 3)
    }
    
    // 🔨 클릭 시작 - 차징 시작
    public override void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        Debug.Log($"🏹 {name} 차징 시작!");
        
        isCharging = true;
        chargeStartTime = Time.time;
        aimDirection = (mouseWorldPosition - playerPosition).normalized;
        
        // 차징 사운드 시작
        PlayChargingSound(true);
        
        // 차징 이펙트 시작
        StartChargingEffect();
    }
    
    // 🔄 클릭 유지 - 차징 진행
    public override void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (!isCharging) return;
        
        // 조준 방향 업데이트
        aimDirection = (mouseWorldPosition - playerPosition).normalized;
        
        // 차징 진행도 계산
        float chargeProgress = GetChargeProgress();
        
        // 차징 이펙트 업데이트
        UpdateChargingEffect(chargeProgress);
        
        // 최대 차징 시간 도달하면 자동 발사
        if (chargeProgress >= 1f)
        {
            FireArrow(playerPosition, 1f);
        }
    }
    
    // 🔄 클릭 종료 - 화살 발사
    public override void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (!isCharging) return;
        
        Debug.Log($"🏹 {name} 화살 발사!");
        
        // 차징 진행도에 따른 발사
        float chargeProgress = GetChargeProgress();
        FireArrow(playerPosition, chargeProgress);
    }
    
    // 던지기 데미지는 베이스 클래스 기본 구현 사용 (간소화)
    
    private float GetChargeProgress()
    {
        if (!isCharging) return 0f;
        
        float chargeTime = Time.time - chargeStartTime;
        return Mathf.Clamp01(chargeTime / maxChargeTime);
    }
    
    private void FireArrow(Vector2 playerPosition, float chargeProgress)
    {
        isCharging = false;
        
        // 차징 진행도에 따른 화살 속도 계산
        float arrowSpeed = Mathf.Lerp(minArrowSpeed, maxArrowSpeed, chargeProgress);
        
        // 화살 생성 위치
        Vector2 firePosition = playerPosition + aimDirection * 0.5f;
        
        // 화살 생성 및 발사
        CreateAndFireArrow(firePosition, aimDirection, arrowSpeed);
        
        // 차징 이펙트 종료
        StopChargingEffect();
        
        // 차징 사운드 중지
        PlayChargingSound(false);
        
        // 발사 사운드
        PlayFireSound();
        
        Debug.Log($"🎯 화살 발사 - 차징: {chargeProgress:P0}, 속도: {arrowSpeed}");
    }
    
    private void CreateAndFireArrow(Vector2 position, Vector2 direction, float speed)
    {
        // TODO: 실제 화살 프리팹 생성 및 발사
        Debug.Log($"🏹 화살 생성: 위치={position}, 방향={direction}, 속도={speed}");
        
        // 임시 화살 오브젝트 생성
        GameObject arrow = new GameObject("Arrow");
        arrow.transform.position = position;
        
        // 화살에 Rigidbody2D 추가하여 물리 기반 이동
        Rigidbody2D rb = arrow.AddComponent<Rigidbody2D>();
        rb.linearVelocity = direction * speed;
        
        // 5초 후 제거
        Destroy(arrow, 5f);
    }
    
    private void StartChargingEffect()
    {
        Debug.Log("✨ 차징 이펙트 시작");
        // TODO: 차징 파티클 이펙트 시작
    }
    
    private void UpdateChargingEffect(float progress)
    {
        // TODO: 차징 진행도에 따른 이펙트 업데이트
        // Debug.Log($"✨ 차징 진행도: {progress:P0}");
    }
    
    private void StopChargingEffect()
    {
        Debug.Log("✨ 차징 이펙트 종료");
        // TODO: 차징 파티클 이펙트 종료
    }
    
    private void PlayChargingSound(bool start)
    {
        if (start)
        {
            Debug.Log("🔊 차징 사운드 시작");
            // TODO: 차징 사운드 재생
        }
        else
        {
            Debug.Log("🔇 차징 사운드 중지");
            // TODO: 차징 사운드 중지
        }
    }
    
    private void PlayFireSound()
    {
        Debug.Log("🎵 화살 발사 사운드");
        // TODO: 발사 사운드 재생
    }
    
    // 기즈모로 조준 방향 표시
    private void OnDrawGizmosSelected()
    {
        if (isCharging)
        {
            Gizmos.color = Color.green;
            
            // 조준선 표시
            Vector3 start = transform.position;
            Vector3 end = start + (Vector3)(aimDirection * 3f);
            Gizmos.DrawLine(start, end);
            
            // 차징 진행도 표시
            float progress = GetChargeProgress();
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, progress);
            Gizmos.DrawWireSphere(transform.position, 0.5f + progress * 0.5f);
        }
        

    }
} 