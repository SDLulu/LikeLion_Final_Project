using UnityEngine;
using System.Collections.Generic;
using Fusion;
public class MeteorPrefabController : NetworkBehaviour
{
    [SerializeField] private int projectileDmg = 1;
    [SerializeField] private float fallSpeed = 20f; // 메테오 낙하 속도
    [SerializeField] private float lifeTime = 5f; // 메테오가 땅에 닿지 않아도 사라질 시간
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private LayerMask groundLayerMask;
    [Networked] private NetworkBool didHitSomething { get; set; }
    [Networked] private TickTimer lifeTimer { get; set; }

    private Collider2D coll;

    public override void Spawned()
    {
        Runner.SetIsSimulated(Object, true);
        coll = GetComponent<Collider2D>();
        lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
    }

    // 물리 업데이트 주기에 맞춰 계속 호출됩니다.
    public override void FixedUpdateNetwork()
    {
        if (!didHitSomething)
        {
            CheckIfHitGround();
            CheckIfWeHitAPlayer();
        }

        if (lifeTimer.ExpiredOrNotRunning(Runner) == false && !didHitSomething)
        {
            transform.Translate(-1 * transform.up * fallSpeed * Runner.DeltaTime, Space.World);
        }

        if (lifeTimer.Expired(Runner) || didHitSomething)
        {
            lifeTimer = TickTimer.None;
            Runner.Despawn(Object);
        }
    }

    private void CheckIfHitGround()
    {
        var groundCollider = Runner.GetPhysicsScene2D()
            .OverlapBox(transform.position, coll.bounds.size, 0, groundLayerMask);

        if (groundCollider != default)
        {
            didHitSomething = true;
        }
    }

    private List<LagCompensatedHit> hits = new List<LagCompensatedHit>();
    private void CheckIfWeHitAPlayer()
    {
        Runner.LagCompensation.OverlapBox(transform.position, coll.bounds.size, Quaternion.identity,
            Object.InputAuthority, hits, playerLayerMask);

        if (hits.Count > 0)
        {
            foreach (var item in hits)
            {
                if (item.Hitbox != null)
                {
                    PlayerInteractionBase player = item.Hitbox.GetComponentInParent<PlayerInteractionBase>();

                    if (player != null)
                    {
                        if (Runner.IsServer)
                        {
                            player.TakeDamage(projectileDmg);
                        }
                        didHitSomething = true;
                        break;
                    }
                }
            }
        }
    }
}
