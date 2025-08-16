using UnityEngine;
using Fusion;
public class BossIdleState : BossStateBase
{
    public override BossStateName StateName => BossStateName.Idle;

    protected override void OnEnterState()
    {
        float randomIdleDuration = Random.Range(3f, 5f);
        fsmRef.BossNetworkBehaviour.StateTimer = TickTimer.CreateFromSeconds(Runner, randomIdleDuration);
    }

    protected override void OnEnterStateRender()
    {
        anim.CrossFadeInFixedTime(animState, animTransitionLength, 0, 0);
    }
}
