using UnityEngine;
using System.Collections.Generic;
using Fusion;
public class FirePrefabController : NetworkBehaviour
{
    [SerializeField] private int projectileDmg = 1;
    [SerializeField] private float moveSpeed = 10f; // 발사체의 이동 속도입니다. 인스펙터 창에서 조절하세요.
    [SerializeField] private float lifeTime = 4f; // 발사체가 몇 초 후에 사라질지 결정합니다.
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private LayerMask groundLayerMask;
    [Networked] private NetworkBool didHitSomething { get; set; }
    [Networked] private TickTimer lifeTimer { get; set; } // 발사체의 생존 시간을 측정하는 네트워크 동기화 타이머입니다.


    private Collider2D coll;


    // 이 네트워크 오브젝트가 생성(Spawn)되었을 때 한 번 호출됩니다.
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
            transform.Translate(transform.up * moveSpeed * Runner.DeltaTime, Space.World);
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
