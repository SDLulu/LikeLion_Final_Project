using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public class GameStageWaitingState : BaseStateBehaviour
{
    public override E_StateName StateName => E_StateName.GameStageWaitingState;
    [Header("설정")]
    [SerializeField] private float minWaitingTime = 1.5f;

    [Header("디버그용")]
    private TickTimer waitingTimer = TickTimer.None;

    protected override void OnEnterState()
    {
        waitingTimer = TickTimer.CreateFromSeconds(Runner, minWaitingTime);
        GameStates.RPC_FadeOutUI(this.Runner);
    }

    protected override void OnFixedUpdate()
    {
        if(Runner.IsServer && waitingTimer.Expired(Runner))
        {
            Debug.Log($"대기시간 {minWaitingTime}초가 초과되었습니다.");
            Machine.ForceActivateState(Machine.GetState<GameStagePlayingState>());
        }    
    }

    protected override void OnExitState()
    {
        Debug.Log("대기 상태 종료");
        GameStates.RPC_FadeInUI(this.Runner);
    }



} 