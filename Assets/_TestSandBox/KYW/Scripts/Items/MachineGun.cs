using UnityEngine;
using Fusion;

// 기관단총
public class MachineGun : NetworkBehaviour, IUsableItem
{
    [SerializeField] private GameObject bulletPrefab; // 총알 프리팹
    [SerializeField] private Transform firePoint;     // 총알 발사 위치
    [SerializeField] private float fireRate = 0.1f;   // 연사 간격(초)
    [SerializeField] private float bulletSpeed = 15f; // 총알 속도
    [SerializeField] private int maxAmmo = 30;        // 최대 탄약
    [SerializeField] private float reloadTime = 2f;   // 재장전 시간(초)

    [Networked] private int currentAmmo { get; set; }
    [Networked] private bool isReloading { get; set; }
    private float lastFireTime;

    private void Start()
    {
        currentAmmo = maxAmmo;
        if (firePoint == null) firePoint = transform;
    }

    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 입력 권한자(InputAuthority, 보통 로컬 플레이어)만 발사 가능
        if (!Object.HasInputAuthority) return; // 내 입력이 아니면 무시
        if (isReloading || currentAmmo <= 0) 
        { 
            StartReloadRpc(); 
            return; 
        }
        FireBulletRpc(mouseWorldPosition);
    }

    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 입력 권한자(InputAuthority, 보통 로컬 플레이어)만 연사 가능
        if (!Object.HasInputAuthority) return; // 내 입력이 아니면 무시
        if (isReloading || currentAmmo <= 0) 
        { 
            StartReloadRpc(); 
            return; 
        }
        if (Time.time - lastFireTime >= fireRate)
        {
            FireBulletRpc(mouseWorldPosition);
        }
    }

    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void FireBulletRpc(Vector2 mouseWorldPosition)
    {
        // StateAuthority(호스트)에서 실제 총알 발사 처리
        FireBullet(mouseWorldPosition);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void StartReloadRpc()
    {
        // StateAuthority(호스트)에서 재장전 처리
        StartReload();
    }

    private void FireBullet(Vector2 mouseWorldPosition)
    {
        // StateAuthority(서버/호스트 권한자)에서만 총알 생성/스폰 가능
        if (!Object.HasStateAuthority) return; // 권한 없으면 무시
        Vector2 dir = (mouseWorldPosition - (Vector2)firePoint.position).normalized;
        if (bulletPrefab != null)
        {
            var bullet = Runner.Spawn(
                bulletPrefab, 
                firePoint.position, 
                Quaternion.identity, 
                Object.InputAuthority
            );
            var bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
                bulletScript.Initialize(dir, bulletSpeed, 10f, 3f, this);
        }
        currentAmmo--;
        lastFireTime = Time.time;
    }

    private void StartReload()
    {
        // StateAuthority에서만 재장전 상태 변경
        if (!Object.HasStateAuthority || isReloading) return;
        isReloading = true;
        Invoke(nameof(FinishReload), reloadTime);
    }
    
    private void FinishReload()
    {
        // StateAuthority에서만 탄약 및 재장전 상태 변경
        if (!Object.HasStateAuthority) return;
        currentAmmo = maxAmmo;
        isReloading = false;
    }
} 