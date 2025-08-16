using UnityEngine;
using Fusion;
using Fusion.Addons.FSM;
public abstract class BossSkillState : BossStateBase
{
    public float stateDuration = 30f; //스킬 상태(페이즈) 지속시간


    protected override void OnEnterState()
    {
        fsmRef.BossNetworkBehaviour.StateTimer = TickTimer.CreateFromSeconds(Runner, stateDuration);
    }
    // OnFixedUpdate에서 스테이트 타이머가 끝났을 때 Idle로 상태전환시키는 함수
    public void CheckExitState()
    {
        if (fsmRef.BossNetworkBehaviour.StateTimer.ExpiredOrNotRunning(Runner))
        {
            fsmRef.BossNetworkBehaviour.CurrentState = BossStateName.Idle;
            fsmRef.StateMachine.ForceActivateState<BossIdleState>();
        }
    }
}
