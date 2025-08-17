using Fusion;
using UnityEngine;

public class LandingEffectController : NetworkBehaviour
{
    [SerializeField] private float lifeTime = 0.5f; // 이펙트가 유지될 짧은 시간
    [Networked] private TickTimer lifeTimer { get; set; }
    public override void Spawned()
    {
        lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
    }
    public override void FixedUpdateNetwork()
    {
        // 호스트에서 타이머가 만료되면 이펙트를 소멸시킵니다.
        if (Object.HasStateAuthority && lifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }
}
