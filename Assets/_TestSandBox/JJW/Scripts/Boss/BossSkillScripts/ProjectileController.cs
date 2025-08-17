using System.Collections.Generic;
using Fusion;
using UnityEngine;


public class ProjectileController : NetworkBehaviour
{
    [Header("Projectile Stats")]
    [SerializeField] private int projectileDmg = 1;
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float lifeTime = 4f;

    [Header("Animation")]
    [SerializeField] private string vanishAnimName = "Vanish";
    
    [Header("Collision Layers")]
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private LayerMask groundLayerMask;

    // --- [변경점 1] OnChanged -> OnChangedRender로 속성 변경 ---
    [Networked, OnChangedRender(nameof(OnStateChangedRender))]
    private NetworkBool IsVanishing { get; set; }

    [Networked] private TickTimer lifeTimer { get; set; }

    private Collider2D coll;
    private Animator anim;
    private List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

    public override void Spawned()
    {
        coll = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
    }

    public override void FixedUpdateNetwork()
    {
        if (IsVanishing) return;

        if (lifeTimer.Expired(Runner))
        {
            if(Object.HasStateAuthority) Runner.Despawn(Object);
            return;
        }

        if (Object.HasStateAuthority)
        {
            CheckIfHitGround();
            if (!IsVanishing)
            {
                CheckIfWeHitAPlayer();
            }
        }
        
        transform.Translate(transform.up * moveSpeed * Runner.DeltaTime, Space.World);
    }
    
    /// <summary>
    /// IsVanishing 값이 변경될 때 모든 클라이언트의 Render 프레임에서 호출됩니다.
    /// </summary>
    public void OnStateChangedRender()
    {
        // IsVanishing 값이 true로 변경되었다면, 소멸 애니메이션을 재생합니다.
        // 인스턴스 메서드이므로 anim 변수에 직접 접근할 수 있어 코드가 더 간결해집니다.
        if (IsVanishing)
        {
            anim.CrossFadeInFixedTime(vanishAnimName, 0.1f);
        }
    }

    private void CheckIfHitGround()
    {
        var groundCollider = Runner.GetPhysicsScene2D().OverlapBox(transform.position, coll.bounds.size, 0, groundLayerMask);
        if (groundCollider != default)
        {
            IsVanishing = true;
        }
    }

    private void CheckIfWeHitAPlayer()
    {
        Runner.LagCompensation.OverlapBox(transform.position, coll.bounds.size, Quaternion.identity, Object.InputAuthority, hits, playerLayerMask);

        if (hits.Count > 0)
        {
            if (Runner.IsServer)
            {
                hits[0].Hitbox.GetComponentInParent<PlayerInteractionBase>().TakeDamage(projectileDmg);
            }
            Runner.Despawn(Object);
        }
    }

    /// <summary>
    /// "Vanish" 애니메이션의 마지막 프레임에 있는 이벤트가 호출할 함수입니다.
    /// </summary>
    public void HandleDespawnEvent()
    {
        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
}
