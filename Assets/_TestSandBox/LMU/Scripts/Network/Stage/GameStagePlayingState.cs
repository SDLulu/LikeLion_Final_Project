using System.Linq;
using LMCore;
using UnityEngine;
using Fusion.Addons.FSM;

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
                Debug.LogError("StartPos 태그가 존재하지 않아 진행할수 없습니다.");
                GameStates.RPC_FadeInUI(Runner, 1.0f);
                return;
            }
            PlayerM.SetPlayerPositions(startPos[0].transform.position);
            GameStates.RPC_FadeInUI(Runner, 1.0f);
        }
    }

    protected override void OnExitState()
    {
    }

    protected override void OnFixedUpdate()
    {
        // Note - 플레이어가 탈출문을 트리거를 하는경우 외부에서 강제로 상태가 변경

        // 모든 플레이어가 죽었을경우 실패상태로 전환
        if (Runner.IsServer && PlayerM.IsAllPlayerDead())
        {
            Machine.ForceActivateState(Machine.GetState<GameStageFailedState>());
        }
    }




}