using System.Collections.Generic;
using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public class GameStageFailedState : BaseStateBehaviour
{
    public override E_StateName StateName => E_StateName.FailedState;

    private TickTimer _fadeOutTimer = TickTimer.None;
    private const float _fadeOutTime = 2.0f;
    protected override void OnEnterState()
    {
        if (Runner.IsServer)
        {
            GameStates.RPC_FadeOutUI(this.Runner, 1.0f);
            GameStates.RPC_FadeOutBGM(this.Runner, false);
            _fadeOutTimer = TickTimer.CreateFromSeconds(Runner, _fadeOutTime);

            // 로비로 이동시 세션정보 초기화, 다른 유저의 네트워크 접속 허용
            LobbyManager.Inst.UpdateSessionInfo(isInGame: false);
            StateOwner.DelayForceActiveState<LobbyState>();
        }
    }   

    protected override void OnFixedUpdate()
    {
        if (Runner.IsServer && _fadeOutTimer.ExpiredOrNotRunning(Runner))
        {
            StateOwner.DelayForceActiveState<LobbyState>();
        }
    }

    protected override void OnExitState()
    {
        PlayerM.SoftResetAllPlayers(GlobalSetting.Inst.LobbySpawnPos);
    }

} 