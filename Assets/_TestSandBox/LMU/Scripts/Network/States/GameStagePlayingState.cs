using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public class GameStagePlayingState : BaseStateBehaviour
{
    public override E_StateName StateName => E_StateName.GameStagePlayingState;

    protected override async void OnEnterState()
    {
        if (Runner.IsServer)
        {
            await MapLoad();
            GameStates.RPC_FadeInUI(this.Runner);
        }
    }

    public async Awaitable MapLoad()
    {
        await Awaitable.NextFrameAsync();
        await Awaitable.WaitForSecondsAsync(2.0f);
        NetEvent.TriggerStageLoadDoneEvent("1-1");

        var startPos = Vector2.zero;
        foreach (var player in PlayerM.Players)
        {
            var playerC = player.Value.GetComponent<PlayerStageController>();
            playerC.SetPosition(startPos);
        }
    }

    protected override void OnFixedUpdate()
    {
        // Todo - 1. 모든 플레이어가 죽었는지를 확인후 FailedState로 이동
        // Todo - 2. 한명의 플레이어라도 완료했는지의 여부를 확인후 CompletedState로 이동
    }

    protected override void OnExitState()
    {
    }
}