using System.Collections.Generic;
using UnityEngine;
using LMCore;

public class UI_LeaderBoard : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private List<UI_LeaderBoardSlot> _leaderBoardSlots;
    [SerializeField] private UI_LeaderBoardSlot _mySlot;

    private string _leaderboardUUID;

    private void Awake()
    {
        _leaderboardUUID = BackEndWorkFlow.Inst.LeaderboardUUID;
    }

    private void OnEnable()
    {
        _leaderboardUUID = BackEndWorkFlow.Inst.LeaderboardUUID;
        if (string.IsNullOrEmpty(_leaderboardUUID))
        {
            Debug.LogError("리더보드 UUID 가 설정되지 않았습니다.");
            return;
        }

        LeaderBoard.Inst.LoadTop10(_leaderboardUUID, OnLoadedTop10);
        LeaderBoard.Inst.LoadMyRank(_leaderboardUUID, OnLoadedMyRank);
    }

    private void OnLoadedTop10(bool ok, List<LeaderBoard.LeaderBoardEntry> list)
    {
        if (ok == false)
        {
            Debug.LogError("리더보드 목록 조회 실패");
            return;
        }

        if (_leaderBoardSlots == null || _leaderBoardSlots.Count <= 0)
        {
            Debug.LogError("리더보드 목록 조회 실패 - 슬롯이 존재하지 않음");
            return;
        }

        if (list == null || list.Count <= 0)
        {
            Debug.LogError("리더보드 목록 조회 실패 - 데이터가 존재하지 않음");
            return;
        }

        for (int i = 0; i < _leaderBoardSlots.Count; i++)
        {
            _leaderBoardSlots[i]?.ClearSlot();
        }

        // 개수만큼 슬롯 업데이트
        int count = Mathf.Min(_leaderBoardSlots.Count, list.Count);
        for (int i = 0; i < count; i++)
        {
            LeaderBoard.LeaderBoardEntry e = list[i];
            _leaderBoardSlots[i]?.UpdateSlot(e.Rank, e.NickName, e.SessionDurationSec, e.Stage, e.TotalScore);
        }

        // 남은 슬롯 초기화
        for (int i = count; i < _leaderBoardSlots.Count; i++)
        {
            _leaderBoardSlots[i]?.ClearSlot();
        }
    }

    private void OnLoadedMyRank(bool ok, LeaderBoard.LeaderBoardEntry e)
    {
        if (ok == false)
        {
            _mySlot?.ClearSlot();
            return;
        }

        if (e == null)
        {
            Debug.LogError("리더보드 내 랭킹 조회 실패 - 데이터가 존재하지 않음");
            return;
        }

        _mySlot?.UpdateSlot(e.Rank, e.NickName, e.SessionDurationSec, e.Stage, e.TotalScore);
    }
}
