using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public class GameStageFailedState : BaseStateBehaviour
{
    public override E_StateName StateName => E_StateName.FailedState;

    protected override async void OnEnterState()
    {
        // 현재 씬인 게임씬을 언로드 후 로비상태진입
        string gameSceneName = GlobalSetting.Inst.GameScenePath;
        string lobbySceneName = GlobalSetting.Inst.LobbyScenePath;
        await LevelManager.UnloadSceneAsync(gameSceneName, lobbySceneName);
        StateOwner.DelayForceActiveState<LobbyState>();
    }   

    protected override void OnFixedUpdate()
    {
    }

    protected override void OnExitState()
    {
    }
} 