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

    

    private List<LeaderboardTableItem> _cachedLeaderboardTables = new List<LeaderboardTableItem>();

    /// <summary>
    /// 상위 10위 리더보드 로드
    /// </summary>
    public void LoadTop10(string leaderboardUUID, Action<bool, List<LeaderBoardEntry>> onCompleted)
    {
        if (string.IsNullOrEmpty(leaderboardUUID))
        {
            Debug.LogError("리더보드 UUID가 비어있습니다.");
            onCompleted?.Invoke(false, null);
            return;
        }

        if (GlobalSetting.Inst.IsEnableBackend == false)
        {
            Debug.Log("백앤드 비활성화 상태이므로 리더보드를 불러오지 않습니다.");
            onCompleted?.Invoke(false, null);
            return;
        }

        // 상위 10위 조회
        int limit = 10;
        Backend.URank.User.GetRankList(leaderboardUUID, limit, bro =>
        {
            if (bro.IsSuccess())
            {
                try
                {
                    var rankListJson = bro.GetFlattenJSON();
                    List<LeaderBoardEntry> entries = new List<LeaderBoardEntry>();

                    if (rankListJson["rows"] != null)
                    {
                        foreach (LitJson.JsonData jsonData in rankListJson["rows"])
                        {
                            LeaderBoardEntry entry = new LeaderBoardEntry
                            {
                                Rank = int.Parse(jsonData["Rank"].ToString()),
                                NickName = jsonData["nickName"].ToString(),
                                SessionDurationSec = int.Parse(jsonData["SessionDurationSec"].ToString()),
                                Stage = jsonData["Stage"].ToString(),
                                TotalScore = int.Parse(jsonData["TotalScore"].ToString())
                            };
                            entries.Add(entry);
                        }
                    }

                    Debug.Log($"상위 10위 리더보드 조회 성공 - {entries.Count}개 항목");
                    onCompleted?.Invoke(true, entries);
                }
                catch (Exception e)
                {
                    Debug.LogError($"상위 10위 데이터 파싱 오류: {e.Message}");
                    onCompleted?.Invoke(false, null);
                }
            }
            else
            {
                Debug.LogError($"상위 10위 리더보드 조회 실패: {bro.GetStatusCode()} - {bro.GetErrorMessage()}");
                onCompleted?.Invoke(false, null);
            }
        });
    }

    /// <summary>
    /// 내 랭킹 정보 로드
    /// </summary>
    public void LoadMyRank(string leaderboardUUID, Action<bool, LeaderBoardEntry> onCompleted)
    {
        if (string.IsNullOrEmpty(leaderboardUUID))
        {
            Debug.LogError("리더보드 UUID가 비어있습니다.");
            onCompleted?.Invoke(false, null);
            return;
        }

        if (GlobalSetting.Inst.IsEnableBackend == false)
        {
            Debug.Log("백앤드 비활성화 상태이므로 내 랭킹을 불러오지 않습니다.");
            onCompleted?.Invoke(false, null);
            return;
        }

        Backend.URank.User.GetMyRank(leaderboardUUID, bro =>
        {
            if (bro.IsSuccess())
            {
                try
                {
                    var myRankJson = bro.FlattenRows();
                    if (myRankJson != null && myRankJson.Count > 0)
                    {
                        LitJson.JsonData jsonData = myRankJson[0];
                        LeaderBoardEntry myRank = new LeaderBoardEntry
                        {
                            Rank = int.Parse(jsonData["Rank"].ToString()),
                            NickName = jsonData["NickName"].ToString(),
                            SessionDurationSec = int.Parse(jsonData["SessionDurationSec"].ToString()),
                            Stage = jsonData["Stage"].ToString(),
                            TotalScore = int.Parse(jsonData["TotalScore"].ToString())
                        };

                        Debug.Log($"내 랭킹 조회 성공 - 순위: {myRank.Rank}");
                        onCompleted?.Invoke(true, myRank);
                    }
                    else
                    {
                        Debug.Log("내 랭킹 데이터가 존재하지 않습니다.");
                        onCompleted?.Invoke(false, null);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"내 랭킹 데이터 파싱 오류: {e.Message}");
                    onCompleted?.Invoke(false, null);
                }
            }
            else
            {
                Debug.LogError($"내 랭킹 조회 실패: {bro.GetStatusCode()} - {bro.GetErrorMessage()}");
                onCompleted?.Invoke(false, null);
            }
        });
    }

    /// <summary>
    /// 랭킹 점수 업데이트
    /// </summary>
    public void UpdateUserScore(string leaderboardUUID, int sessionDurationSec, string stage, int totalScore, Action<bool> onCompleted)
    {
        if (string.IsNullOrEmpty(leaderboardUUID))
        {
            Debug.LogError("리더보드 UUID가 비어있습니다.");
            onCompleted?.Invoke(false);
            return;
        }

        if (GlobalSetting.Inst.IsEnableBackend == false)
        {
            Debug.Log("백앤드 비활성화 상태이므로 랭킹을 업데이트하지 않습니다.");
            onCompleted?.Invoke(false);
            return;
        }

        // TODO: 정확한 Backend URank API 시그니처 확인 후 구현
        Debug.LogWarning($"랭킹 업데이트 요청됨 - UUID: {leaderboardUUID}, TotalScore: {totalScore}");
        Debug.LogWarning("Backend URank API의 정확한 시그니처 확인 후 구현이 필요합니다.");
        
        onCompleted?.Invoke(true);
    }

    /// <summary>
    /// PlayerSessionRecord를 사용한 랭킹 점수 업데이트
    /// </summary>
    public void UpdateUserScore(string leaderboardUUID, PlayerSessionRecord sessionRecord, Action<bool> onCompleted)
    {
        if (sessionRecord == null)
        {
            Debug.LogError("세션 레코드가 null입니다.");
            onCompleted?.Invoke(false);
            return;
        }

        UpdateUserScore(leaderboardUUID, sessionRecord.SessionDurationSec, sessionRecord.Stage, sessionRecord.TotalScore, onCompleted);
    }

    /// <summary>
    /// 리더보드 테이블 목록 로드 (간단한 캐시)
    /// </summary>
    public void LoadLeaderboardTables(Action<bool, List<LeaderboardTableItem>> onCompleted)
    {
        // 캐시 확인 - 한번 로드했으면 재사용
        if (_cachedLeaderboardTables.Count > 0)
        {
            Debug.Log("캐시된 리더보드 테이블 목록을 사용합니다.");
            onCompleted?.Invoke(true, new List<LeaderboardTableItem>(_cachedLeaderboardTables));
            return;
        }

        if (GlobalSetting.Inst.IsEnableBackend == false)
        {
            Debug.Log("백앤드 비활성화 상태이므로 리더보드 목록을 불러오지 않습니다.");
            onCompleted?.Invoke(false, null);
            return;
        }

        Backend.Leaderboard.User.GetLeaderboards(bro =>
        {
            if (bro.IsSuccess())
            {
                List<LeaderboardTableItem> list = bro.GetLeaderboardTableList();
                if (list != null && list.Count > 0)
                {
                    // 캐시에 저장
                    _cachedLeaderboardTables.Clear();
                    _cachedLeaderboardTables.AddRange(list);

                    for (int i = 0; i < list.Count; i++)
                    {
                        LeaderboardTableItem item = list[i];
                        Debug.Log($"[리더보드] title={item.title}, uuid={item.uuid}, table={item.table}, column={item.column}, order={item.order}");
                    }

                    Debug.Log($"리더보드 테이블 목록 캐시 완료 - {list.Count}개 테이블");
                }

                onCompleted?.Invoke(true, list);
            }
            else
            {
                Debug.LogError($"리더보드 목록 조회 실패 : {bro.GetStatusCode()} - {bro.GetErrorMessage()}");
                onCompleted?.Invoke(false, null);
            }
        });
    }

    /// <summary>
    /// 캐시된 테이블 목록 가져오기
    /// </summary>
    public List<LeaderboardTableItem> GetCachedTables()
    {
        return new List<LeaderboardTableItem>(_cachedLeaderboardTables);
    }

    /// <summary>
    /// 특정 UUID의 테이블 정보 찾기
    /// </summary>
    public LeaderboardTableItem FindTableByUUID(string uuid)
    {
        for (int i = 0; i < _cachedLeaderboardTables.Count; i++)
        {
            if (_cachedLeaderboardTables[i].uuid == uuid)
            {
                return _cachedLeaderboardTables[i];
            }
        }
        return null;
    }

    /// <summary>
    /// 캐시 초기화
    /// </summary>
    public void ClearCache()
    {
        _cachedLeaderboardTables.Clear();
        Debug.Log("리더보드 테이블 캐시가 초기화되었습니다.");
    }
}