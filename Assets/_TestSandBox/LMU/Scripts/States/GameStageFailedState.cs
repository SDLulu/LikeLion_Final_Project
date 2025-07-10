using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public class GameStageFailedState : StateBehaviour
{
    public UI_Controller UIController {get; set;}
    public Fader Fader {get; set;}

    [Header("설정")]
    [SerializeField] private float minWaitingTime = 3.0f;

    [Header("디버그용")]
    private TickTimer waitingTimer = TickTimer.None;

    protected override void OnEnterState()
    {
        waitingTimer = TickTimer.CreateFromSeconds(Runner, minWaitingTime);
        RPC_FadeInUI();
    }

    protected override void OnFixedUpdate()
    {
        if(Runner.IsServer && waitingTimer.Expired(Runner))
        {
            Debug.Log($"대기시간 {minWaitingTime}초가 초과되었습니다.");
            Machine.ForceActivateState(Machine.GetState<GameStagePlayingState>());
        }    
        else
        {
            Debug.Log($"남은 대기시간: {waitingTimer.RemainingTime(Runner)}");
        }
    }

    protected override void OnExitState()
    {
        Debug.Log("대기 상태 종료");
        RPC_FadeOutUI();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public async void RPC_FadeInUI()
    {
        UIController.ActiveGameUI(false);
        await Fader.BlackFadeOutAsync();
        UIController.DeactiveAllLobbyUI();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public async void RPC_FadeOutUI()
    {
        UIController.ActiveGameUI(true);
        await Fader.BlackFadeInAsync();
    }
} 