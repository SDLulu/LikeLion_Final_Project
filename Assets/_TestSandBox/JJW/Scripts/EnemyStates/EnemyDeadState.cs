using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDeadState : EnemyStateBase
{
    public override EnemyStateName StateName => EnemyStateName.Dead;

    protected override void OnEnterState()
    {

    }

    protected override void OnEnterStateRender()
    {
        anim.CrossFadeInFixedTime(animState, animTransitionLength, 0, 0f);
    }
}
