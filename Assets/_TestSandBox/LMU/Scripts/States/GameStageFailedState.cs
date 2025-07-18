using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public class GameStageFailedState : BaseStateBehaviour
{

    [Header("설정")]
    [SerializeField] private float minWaitingTime = 3.0f;

    [Header("디버그용")]
    private TickTimer waitingTimer = TickTimer.None;

    protected override void OnEnterState()
    {
        waitingTimer = TickTimer.CreateFromSeconds(Runner, minWaitingTime);
        RPC_FadeOutUI();
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
        RPC_FadeInUI();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public async void RPC_FadeOutUI()
    {
        UIController.ActiveGameUI(false);
        await Fader.FadeOutAsync();
        UIController.DeactiveAllLobbyUI();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public async void RPC_FadeInUI()
    {
        UIController.ActiveGameUI(true);
        await Fader.FadeInAsync();
    }
} 