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
        public int SessionDurationSec;
        public string Stage;
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
            var myEntry = ConvertToLeaderBoardEntry(myInfo);
            
            Debug.Log($"<color=#00FF00>내 순위 조회 성공: {myEntry.Rank}등</color>");
            onCompleted?.Invoke(true, myEntry, callback);
        });
    }

    /// <summary>
    /// 1등부터 10등까지의 리더보드 순위를 조회
    /// </summary>
    public void GetTop10RankingsAsync(string leaderboardUuid, Action<bool, List<LeaderBoardEntry>, BackendReturnObject> onCompleted)
    {
        if (string.IsNullOrEmpty(leaderboardUuid))
        {
            Debug.LogError("리더보드 UUID가 비어있습니다.");
            onCompleted?.Invoke(false, null, null);
            return;
        }

        int limit = 10;  // 상위 10명
        int offset = 0;  // 1등부터 시작

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
                var entry = ConvertToLeaderBoardEntry(item);
                top10List.Add(entry);
            }

            Debug.Log($"<color=#00FF00>상위 랭킹 조회 성공: {top10List.Count}명</color>");
            onCompleted?.Invoke(true, top10List, callback);
        });
    }

    /// <summary>
    /// UserLeaderboardItem을 LeaderBoardEntry로 변환
    /// </summary>
    private LeaderBoardEntry ConvertToLeaderBoardEntry(UserLeaderboardItem item)
    {
        var entry = new LeaderBoardEntry();
        
        // 기본 정보
        entry.Rank = int.Parse(item.rank);
        entry.NickName = item.nickname;
        entry.TotalScore = int.Parse(item.score);
        
        // extraData 처리 - 뒤끝에서는 보통 JSON이 아닌 단순 값이 올 수 있음
        if (string.IsNullOrEmpty(item.extraData) == false)
        {
            Debug.Log($"extraData 내용: '{item.extraData}'");
            
            // JSON 형태인지 확인 ('{' 로 시작하는지)
            if (item.extraData.StartsWith("{"))
            {
                try
                {
                    var extraJson = LitJson.JsonMapper.ToObject(item.extraData);
                    
                    if (extraJson.ContainsKey("SessionDurationSec"))
                    {
                        entry.SessionDurationSec = int.Parse(extraJson["SessionDurationSec"].ToString());
                    }
                    
                    if (extraJson.ContainsKey("Stage"))
                    {
                        entry.Stage = extraJson["Stage"].ToString();
                    }
                    else
                    {
                        entry.Stage = "Unknown";
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"extraData JSON 파싱 실패: {ex.Message}, 원본: '{item.extraData}'");
                    SetDefaultExtraValues(entry);
                }
            }
            else
            {
                // JSON이 아닌 경우 - extraData를 Stage로 사용하거나 기본값 설정
                Debug.Log($"extraData가 JSON 형태가 아님: '{item.extraData}'");
                entry.Stage = item.extraData; // extraData를 스테이지명으로 사용
                entry.SessionDurationSec = 0;
            }
        }
        else
        {
            SetDefaultExtraValues(entry);
        }
        
        // extraName도 확인해보자 (추가 정보가 있을 수 있음)
        if (string.IsNullOrEmpty(item.extraName) == false)
        {
            Debug.Log($"extraName 내용: '{item.extraName}'");
        }
        
        return entry;
    }
    
    /// <summary>
    /// 기본 extraData 값 설정
    /// </summary>
    private void SetDefaultExtraValues(LeaderBoardEntry entry)
    {
        entry.SessionDurationSec = 0;
        entry.Stage = "Unknown";
    }

}