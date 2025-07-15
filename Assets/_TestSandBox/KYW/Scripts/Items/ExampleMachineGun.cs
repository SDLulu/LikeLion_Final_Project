using UnityEngine;

// 🔫 예제 기관단총 아이템 (연속 발사)
// 클릭 유지 시 일정 딜레이마다 마우스 방향으로 총알 발사
// ⚠️ Fusion 2 주의: 실제 게임에서는 탄약 수를 NetworkBehaviour로 동기화 권장
public class ExampleMachineGun : UsableItemBase
{
    [Header("Machine Gun Settings")]
    [SerializeField] private GameObject bulletPrefab; // 총알 프리팹
    [SerializeField] private Transform firePoint; // 총알 발사 위치
    [SerializeField] private float fireRate = 0.1f; // 발사 간격 (초)
    [SerializeField] private float bulletSpeed = 15f; // 총알 속도
    [SerializeField] private float bulletDamage = 10f; // 총알 데미지
    [SerializeField] private float bulletLifetime = 3f; // 총알 생존 시간
    [SerializeField] private int maxAmmo = 30; // 최대 탄약
    [SerializeField] private float reloadTime = 2f; // 재장전 시간
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem muzzleFlash; // 총구 화염
    [SerializeField] private AudioSource fireSound; // 발사 소리
    
    [Header("Current State")]
    [SerializeField] private int currentAmmo; // 현재 탄약
    [SerializeField] private bool isFiring = false; // 발사 중인지
    [SerializeField] private bool isReloading = false; // 재장전 중인지
    [SerializeField] private float lastFireTime; // 마지막 발사 시간
    
    private Camera playerCamera;
    
    protected override void Awake()
    {
        base.Awake();
        currentAmmo = maxAmmo;
        playerCamera = Camera.main;

        
        // 발사 지점이 없으면 자신의 위치 사용
        if (firePoint == null)
            firePoint = transform;
    }
    
    // 🔫 클릭 시작 - 발사 시작
    public override void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (isReloading)
        {
            Debug.Log("⏳ 재장전 중입니다!");
            return;
        }
        
        if (currentAmmo <= 0)
        {
            Debug.Log("🔄 탄약이 없습니다! 재장전 중...");
            StartReload();
            return;
        }
        
        Debug.Log($"🔫 {name} 발사 시작!");
        isFiring = true;
        
        // 첫 발사
        FireBullet(mouseWorldPosition, playerPosition);
    }
    
    // 🔫 클릭 유지 중 - 연속 발사
    public override void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (!isFiring || isReloading) return;
        
        // 발사 간격 체크
        if (Time.time - lastFireTime >= fireRate)
        {
            if (currentAmmo <= 0)
            {
                Debug.Log("🔄 탄약 소진! 재장전 중...");
                StartReload();
                return;
            }
            
            FireBullet(mouseWorldPosition, playerPosition);
        }
    }
    
    // 🔫 클릭 종료 - 발사 중단
    public override void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        Debug.Log($"🔫 {name} 발사 중단!");
        isFiring = false;
    }
    
    // 총알 발사 처리
    private void FireBullet(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 발사 방향 계산
        Vector2 fireDirection = (mouseWorldPosition - (Vector2)firePoint.position).normalized;
        
        // 총알 생성
        if (bulletPrefab != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            
            // 총알 설정
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(fireDirection, bulletSpeed, bulletDamage, bulletLifetime, gameObject);
            }
            else
            {
                // Bullet 스크립트가 없으면 기본 물리 적용
                Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
                if (bulletRb != null)
                {
                    bulletRb.linearVelocity = fireDirection * bulletSpeed;
                    Destroy(bullet, bulletLifetime);
                }
            }
        }
        else
        {
            // 총알 프리팹이 없으면 기본 총알 생성
            CreateBasicBullet(firePoint.position, fireDirection);
        }
        
        // 탄약 소모
        currentAmmo--;
        lastFireTime = Time.time;
        
        // 이펙트 재생
        PlayFireEffects();
        
        Debug.Log($"🔫 발사! 남은 탄약: {currentAmmo}");
    }
    
    // 기본 총알 생성 (프리팹이 없을 때)
    private void CreateBasicBullet(Vector2 position, Vector2 direction)
    {
        GameObject bullet = new GameObject("Basic Bullet");
        bullet.transform.position = position;
        
        // 기본 컴포넌트 추가
        SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
        sr.color = Color.yellow;
        sr.sprite = CreateBulletSprite();
        
        CircleCollider2D col = bullet.AddComponent<CircleCollider2D>();
        col.radius = 0.05f;
        col.isTrigger = true;
        
        Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearVelocity = direction * bulletSpeed;
        
        // 기본 총알 스크립트 추가
        Bullet bulletScript = bullet.AddComponent<Bullet>();
        bulletScript.Initialize(direction, bulletSpeed, bulletDamage, bulletLifetime, gameObject);
    }
    
    // 간단한 총알 스프라이트 생성
    private Sprite CreateBulletSprite()
    {
        Texture2D texture = new Texture2D(4, 4);
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                texture.SetPixel(x, y, Color.yellow);
            }
        }
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
    }
    
    // 발사 이펙트 재생
    private void PlayFireEffects()
    {
        // 총구 화염
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        
        // 발사 소리
        if (fireSound != null)
        {
            fireSound.Play();
        }
    }
    
    // 재장전 시작
    private void StartReload()
    {
        if (isReloading) return;
        
        isReloading = true;
        isFiring = false;
        
        Debug.Log($"🔄 재장전 시작... ({reloadTime}초)");
        Invoke(nameof(FinishReload), reloadTime);
    }
    
    // 재장전 완료
    private void FinishReload()
    {
        currentAmmo = maxAmmo;
        isReloading = false;
        
        Debug.Log($"✅ 재장전 완료! 탄약: {currentAmmo}");
    }
    
    // 수동 재장전 (R키 등)
    public void ManualReload()
    {
        if (currentAmmo < maxAmmo && !isReloading)
        {
            StartReload();
        }
    }
    
    // UI용 탄약 정보
    public string GetAmmoInfo()
    {
        if (isReloading) return "재장전 중...";
        return $"{currentAmmo}/{maxAmmo}";
    }
    
    // 기즈모로 발사 방향 표시
    private void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
            
            // 마우스 방향 표시
            if (playerCamera != null)
            {
                Vector3 mousePos = playerCamera.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0;
                Vector3 direction = (mousePos - firePoint.position).normalized;
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(firePoint.position, direction * 2f);
            }
        }
    }
} 