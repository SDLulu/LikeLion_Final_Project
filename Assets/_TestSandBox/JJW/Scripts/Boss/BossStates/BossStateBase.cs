using UnityEngine;
using Fusion.Addons.FSM;

public abstract class BossStateBase : StateBehaviour
{
    public abstract BossStateName StateName { get;}
    public BossFSM fsmRef; //State 변환 및 fsm 관련된 행동을 위한 레퍼런스
    public BossBase boss;
    public Animator anim; //애니메이터
    public string animState;
    public float animTransitionLength = 4f / 60f;
}
