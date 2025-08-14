using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public class GameStageFailedState : BaseStateBehaviour
{
    public override E_StateName StateName => E_StateName.FailedState;

    protected override async void OnEnterState()
    {
        if (Runner.IsServer)
        {
            GameStates.RPC_FadeOutUI(this.Runner, 1.0f);
            await Awaitable.WaitForSecondsAsync(2.0f);
            var sessionProperties = new System.Collections.Generic.Dictionary<string, SessionProperty>();
            sessionProperties["InGame"] = false;
            Runner.SessionInfo.UpdateCustomProperties(sessionProperties);
            StateOwner.DelayForceActiveState<LobbyState>();
        }
    }   

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnExitState()
    {
    }
} 