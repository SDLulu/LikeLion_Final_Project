using UnityEngine;
using Fusion;

// 샷건
public class Shotgun : NetworkBehaviour, IUsableItem
{
    [Header("References")]
    [SerializeField] private GameObject bulletPrefab; // 총알 프리팹
    [SerializeField] private Transform firePoint;     // 총알 발사 위치
    
    [Header("Weapon Settings")]
    [SerializeField] private float fireRate = 0.8f;   // 연사 간격(초)
    [SerializeField] private float bulletSpeed = 12f; // 총알 속도
    [SerializeField] private int bulletCount = 5;     // 한 번에 발사할 총알 수
    [SerializeField] private float spreadAngle = 30f; // 총알 퍼짐 각도 (도)

    [Networked] private TickTimer fireRateTimer { get; set; }

    public override void Spawned()
    {
        // 물리 시뮬레이션 설정
        Runner.SetIsSimulated(Object, true);
        // 렌더링 소스 설정 (보간 사용)
        base.Object.RenderSource = RenderSource.Interpolated;
        // 원격 렌더링 타임프레임 강제 설정
        base.Object.ForceRemoteRenderTimeframe = true;
    }

    public override void FixedUpdateNetwork()
    {
        // 샷건은 장전 시스템이 없으므로 별도 처리 불필요
    }

    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 한 번 클릭할 때마다 발사
        FireShotgunRpc(mouseWorldPosition);
    }

    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void FireShotgunRpc(Vector2 mouseWorldPosition)
    {
        // StateAuthority에서만 실행
        if (!Object.HasStateAuthority) return;
        
        // 발사 쿨다운 체크
        if (!fireRateTimer.ExpiredOrNotRunning(Runner)) return;
        
        FireShotgun(mouseWorldPosition);
        
        // 발사 후 쿨다운 타이머 시작
        fireRateTimer = TickTimer.CreateFromSeconds(Runner, fireRate);
    }

    private void FireShotgun(Vector2 mouseWorldPosition)
    {
        if (bulletPrefab == null || firePoint == null) return;

        // 기본 발사 방향 계산 (오브젝트에서 마우스 방향)
        Vector2 baseDirection = (mouseWorldPosition - (Vector2)this.transform.position).normalized;
        
        if (Runner.IsServer)
        {
            // 여러 발의 총알 발사
            for (int i = 0; i < bulletCount; i++)
            {
                // 랜덤 각도 계산 (-spreadAngle/2 ~ +spreadAngle/2)
                float randomAngle = Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f);
                
                // 기본 방향을 랜덤 각도만큼 회전
                Vector2 fireDirection = RotateVector(baseDirection, randomAngle);
                
                // 총알 생성
                var bullet = Runner.Spawn(bulletPrefab, firePoint.position, firePoint.rotation, Object.InputAuthority);
                
                // 발사 방향 직접 설정
                var bulletRb = bullet.GetComponent<Rigidbody2D>();
                if (bulletRb != null)
                {
                    bulletRb.linearVelocity = fireDirection * bulletSpeed;
                }
                
            }
        }
    }

    // 벡터를 특정 각도만큼 회전시키는 헬퍼 메서드
    private Vector2 RotateVector(Vector2 vector, float angle)
    {
        float radian = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radian);
        float sin = Mathf.Sin(radian);
        
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

} 