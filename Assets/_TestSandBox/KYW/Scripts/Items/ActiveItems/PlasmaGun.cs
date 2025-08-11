using UnityEngine;
using Fusion;

// 플라즈마건
public class PlasmaGun : NetworkBehaviour, IItemInteraction
{
    [Header("References")]
    [SerializeField] private GameObject plasmaBulletPrefab; // 플라즈마 총알 프리팹
    [SerializeField] private Transform firePoint;           // 총알 발사 위치
    
    [Header("Weapon Settings")]
    [SerializeField] private float fireRate = 1f;        // 연사 간격(초)
    [SerializeField] private float bulletSpeed = 15f;      // 총알 속도 - 샷건보다 빠름

    [Networked] private TickTimer fireRateTimer { get; set; }
    [Networked] private NetworkBool IsHeld { get; set; }

    bool IItemInteraction.IsHeld => IsHeld;  // 인터페이스 구현 (명시적 구현)

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
        // 플라즈마건은 장전 시스템이 없으므로 별도 처리 불필요
    }

    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 한 번 클릭할 때마다 발사
        FirePlasmaRpc(mouseWorldPosition);
    }

    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void FirePlasmaRpc(Vector2 mouseWorldPosition)
    {
        // StateAuthority에서만 실행
        if (!Object.HasStateAuthority) return;
        
        // 발사 쿨다운 체크
        if (!fireRateTimer.ExpiredOrNotRunning(Runner)) return;
        
        FirePlasma(mouseWorldPosition);
        
        // 발사 후 쿨다운 타이머 시작
        fireRateTimer = TickTimer.CreateFromSeconds(Runner, fireRate);
    }

    private void FirePlasma(Vector2 mouseWorldPosition)
    {
        if (plasmaBulletPrefab == null || firePoint == null) return;

        // 발사 방향 계산 (오브젝트에서 마우스 방향)
        Vector2 fireDirection = (mouseWorldPosition - (Vector2)this.transform.position).normalized;
        
        if (Runner.IsServer)
        {
            // 플라즈마 총알 한 발만 발사
            var plasmaBullet = Runner.Spawn(plasmaBulletPrefab, firePoint.position, firePoint.rotation, Object.InputAuthority);
            
            // 발사 방향 설정
            var bulletRb = plasmaBullet.GetComponent<Rigidbody2D>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = fireDirection * bulletSpeed;
            }
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