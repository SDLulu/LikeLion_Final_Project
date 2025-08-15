using Fusion.Addons.FSM;
using UnityEngine;

public abstract class EnemyStateBase : StateBehaviour
{
    public EnemyFSM fsmRef; //State 변환 및 fsm 관련된 행동을 위한 레퍼런스
    public Animator anim; //애니메이터
    public abstract EnemyStateName StateName { get;}
    public string animState;
    public float animTransitionLength = 4f / 60f;
}
