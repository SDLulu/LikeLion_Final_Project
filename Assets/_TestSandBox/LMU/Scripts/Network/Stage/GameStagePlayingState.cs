using System.Linq;
using Fusion;
using Fusion.Addons.FSM;
using LMCore;
using UnityEngine;

public class GameStagePlayingState : BaseStateBehaviour
{
    public override E_StateName StateName => E_StateName.PlayingState;

    protected override void OnEnterState()
    {
        if (Runner.IsServer)
        {
            var startPos = GameObject.FindGameObjectsWithTag("StartPos").ToList();
            if (startPos == null || startPos.Count <= 0)
            {
                Debug.LogError("시작 위치가 없습니다.");
                return;
            }

            var players = PlayerM.GetPlayers();
            if (players.Count <= 0)
            {
                Debug.LogError("플레이어가 없습니다.");
                return;
            }

            foreach (var player in players)
            {
                var playerC = player.Value.GetComponent<PlayerStageController>();
                playerC.SetPosition(startPos[0].transform.position);
            }

            // 모든 로딩이 완료된 후 화면을 밝힘
            GameStates.RPC_FadeInUI(this.Runner);
        }
    }

    protected override void OnExitState()
    {
    }



    protected override void OnFixedUpdate()
    {
        // Todo - 1. 모든 플레이어가 죽었는지를 확인후 FailedState로 이동
        // Todo - 2. 한명의 플레이어라도 완료했는지의 여부를 확인후 CompletedState로 이동
    }


}