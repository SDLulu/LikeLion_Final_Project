using System.Linq;
using LMCore;
using UnityEngine;

public class GameStagePlayingState : BaseStateBehaviour
{
    public override E_StateName StateName => E_StateName.PlayingState;

    private bool _isFirst = true;
    protected override void OnEnterState()
    {
        if (Runner.IsServer)
        {
            if (_isFirst)
            {
                PlayerM.SoftResetAllPlayers(GlobalSetting.Inst.LobbySpawnPos);
                _isFirst = false;
            }

            var startPos = GameObject.FindGameObjectsWithTag("StartPos").ToList();
            PlayerM.SetPlayerPositions(startPos[0].transform.position);
            GameStates.RPC_FadeInUI(Runner, 1.0f);
        }
    }

    protected override void OnExitState()
    {
    }



    protected override void OnFixedUpdate()
    {
    }


}