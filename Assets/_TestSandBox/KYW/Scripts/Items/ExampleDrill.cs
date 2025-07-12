using UnityEngine;

// 🔧 예제 드릴 아이템 (간소화 버전 + 던지기 데미지)
// Press=시작, Hold=지속, Release=종료로 간단하게 처리
// 무거운 도구이므로 던지기 데미지가 높음
public class ExampleDrill : UsableItemBase
{
    [Header("Drill Settings")]
    [SerializeField] private float drillRange = 1.5f;
    [SerializeField] private float drillDamage = 10f;
    [SerializeField] private LayerMask targetLayers = -1;
    [SerializeField] private float drillTickRate = 0.1f; // 드릴링 간격
    
    private bool isDrilling = false;
    private float lastDrillTime = 0f;
    private Vector2 drillDirection;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 🔧 드릴 이미지 방향 설정 
        // 드릴 끝이 오른쪽을 향한다면 Vector2.right
        // 드릴 끝이 위쪽을 향한다면 Vector2.up
        defaultDirection = Vector2.right; // 보통 드릴은 오른쪽을 향함
        
        // 던지기 설정은 베이스 클래스 기본값 사용 (데미지: 1, 최소속도: 3)
    }
    
    // 🔨 클릭 시작 - 드릴 시작
    public override void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        Debug.Log($"🔧 {name} 드릴 시작!");
        isDrilling = true;
        drillDirection = (mouseWorldPosition - playerPosition).normalized;
        
        // 드릴 사운드 시작
        PlayDrillSound(true);
    }
    
    // 🔄 클릭 유지 - 드릴링 계속
    public override void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (!isDrilling) return;
        
        // 틱 레이트에 따른 드릴링
        // ⚠️ Fusion 2 주의: 실제로는 Runner.SimulationTime 사용 권장
        if (Time.time - lastDrillTime >= drillTickRate)
        {
            lastDrillTime = Time.time;
            
            // 드릴 방향 업데이트
            drillDirection = (mouseWorldPosition - playerPosition).normalized;
            
            // 드릴링 수행
            PerformDrilling(playerPosition);
        }
    }
    
    // 🔄 클릭 종료 - 드릴 중지
    public override void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        Debug.Log($"🔧 {name} 드릴 중지!");
        isDrilling = false;
        
        // 드릴 사운드 중지
        PlayDrillSound(false);
    }
    
    // 던지기 데미지는 베이스 클래스 기본 구현 사용 (간소화)
    
    private void PerformDrilling(Vector2 playerPosition)
    {
        Vector2 drillPosition = playerPosition + drillDirection * drillRange;
        
        // 드릴 범위 내 타겟 감지
        Collider2D[] targets = Physics2D.OverlapCircleAll(drillPosition, 0.5f, targetLayers);
        
        foreach (var target in targets)
        {
            if (target.transform.position != transform.position)
            {
                Debug.Log($"⛏️ 드릴링: {target.name}");
                
                // 드릴 데미지 적용
                ApplyDrillDamage(target);
                
                // 드릴 파티클 이펙트
                CreateDrillEffect(drillPosition);
            }
        }
    }
    
    private void ApplyDrillDamage(Collider2D target)
    {
        // TODO: 실제 데미지 시스템 연동
        Debug.Log($"⛏️ 드릴 데미지 {drillDamage}: {target.name}");
    }
    
    private void CreateDrillEffect(Vector2 position)
    {
        // TODO: 드릴 파티클 이펙트 생성
        Debug.Log($"✨ 드릴 이펙트: {position}");
    }
    
    private void PlayDrillSound(bool start)
    {
        if (start)
        {
            Debug.Log("🔊 드릴 사운드 시작");
            // TODO: 드릴 사운드 재생
        }
        else
        {
            Debug.Log("🔇 드릴 사운드 중지");
            // TODO: 드릴 사운드 중지
        }
    }
    
    // 기즈모로 드릴 범위 표시
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        
        Vector2 gizmoPos = transform.position;
        if (isDrilling)
        {
            gizmoPos += drillDirection * drillRange;
        }
        
        Gizmos.DrawWireSphere(gizmoPos, 0.5f);
        
        // 드릴 방향 표시
        if (isDrilling)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, gizmoPos);
        }
        
        // 던지기 데미지 표시 (다이아몬드 모양)
        Gizmos.color = Color.cyan;
        Vector3 pos = transform.position;
        Gizmos.DrawLine(pos + Vector3.up * 0.3f, pos + Vector3.right * 0.3f);
        Gizmos.DrawLine(pos + Vector3.right * 0.3f, pos + Vector3.down * 0.3f);
        Gizmos.DrawLine(pos + Vector3.down * 0.3f, pos + Vector3.left * 0.3f);
        Gizmos.DrawLine(pos + Vector3.left * 0.3f, pos + Vector3.up * 0.3f);
    }
} 