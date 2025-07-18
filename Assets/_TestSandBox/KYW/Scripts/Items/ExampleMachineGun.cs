using UnityEngine;

// 예제 기관단총 (간단 버전)
public class ExampleMachineGun : UsableItemBase
{
    [SerializeField] private GameObject bulletPrefab; // 총알 프리팹
    [SerializeField] private Transform firePoint;     // 총알 발사 위치
    [SerializeField] private float fireRate = 0.1f;   // 연사 간격(초)
    [SerializeField] private float bulletSpeed = 15f; // 총알 속도
    [SerializeField] private int maxAmmo = 30;        // 최대 탄약
    [SerializeField] private float reloadTime = 2f;   // 재장전 시간(초)

    private int currentAmmo;      // 현재 탄약
    private float lastFireTime;   // 마지막 발사 시각
    private bool isReloading;     // 재장전 중 여부

    // 시작 시 탄약 세팅, firePoint 없으면 자기 위치 사용
    private void Start()
    {
        currentAmmo = maxAmmo;
        if (firePoint == null) firePoint = transform;
    }

    protected override void StartUse(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 기관단총은 Press 방식이 아니므로, 아무 동작도 필요 없음
        // 또는 필요하다면 OnUsePress의 로직을 이곳으로 옮기세요
    }

    // 클릭 시작: 탄약 있으면 1발 발사, 없으면 재장전
    public override void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (isReloading || currentAmmo <= 0) { StartReload(); return; }
        FireBullet(mouseWorldPosition);
    }

    // 클릭 유지: 연사 간격마다 발사, 탄약 없으면 재장전
    public override void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (isReloading || currentAmmo <= 0) { StartReload(); return; }
        if (Time.time - lastFireTime >= fireRate)
            FireBullet(mouseWorldPosition);
    }

    // 클릭 종료: 별도 동작 없음
    public override void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    // 총알 발사 처리
    private void FireBullet(Vector2 mouseWorldPosition)
    {
        // 마우스 방향으로 총알 발사
        Vector2 dir = (mouseWorldPosition - (Vector2)firePoint.position).normalized;
        if (bulletPrefab != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            var bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
                bulletScript.Initialize(dir, bulletSpeed, 10f, 3f, gameObject); // 방향, 속도, 데미지, 생존시간, 발사자
            else
            {
                var rb = bullet.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = dir * bulletSpeed;
                Destroy(bullet, 3f);
            }
        }
        currentAmmo--;                // 탄약 소모
        lastFireTime = Time.time;     // 발사 시각 갱신
    }

    // 재장전 시작
    private void StartReload()
    {
        if (isReloading) return;
        isReloading = true;
        Invoke(nameof(FinishReload), reloadTime);
    }
    // 재장전 완료
    private void FinishReload()
    {
        currentAmmo = maxAmmo;
        isReloading = false;
    }
} 