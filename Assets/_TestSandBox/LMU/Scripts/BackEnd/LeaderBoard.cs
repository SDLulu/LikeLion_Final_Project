using System;
using System.Collections.Generic;
using BackEnd;
using BackEnd.Leaderboard;
using LMCore;
using UnityEngine;

public class LeaderBoard : BaseManager<LeaderBoard>
{
    public class LeaderBoardEntry
    {
        public int Rank;
        public string NickName;
        public int TotalScore;
    }

    /// <summary>
    /// 리더보드 갱신 함수, PlayerSessionRecord 데이터로 리더보드를 업데이트
    /// </summary>
    public void UpdateLeaderboardAsync(string leaderboardUuid, string tableName, string rowInDate, PlayerSessionRecord sessionRecord, Action<BackendReturnObject> onCompleted)
    {
        if (string.IsNullOrEmpty(leaderboardUuid))
        {
            Debug.LogError("리더보드 UUID가 비어있습니다.");
            onCompleted?.Invoke(null);
            return;
        }

        if (string.IsNullOrEmpty(tableName))
        {
            Debug.LogError("테이블 이름이 비어있습니다.");
            onCompleted?.Invoke(null);
            return;
        }

        if (string.IsNullOrEmpty(rowInDate))
        {
            Debug.LogError("행 inDate가 비어있습니다.");
            onCompleted?.Invoke(null);
            return;
        }

        if (sessionRecord == null)
        {
            Debug.LogError("세션 기록이 null입니다.");
            onCompleted?.Invoke(null);
            return;
        }

        Param param = sessionRecord.ToParam();
        
        Backend.Leaderboard.User.UpdateMyDataAndRefreshLeaderboard(leaderboardUuid, tableName, rowInDate, param, callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("<color=#00FF00>리더보드 갱신 성공</color>");
            }
            else
            {
                Debug.LogError("리더보드 갱신 실패 : " + callback);
            }

            onCompleted?.Invoke(callback);
        });
    }

    /// <summary>
    /// 내 리더보드 순위와 데이터를 조회
    /// </summary>
    public void GetMyRankAsync(string leaderboardUuid, Action<bool, LeaderBoardEntry, BackendReturnObject> onCompleted)
    {
        if (string.IsNullOrEmpty(leaderboardUuid))
        {
            Debug.LogError("리더보드 UUID가 비어있습니다.");
            onCompleted?.Invoke(false, null, null);
            return;
        }

        Backend.Leaderboard.User.GetMyLeaderboard(leaderboardUuid, callback =>
        {
            if (callback.IsSuccess() == false)
            {
                Debug.LogError("내 순위 조회 실패 : " + callback);
                onCompleted?.Invoke(false, null, callback);
                return;
            }

            var userLeaderboardList = callback.GetUserLeaderboardList();
            if (userLeaderboardList == null || userLeaderboardList.Count == 0)
            {
                Debug.Log("리더보드에 내 정보가 없습니다.");
                onCompleted?.Invoke(false, null, callback);
                return;
            }

            // 내 정보는 첫 번째 항목
            var myInfo = userLeaderboardList[0];
            var myEntry = ConvertToLeaderBoardEntry(myInfo, callback);
            
            Debug.Log($"<color=#00FF00>내 순위 조회 성공: {myEntry.Rank}등</color>");
            onCompleted?.Invoke(true, myEntry, callback);
        });
    }

    public void GetTop10RankingsAsync(string leaderboardUuid, Action<bool, List<LeaderBoardEntry>, BackendReturnObject> onCompleted)
    {
        if (string.IsNullOrEmpty(leaderboardUuid))
        {
            Debug.LogError("리더보드 UUID가 비어있습니다.");
            onCompleted?.Invoke(false, null, null);
            return;
        }

        // 상위 10명 조회
        int limit = 10;
        int offset = 0;

        Backend.Leaderboard.User.GetLeaderboard(leaderboardUuid, limit, offset, callback =>
        {
            if (callback.IsSuccess() == false)
            {
                Debug.LogError("상위 랭킹 조회 실패 : " + callback);
                onCompleted?.Invoke(false, null, callback);
                return;
            }

            var userLeaderboardList = callback.GetUserLeaderboardList();
            if (userLeaderboardList == null)
            {
                Debug.Log("리더보드가 비어있습니다.");
                onCompleted?.Invoke(true, new List<LeaderBoardEntry>(), callback);
                return;
            }

            List<LeaderBoardEntry> top10List = new List<LeaderBoardEntry>();
            foreach (var item in userLeaderboardList)
            {
                var entry = ConvertToLeaderBoardEntry(item, callback);
                top10List.Add(entry);
            }

            Debug.Log($"<color=#00FF00>상위 랭킹 조회 성공: {top10List.Count}명</color>");
            onCompleted?.Invoke(true, top10List, callback);
        });
    }

    /// <summary>
    /// UserLeaderboardItem을 LeaderBoardEntry로 변환하는 함수
    /// </summary>
    private LeaderBoardEntry ConvertToLeaderBoardEntry(UserLeaderboardItem item, BackendReturnObject callback)
    {
        var entry = new LeaderBoardEntry();
        
        entry.Rank = int.Parse(item.rank);
        
        // 추가 정보에서 실제 클라이언트 닉네임을 가져오기
        string actualNickName = GetActualNickNameFromExtra(item, callback);
        entry.NickName = string.IsNullOrEmpty(actualNickName) ? item.nickname : actualNickName;
        
        entry.TotalScore = int.Parse(item.score);
        
        return entry;
    }

    /// <summary>
    /// 리더보드 아이템의 추가 정보에서 실제 닉네임을 추출
    /// </summary>
    private string GetActualNickNameFromExtra(UserLeaderboardItem item, BackendReturnObject callback)
    {
        try
        {
            var jsonData = callback.GetReturnValuetoJSON();
            if (jsonData != null && jsonData["rows"] != null)
            {
                var rows = jsonData["rows"];
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    // 같은 gamerInDate를 가진 항목 찾기 / 해당 행에서 NickName 필드 찾기
                    if (row["gamerInDate"].ToString() == item.gamerInDate)
                    {
                        if (row.ContainsKey("NickName"))
                        {
                            string actualNickName = row["NickName"].ToString();
                            return actualNickName;
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"추가 정보에서 닉네임 추출 실패: {ex.Message}");
        }

        return string.Empty;
    }
    


}