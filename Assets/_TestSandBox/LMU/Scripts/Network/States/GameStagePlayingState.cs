using System.Linq;
using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public class GameStagePlayingState : BaseStateBehaviour
{
    public override E_StateName StateName => E_StateName.GameStagePlayingState;

    private int _stageDataIndex = -1;
    public override void Spawned()
    {
        if (Runner.IsServer)
        {
            _stageDataIndex = DataManager.Inst.StageData.First().Key;
        }
    }

    private bool _isFirstEnter = true;
    protected override async void OnEnterState()
    {
        if (Runner.IsServer)
        {
            await MapLoad();

            if (_isFirstEnter)
            {
                _isFirstEnter = false;
                GameStates.RPC_FadeInUI(this.Runner);
            }
        }
    }

    protected override void OnExitState()
    {
        if (Runner.IsServer)
        {
            _stageDataIndex++;
        }
    }

    public async Awaitable MapLoad()
    {
        await Awaitable.NextFrameAsync();
        await Awaitable.WaitForSecondsAsync(2.0f);
        var stageData = DataManager.Inst.GetStageData(_stageDataIndex);
        NetEvent.TriggerStageLoadDoneEvent(stageData);

        var startPos = new Vector2(15.0f, 15.0f);
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


}