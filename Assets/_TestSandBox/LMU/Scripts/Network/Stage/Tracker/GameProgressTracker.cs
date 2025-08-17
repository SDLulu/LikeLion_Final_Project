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

    // 사용하지 않음 - 새 구조에서는 매번 데이터베이스에서 최고점수 조회
    // private Dictionary<PlayerRef, string> _playerInDateMap = new();
    // private Dictionary<PlayerRef, int> _playerBestTotalMap = new();

    private E_StateName _curState = E_StateName.None;

    private void SetDefaultData()
    {
        _isSessionActive = true;
        // 세션 기반 캐시는 더 이상 사용하지 않음
        // _playerInDateMap.Clear();
        // _playerBestTotalMap.Clear();
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
                await Awaitable.WaitForSecondsAsync(5.0f);
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



    private void TrySubmitAllPlayers(NetworkRunner runner)
	{
		if (runner.IsServer == false)
			return;

        foreach (var player in runner.ActivePlayers)
			TrySubmitSinglePlayer(runner, player);
	}

    /// <summary>
    /// 개별 플레이어에 대한 서버 세션 데이터 전송
    /// </summary>
    private void TrySubmitSinglePlayer(NetworkRunner runner, PlayerRef player)
	{
		if (runner.IsServer == false)
			return;

        // 세션정보 초기화 - 총점 계산
        int item = _scoreTracker != null ? _scoreTracker.GetItemScoreOf(player) : 0;
        int kill = _scoreTracker != null ? _scoreTracker.GetMonsterScoreOf(player) : 0;
		int total = _scoreTracker != null ? _scoreTracker.GetTotalScoreOf(player) : 0;
		string stageId = NetStageId.ToString();
		int sessionSec = Mathf.RoundToInt(NetSessionElapsedSeconds);

		var record = new PlayerSessionRecord();
		record.SessionDurationSec = sessionSec;
		record.Stage = stageId;
		record.TotalScore = total;

        // 닉네임
        var data = PlayerManager.Inst.GetPlayerData(player);
        if (data != null)
        {
            record.NickName = data.NickName;
            Debug.Log($"<color=#FFFF00> 점수를 기록합니다 - " +
            $"플레이어 닉네임: {record.NickName}" +
            $"총점: {total}" +
            $"스테이지: {stageId}" +
            $"세션: {sessionSec}" + "\n" +
            "</color>");
        }

        SubmitBestRecord(player, record, total);
	}


    private void SubmitBestRecord(PlayerRef player, PlayerSessionRecord newRecord, int newTotal)
	{
		if (GlobalSetting.Inst.IsEnableBackend == false)
		{
            Debug.LogWarning("백엔드가 비활성화되어 점수 제출을 건너뜁니다.");
			return;
		}

        // 항상 새로운 게임 기록을 데이터베이스에 삽입 - 모든 기록 보관
        string tableName = BackEndWorkFlow.Inst.TABLE_NAME;
        UserData.InsertSessionAsync(tableName, newRecord, callback =>
        {
            if (callback.IsSuccess())
            {
                string insertInDate = callback.GetInDate();
                Debug.Log($"<color=#00FF00>게임 기록 저장 성공 - " +
                $"플레이어: {player}" +
                $"InDate: {insertInDate}" +
                $"총점: {newTotal}" +
                "</color>");
                
                // 저장 후 해당 닉네임의 데이터베이스 전체에서 최고 점수로 리더보드 업데이트
                UpdateLeaderboardWithBestScore(newRecord.NickName);
            }
            else
            {
                Debug.LogError($"[GameProgressTracker] 게임 기록 저장 실패 - Player: {player}, Error: {callback}");
            }
        });
	}

    /// <summary>
    /// 닉네임의 데이터베이스 전체 기록 중 최고 점수로 리더보드 업데이트
    /// </summary>
    private void UpdateLeaderboardWithBestScore(string nickName)
    {
        if (string.IsNullOrEmpty(nickName))
            return;

        string tableName = BackEndWorkFlow.Inst.TABLE_NAME;
        
        UserData.GetBestScoreByNickNameAsync(tableName, nickName, (success, bestRecord, callback) =>
        {
            if (success && bestRecord != null)
            {
                
                // 최고 점수 기록으로 리더보드 업데이트
                string leaderboardUuid = BackEndWorkFlow.Inst.LeaderboardUUID;
                LeaderBoard.Inst.UpdateLeaderboardAsync(leaderboardUuid, tableName, bestRecord.InDate, bestRecord, leaderboardCallback =>
                {
                    if (leaderboardCallback != null && leaderboardCallback.IsSuccess())
                    {
                        Debug.Log($"{nickName} 최고점수 {bestRecord.TotalScore}로 리더보드 업데이트 성공</color>");
                    }
                    else
                    {
                        Debug.Log($"{nickName} 최고점수 미달 리더보드 업데이트 실패 -  {leaderboardCallback?.ToString()}");
                    }
                });
            }
            else
            {
                Debug.LogError($"{nickName} 최고 점수 조회 실패: {callback?.ToString()}");
            }
        });
    }
}


