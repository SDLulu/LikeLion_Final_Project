using Fusion;
using UnityEngine;

public class EnemyStunState : EnemyStateBase
{
    public override EnemyStateName StateName => EnemyStateName.Stun;

     protected override void OnEnterState()
    {
    }

    protected override void OnEnterStateRender()
    {
        anim.CrossFadeInFixedTime(animState, animTransitionLength, 0, 0);
    }
}
