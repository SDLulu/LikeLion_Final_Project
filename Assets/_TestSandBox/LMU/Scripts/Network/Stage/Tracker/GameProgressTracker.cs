using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameProgressTracker : NetworkBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_StageProgress _uiStageProgress;
    [SerializeField] private PlayerScoreTracker _scoreTracker;
	[Networked] public float NetStageElapsedSeconds { get; private set; }
	[Networked] public float NetSessionElapsedSeconds { get; private set; }
	[Networked] public NetworkString<_8> NetStageId { get; private set; }

    private bool _isSessionActive = false;
    private bool _isStageActive = false;
    private bool _isCutSceneActive = false;

    // 서버의 InDade 캐시 기록용
    private Dictionary<PlayerRef, string> _playerInDateMap = new();
    private Dictionary<PlayerRef, int> _playerBestTotalMap = new();

    private E_StateName _curState = E_StateName.None;

    private void SetDefaultData()
    {
        _isSessionActive = true;
        _playerInDateMap.Clear();
        _playerBestTotalMap.Clear();
        NetStageElapsedSeconds = 0.0f;
        NetSessionElapsedSeconds = 0.0f;
        NetStageId = string.Empty;
        _scoreTracker.ClearScore();
    }

    public override void Spawned()
    {
        base.Spawned();
        _isSessionActive = true;

        NetworkEventSystem.Inst.OnGameStateChangedEvent += OnGameStateChanged;
        NetworkEventSystem.Inst.OnStageLoadDoneEvent += OnStageLoadDone;
        NetworkEventSystem.Inst.OnCutSceneActiveEvent += OnCutSceneActive;

        SetDefaultData();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _isSessionActive = false;
        _isStageActive = false;
        _isCutSceneActive = false;
        if (NetworkEventSystem.HasInstance)
        {
            NetworkEventSystem.Inst.OnGameStateChangedEvent -= OnGameStateChanged;
            NetworkEventSystem.Inst.OnStageLoadDoneEvent -= OnStageLoadDone;
            NetworkEventSystem.Inst.OnCutSceneActiveEvent -= OnCutSceneActive;
        }
        base.Despawned(runner, hasState);
    }

    public override void FixedUpdateNetwork()
    {
        if (_curState == E_StateName.LobbyState)
            return;

        if (Object.HasStateAuthority)
        {
            if (_isSessionActive)
            {
                NetSessionElapsedSeconds += Runner.DeltaTime;
            }

            if (_isStageActive)
            {
                NetStageElapsedSeconds += Runner.DeltaTime;
            }
        }
    }

    public override void Render()
    {
        base.Render();
        _uiStageProgress.UpdateTimeData(
            NetSessionElapsedSeconds,
            NetStageElapsedSeconds,
            NetSessionElapsedSeconds,
            NetStageId.ToString()
        );
    }

    private async void OnGameStateChanged(NetworkRunner runner, E_StateName prev, E_StateName current)
    {
        if (runner.IsServer)
        {
            _curState = current;
            // 게임을 실패하면 현재 기록에 대한 정보를 넘기고 데이터 초기화
            if (current == E_StateName.FailedState)
            {
                TrySubmitAllPlayers(runner);
                await Awaitable.WaitForSecondsAsync(2.0f);
                SetDefaultData();
            }
        }
    }

    private void OnStageLoadDone(Stage.Data stageData)
    {
        // 스테이지 시작: 스테이지 타임 시작 및 스테이지 ID 설정
        _isStageActive = true;
        if (Object.HasStateAuthority)
        {
            NetStageElapsedSeconds = 0f;
            NetStageId = stageData.CurrentStage;
        }
    }

    private void OnCutSceneActive(bool isActive)
    {
        _isCutSceneActive = isActive;

        if (isActive)
        {
            // 컷씬 시작 시 스테이지 시간은 초기화 및 정지
            _isStageActive = false;
            if (Object.HasStateAuthority)
            {
                NetStageElapsedSeconds = 0f;
            }
        }
    }


    /// <summary>
    /// 개별 플레이어에 대한 서버 세션 데이터 전송
    /// </summary>
    private void TrySubmitSinglePlayer(NetworkRunner runner, PlayerRef player)
	{
		if (runner.IsServer == false)
			return;

        // 세션정보 초기화
        int item = _scoreTracker != null ? _scoreTracker.GetItemScoreOf(player) : 0;
        int kill = _scoreTracker != null ? _scoreTracker.GetMonsterScoreOf(player) : 0;
		int total = item + kill;
		string stageId = NetStageId.ToString();
		int sessionSec = Mathf.RoundToInt(NetSessionElapsedSeconds);

		var record = new PlayerSessionRecord();
		record.SessionDurationSec = sessionSec;
		record.Stage = stageId;
		record.ItemScore = item;
		record.KillScore = kill;

        // 닉네임
        var playerObj = runner.GetPlayerObject(player);
        if (playerObj != null)
        {
            var pdata = playerObj.GetComponent<PlayerData>();
            if (pdata != null)
                record.NickName = pdata.NickName;
        }

        SubmitBestRecord(player, record, total);
	}

    private void TrySubmitAllPlayers(NetworkRunner runner)
	{
		if (runner.IsServer == false)
			return;

        foreach (var player in runner.ActivePlayers)
			TrySubmitSinglePlayer(runner, player);
	}

    private void SubmitBestRecord(PlayerRef player, PlayerSessionRecord newRecord, int newTotal)
	{
		if (GlobalSetting.Inst.IsEnableBackend == false)
			return;

        // 유저당 1행 정책 - 서버 세션 내에서 inDate 캐시를 사용해 비교 업데이트
        if (_playerInDateMap.ContainsKey(player) == false)
        {
            string tableName = BackEndWorkFlow.Inst.TABLE_NAME;
            UserData.InsertSessionAsync(BackEndWorkFlow.Inst.TABLE_NAME, newRecord, callback =>
            {
                if (callback.IsSuccess())
                {
                    string insertInDate = callback.GetInDate();
                    if (string.IsNullOrEmpty(insertInDate) == false)
                    {
                        _playerInDateMap[player] = insertInDate;
                        _playerBestTotalMap[player] = newTotal;
                        UpdateLeaderboardAfterInsert(insertInDate, newRecord);
                    }
                }
            });
            return;
        }

        int prevTotal = 0;
        if (_playerBestTotalMap.ContainsKey(player))
            prevTotal = _playerBestTotalMap[player];

        if (newTotal > prevTotal)
        {
            string inDate = _playerInDateMap[player];
            string tableName = BackEndWorkFlow.Inst.TABLE_NAME;
            UserData.UpdateSessionAsync(tableName, inDate, newRecord, callback =>
            {
                if (callback.IsSuccess())
                {
                    _playerBestTotalMap[player] = newTotal;
                    
                    // 기존 데이터 업데이트 후 리더보드 업데이트
                    UpdateLeaderboardAfterUpdate(inDate, newRecord);
                }
            });
        }
	}

    /// <summary>
    /// 새 데이터 삽입 후 리더보드 업데이트
    /// </summary>
    private void UpdateLeaderboardAfterInsert(string inDate, PlayerSessionRecord record)
    {
        if (LeaderBoard.HasInstance == false)
        {
            Debug.LogWarning("LeaderBoard 인스턴스가 없습니다.");
            return;
        }

        string leaderboardUuid = BackEndWorkFlow.Inst.LeaderboardUUID;
        string tableName = BackEndWorkFlow.Inst.TABLE_NAME;
        LeaderBoard.Inst.UpdateLeaderboardAsync(leaderboardUuid, tableName, inDate, record, callback =>
        {
            if (callback != null && callback.IsSuccess())
            {
            }
            else
            {
            }
        });
    }

    /// <summary>
    /// 기존 데이터 업데이트 후 리더보드 업데이트
    /// </summary>
    private void UpdateLeaderboardAfterUpdate(string inDate, PlayerSessionRecord record)
    {
        if (LeaderBoard.HasInstance == false)
        {
            Debug.LogWarning("LeaderBoard 인스턴스가 없습니다.");
            return;
        }

        string leaderboardUuid = BackEndWorkFlow.Inst.LeaderboardUUID;
        string tableName = BackEndWorkFlow.Inst.TABLE_NAME;
            LeaderBoard.Inst.UpdateLeaderboardAsync(leaderboardUuid, tableName, inDate, record, callback =>
        {
            if (callback != null && callback.IsSuccess())
            {
                Debug.Log("<color=#00FF00>기존 데이터 업데이트 후 리더보드 갱신 완료</color>");
            }
            else
            {
                Debug.LogError("기존 데이터 업데이트 후 리더보드 갱신 실패: " + callback?.ToString());
            }
        });
    }
}


