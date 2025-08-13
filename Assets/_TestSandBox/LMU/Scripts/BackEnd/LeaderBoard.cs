using System;
using System.Collections.Generic;
using BackEnd;
using LMCore;
using UnityEngine;

/// <summary>
/// 뒤끝 리더보드 조회 전용 매니저. 테이블 목록 불러오기를 담당합니다.
/// </summary>
public class LeaderBoard : BaseManager<LeaderBoard>
{
	public class LeaderBoardEntry
	{
		public int Rank;
		public string NickName;
		public int SessionDurationSec;
		public string Stage;
		public int TotalScore;
	}

    /// <summary>
    /// 리더보드 테이블 목록을 불러옵니다.
    /// </summary>
    public void LoadLeaderboardTables(Action<bool, List<BackEnd.Leaderboard.LeaderboardTableItem>> onCompleted)
    {
        if (GlobalSetting.Inst.IsEnableBackend == false)
        {
            Debug.Log("백앤드 비활성화 상태이므로 리더보드 목록을 불러오지 않습니다.");
            if (onCompleted != null)
            {
                onCompleted(false, null);
            }
            return;
        }

        bool initOk = BackEndWorkFlow.Inst.InitBackend();
        if (initOk == false)
        {
            Debug.LogError("뒤끝 초기화 실패로 리더보드 목록을 불러올 수 없습니다.");
            if (onCompleted != null)
            {
                onCompleted(false, null);
            }
            return;
        }

        Backend.Leaderboard.User.GetLeaderboards(bro =>
        {
            if (bro.IsSuccess())
            {
                List<BackEnd.Leaderboard.LeaderboardTableItem> list = bro.GetLeaderboardTableList();
                if (list == null)
                {
                    Debug.Log("리더보드 테이블이 존재하지 않습니다.");
                }
                else
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        BackEnd.Leaderboard.LeaderboardTableItem item = list[i];
                        Debug.Log($"[리더보드] title={item.title}, uuid={item.uuid}, table={item.table}, column={item.column}, order={item.order}");
                    }
                }

                if (onCompleted != null)
                {
                    onCompleted(true, list);
                }
            }
            else
            {
                Debug.LogError($"리더보드 목록 조회 실패 : {bro.GetStatusCode()} - {bro.GetErrorMessage()}");
                if (onCompleted != null)
                {
                    onCompleted(false, null);
                }
            }
        });
    }

    [ContextMenu("리더보드 테이블 목록 불러오기(로그)")]
    private void DebugLoadTables()
    {
        LoadLeaderboardTables((ok, list) =>
        {
            if (ok == false)
            {
                Debug.LogError("리더보드 목록 불러오기 실패");
                return;
            }

            Debug.Log($"리더보드 테이블 수 : {list?.Count ?? 0}");
        });
    }

    /// <summary>
    /// 상위 10개 랭킹 데이터를 불러옵니다. 백엔드 비활성화 시 더미 데이터를 반환합니다.
    /// </summary>
    public void LoadTop10(string leaderboardUuid, Action<bool, List<LeaderBoardEntry>> onCompleted)
    {
        if (string.IsNullOrEmpty(leaderboardUuid))
        {
            if (onCompleted != null)
            {
                onCompleted(false, null);
            }
            return;
        }

        if (GlobalSetting.Inst.IsEnableBackend == false)
        {
            List<LeaderBoardEntry> dummy = BuildDummyTop10();
            if (onCompleted != null)
            {
                onCompleted(true, dummy);
            }
            return;
        }

        bool initOk = BackEndWorkFlow.Inst.InitBackend();
        if (initOk == false)
        {
            if (onCompleted != null)
            {
                onCompleted(false, null);
            }
            return;
        }

		// 실제 호출
		Backend.Leaderboard.User.GetLeaderboard(leaderboardUuid, 1, 10, callback =>
		{
			if (callback.IsSuccess())
			{
				List<LeaderBoardEntry> result = new List<LeaderBoardEntry>();
				LitJson.JsonData json = callback.GetReturnValuetoJSON();
				LitJson.JsonData rows = null;
				if (json != null && json.Keys != null && json.Keys.Contains("rows"))
				{
					rows = json["rows"]; 
				}
				else if (json != null && json.Keys != null && json.Keys.Contains("row"))
				{
					rows = json["row"]; 
				}

				if (rows != null)
				{
					for (int i = 0; i < rows.Count; i++)
					{
						LeaderBoardEntry entry = ParseEntry(rows[i], i);
						result.Add(entry);
					}
				}

				if (onCompleted != null)
				{
					onCompleted(true, result);
				}
			}
			else
			{
				Debug.LogError($"상위 랭킹 조회 실패 : {callback.GetStatusCode()} - {callback.GetErrorMessage()}");
				if (onCompleted != null)
				{
					onCompleted(false, null);
				}
			}
		});
    }

    /// <summary>
    /// 내 랭킹 하나를 불러옵니다. 백엔드 비활성화 시 더미 데이터를 반환합니다.
    /// </summary>
    public void LoadMyRank(string leaderboardUuid, Action<bool, LeaderBoardEntry> onCompleted)
    {
        if (string.IsNullOrEmpty(leaderboardUuid))
        {
            if (onCompleted != null)
            {
                onCompleted(false, null);
            }
            return;
        }

        if (GlobalSetting.Inst.IsEnableBackend == false)
        {
            LeaderBoardEntry dummy = new LeaderBoardEntry
            {
                Rank = 7,
                NickName = BackEndWorkFlow.NickName,
                SessionDurationSec = 987,
                Stage = "Stage-3",
                TotalScore = 1234
            };
            if (onCompleted != null)
            {
                onCompleted(true, dummy);
            }
            return;
        }

        bool initOk = BackEndWorkFlow.Inst.InitBackend();
        if (initOk == false)
        {
            if (onCompleted != null)
            {
                onCompleted(false, null);
            }
            return;
        }

		Backend.Leaderboard.User.GetLeaderboard(leaderboardUuid, callback =>
		{
			if (callback.IsSuccess())
			{
				LitJson.JsonData json = callback.GetReturnValuetoJSON();
				LeaderBoardEntry entry = null;
				if (json != null && json.Keys != null && json.Keys.Contains("row"))
				{
					entry = ParseEntry(json["row"], 0);
				}
				else if (json != null && json.Keys != null && json.Keys.Contains("rows") && json["rows"].Count > 0)
				{
					entry = ParseEntry(json["rows"][0], 0);
				}

				if (entry == null)
				{
					if (onCompleted != null)
					{
						onCompleted(false, null);
					}
					return;
				}

				if (onCompleted != null)
				{
					onCompleted(true, entry);
				}
			}
			else
			{
				Debug.LogError($"내 랭킹 조회 실패 : {callback.GetStatusCode()} - {callback.GetErrorMessage()}");
				if (onCompleted != null)
				{
					onCompleted(false, null);
				}
			}
		});
    }

    private List<LeaderBoardEntry> BuildDummyTop10()
    {
        List<LeaderBoardEntry> list = new List<LeaderBoardEntry>(10);
        for (int i = 0; i < 10; i++)
        {
            LeaderBoardEntry e = new LeaderBoardEntry
            {
                Rank = i + 1,
                NickName = $"User_{i + 1}",
                SessionDurationSec = UnityEngine.Random.Range(100, 5000),
                Stage = $"Stage-{UnityEngine.Random.Range(1, 10)}",
                TotalScore = UnityEngine.Random.Range(1000, 10000)
            };
            list.Add(e);
        }
        return list;
    }

	private LeaderBoardEntry ParseEntry(LitJson.JsonData row, int index)
	{
		LeaderBoardEntry e = new LeaderBoardEntry();

		int rank = index + 1;
		if (row != null && row.Keys != null && row.Keys.Contains("rank"))
		{
			int.TryParse(row["rank"].ToString(), out rank);
		}
		e.Rank = rank;

		string nick = string.Empty;
		if (row != null && row.Keys != null && row.Keys.Contains("nickname"))
		{
			nick = row["nickname"].ToString();
		}
		else if (row != null && row.Keys != null && row.Keys.Contains("NickName"))
		{
			nick = row["NickName"].ToString();
		}
		e.NickName = nick;

		int session = 0;
		if (row != null && row.Keys != null && row.Keys.Contains("SessionDurationSec"))
		{
			int.TryParse(row["SessionDurationSec"].ToString(), out session);
		}
		e.SessionDurationSec = session;

		string stage = string.Empty;
		if (row != null && row.Keys != null && row.Keys.Contains("Stage"))
		{
			stage = row["Stage"].ToString();
		}
		e.Stage = stage;

		int total = 0;
		if (row != null && row.Keys != null && row.Keys.Contains("score"))
		{
			int.TryParse(row["score"].ToString(), out total);
		}
		else
		{
			int item = 0;
			int kill = 0;
			if (row != null && row.Keys != null && row.Keys.Contains("ItemScore"))
			{
				int.TryParse(row["ItemScore"].ToString(), out item);
			}
			if (row != null && row.Keys != null && row.Keys.Contains("KillScore"))
			{
				int.TryParse(row["KillScore"].ToString(), out kill);
			}
			total = item + kill;
		}
		e.TotalScore = total;

		return e;
	}
}


