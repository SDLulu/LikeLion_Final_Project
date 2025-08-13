using System.Collections.Generic;
using UnityEngine;
using LMCore;

public class UI_LeaderBoard : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private List<UI_LeaderBoardSlot> _leaderBoardSlots;
    [SerializeField] private UI_LeaderBoardSlot _mySlot;

    [Header("리더보드 식별자")]
    [SerializeField] private string _leaderboardUUID;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(_leaderboardUUID))
        {
            Debug.LogError("리더보드 UUID 가 설정되지 않았습니다.");
            return;
        }

        // 상위 10개 불러오기
        LeaderBoard.Inst.LoadTop10(_leaderboardUUID, OnLoadedTop10);
        // 내 랭킹 불러오기
        LeaderBoard.Inst.LoadMyRank(_leaderboardUUID, OnLoadedMyRank);
    }

    private void OnLoadedTop10(bool ok, List<LeaderBoard.LeaderBoardEntry> list)
    {
        if (ok == false)
        {
            if (_leaderBoardSlots != null)
            {
                for (int i = 0; i < _leaderBoardSlots.Count; i++)
                {
                    _leaderBoardSlots[i]?.ClearSlot();
                }
            }
            return;
        }

        int count = Mathf.Min(_leaderBoardSlots?.Count ?? 0, list?.Count ?? 0);
        for (int i = 0; i < count; i++)
        {
            LeaderBoard.LeaderBoardEntry e = list[i];
            _leaderBoardSlots[i].SetSlot(e.Rank, e.NickName, e.SessionDurationSec, e.Stage, e.TotalScore);
        }

        if (_leaderBoardSlots != null)
        {
            for (int i = count; i < _leaderBoardSlots.Count; i++)
            {
                _leaderBoardSlots[i]?.ClearSlot();
            }
        }
    }

    private void OnLoadedMyRank(bool ok, LeaderBoard.LeaderBoardEntry e)
    {
        if (ok == false)
        {
            _mySlot?.ClearSlot();
            return;
        }

        _mySlot?.SetSlot(e.Rank, e.NickName, e.SessionDurationSec, e.Stage, e.TotalScore);
    }
}
