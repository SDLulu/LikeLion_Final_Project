using UnityEngine;

// 기관단총
public class MachineGun : MonoBehaviour, IUsableItem
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

    private void Start()
    {
        currentAmmo = maxAmmo;
        if (firePoint == null) firePoint = transform;
    }

    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (isReloading || currentAmmo <= 0) 
        { 
            StartReload(); 
            return; 
        }
        FireBullet(mouseWorldPosition);
    }

    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (isReloading || currentAmmo <= 0) 
        { 
            StartReload(); 
            return; 
        }
        if (Time.time - lastFireTime >= fireRate)
        {
            FireBullet(mouseWorldPosition);
        }
    }

    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    private void FireBullet(Vector2 mouseWorldPosition)
    {
        Vector2 dir = (mouseWorldPosition - (Vector2)firePoint.position).normalized;
        if (bulletPrefab != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            var bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
                bulletScript.Initialize(dir, bulletSpeed, 10f, 3f, gameObject);
            else
            {
                var rb = bullet.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = dir * bulletSpeed;
                Destroy(bullet, 3f);
            }
        }
        currentAmmo--;
        lastFireTime = Time.time;
    }

    private void StartReload()
    {
        if (isReloading) return;
        isReloading = true;
        Invoke(nameof(FinishReload), reloadTime);
    }
    
    private void FinishReload()
    {
        currentAmmo = maxAmmo;
        isReloading = false;
    }
} 