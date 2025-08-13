using UnityEngine;
using Fusion;

// 기관단총
public class MachineGun : NetworkBehaviour, IItemInteraction
{
    [Header("References")]
    [SerializeField] private GameObject bulletPrefab; // 총알 프리팹
    [SerializeField] private Transform firePoint;     // 총알 발사 위치
    
    [Header("Weapon Settings")]
    [SerializeField] private float fireRate = 0.1f;   // 연사 간격(초)
    [SerializeField] private float bulletSpeed = 15f; // 총알 속도
    [SerializeField] private int maxAmmo = 30;        // 최대 탄약
    [SerializeField] private float reloadTime = 2f;   // 재장전 시간(초)
    [SerializeField] private float machineGunRecoilForce = 0.8f; // 리코일 힘(소량)
    [SerializeField] private float machineGunRecoilStun = 0.06f; // 입력/물리 차단 스턴 시간(짧게)

    [Networked] private int currentAmmo { get; set; }
    [Networked] private TickTimer fireRateTimer { get; set; }
    [Networked] private TickTimer reloadTimer { get; set; }
    [Networked] private NetworkBool IsHeld { get; set; }

    bool IItemInteraction.IsHeld => IsHeld;  // 인터페이스 구현 (명시적 구현)

    public override void Spawned()
    {
        // StateAuthority에서만 초기값 설정 (Host가 설정 → 모든 클라이언트에 동기화)
        if (Object.HasStateAuthority)
        {
            currentAmmo = maxAmmo;
        }        
        // 물리 시뮬레이션 설정
        Runner.SetIsSimulated(Object, true);
    }

    public override void FixedUpdateNetwork()
    {
        // StateAuthority(호스트)에서만 재장전 완료 처리
        if (Object.HasStateAuthority)
        {
            if (reloadTimer.Expired(Runner))
            {
                currentAmmo = maxAmmo;
                reloadTimer = TickTimer.None; // 타이머 정지
            }
        }
    }

    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (reloadTimer.IsRunning) return;
        
        if (currentAmmo <= 0) 
        { 
            StartReloadRpc(); 
            return; 
        }

        FireBulletRpc(mouseWorldPosition);
    }

    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void FireBulletRpc(Vector2 mouseWorldPosition)
    {
        // StateAuthority에서만 실행
        if (!Object.HasStateAuthority) return;
        
        // ✅ "타이머가 만료되었거나 시작 전인가?"를 확인해야 합니다.
        // 이 조건이 false여야 (즉, 타이머가 한창 돌고 있을 때) 차단됩니다.
        if (!fireRateTimer.ExpiredOrNotRunning(Runner)) return;
        
        FireBullet(mouseWorldPosition);
        currentAmmo--;
        
        // 발사 후 쿨다운 타이머 시작
        fireRateTimer = TickTimer.CreateFromSeconds(Runner, fireRate);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void StartReloadRpc()
    {
        // StateAuthority에서만 실행
        if (!Object.HasStateAuthority) return;
        
        if (reloadTimer.IsRunning) return;
        
        reloadTimer = TickTimer.CreateFromSeconds(Runner, reloadTime);
    }

    private void FireBullet(Vector2 mouseWorldPosition)
    {
        if (bulletPrefab == null || firePoint == null) return;

        // 발사 방향 계산 (총구에서 마우스 방향)
        Vector2 fireDirection = (mouseWorldPosition - (Vector2)this.transform.position).normalized;
        
        if (Runner.IsServer)
        {
            // 총알 생성
            var bullet = Runner.Spawn(bulletPrefab, firePoint.position, firePoint.rotation, Object.InputAuthority);
            
            // 발사 방향 직접 설정
            var bulletRb = bullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = fireDirection * bulletSpeed;
            }
            
            // 리코일 적용 (발사 반대 방향으로 넉백+짧은 스턴)
            ApplyRecoil(fireDirection, machineGunRecoilForce, machineGunRecoilStun);
            
        }
    }

    private void ApplyRecoil(Vector2 fireDirection, float force, float stunSeconds)
    {
        if (!Object.HasStateAuthority) return;

        var ownerObject = Runner.GetPlayerObject(Object.InputAuthority);
        if (ownerObject == null) return;

        var interaction = ownerObject.GetComponent<IPlayerInteraction>();
        if (interaction != null)
        {
            Vector2 recoilDir = (-fireDirection).normalized;
            Vector2 forceVec = recoilDir * force;
            interaction.ApplyKnockback(forceVec, stunSeconds);
        }
    }

    public void OnPickedUp()
    {
        if (!HasStateAuthority) return;
        IsHeld = true;
    }

    public void OnReleased()
    {
        if (!HasStateAuthority) return;
        IsHeld = false;
    }

    public void ApplyKnockback(Vector2 force, float duration = 0f)
    {
        if (!HasStateAuthority) return;
        
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }
} 