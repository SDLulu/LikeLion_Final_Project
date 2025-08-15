using System.Collections.Generic;
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

            PlayerM.SoftResetAllPlayers();
           
            // 로비로 이동시 세션정보 초기화, 다른 유저의 네트워크 접속 허용
            LobbyManager.Inst.UpdateSessionInfo(isInGame: false);
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