using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameProgressTracker : NetworkBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_StageProgress _uiStageProgress;
    [SerializeField] private PlayerScoreTracker _scoreTracker;
	[Networked, OnChangedRender(nameof(OnChangedData))] public float NetStageElapsedSeconds { get; private set; }
	[Networked, OnChangedRender(nameof(OnChangedData))] public float NetSessionElapsedSeconds { get; private set; }
	[Networked, OnChangedRender(nameof(OnChangedData))] public NetworkString<_8> NetStageId { get; private set; }

    private bool _isSessionActive = false;
    private bool _isStageActive = false;
    private bool _isCutSceneActive = false;

    private E_StateName _curState = E_StateName.None;

    private void SetDefaultData()
    {
        _isSessionActive = true;
        NetStageElapsedSeconds = 0.0f;
        NetSessionElapsedSeconds = 0.0f;
        NetStageId = string.Empty;
        _scoreTracker.ClearScore();
    }

    public event Action OnChangedDataEvent;
    public void OnChangedData()
    {
        OnChangedDataEvent?.Invoke();
    }

    public void AddOnChangedDataEvent(Action action)
    {
        OnChangedDataEvent += action;
    }

    public void RemoveOnChangedDataEvent(Action action)
    {
        OnChangedDataEvent -= action;
    }

    public override void Spawned()
    {
        base.Spawned();
        _isSessionActive = true;
        LobbyManager.Inst.ProgressTracker = this;
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
        LobbyManager.Inst.ProgressTracker = null;
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
            if (current == E_StateName.FailedState || current == E_StateName.EmptyState)
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

    private async void TrySubmitAllPlayers(NetworkRunner runner)
	{
		if (runner.IsServer == false)
			return;

        // 모든 플레이어의 기록을 수집
        var playerRecords = new List<(PlayerRef player, PlayerSessionRecord record)>();
        foreach (var player in runner.ActivePlayers)
        {
            var record = CollectPlayerRecord(runner, player);
            if (record != null)
            {
                playerRecords.Add((player, record));
            }
        }

        // 순차적으로 리더보드 업데이트 처리
        await ProcessPlayerRecordsSequentially(playerRecords);
	}

    /// <summary>
    /// 플레이어 기록 수집
    /// </summary>
    private PlayerSessionRecord CollectPlayerRecord(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer == false)
            return null;

        // 세션정보 초기화 - 총점 계산 및 닉네임 가져오기
        int item = _scoreTracker != null ? _scoreTracker.GetItemScoreOf(player) : 0;
        int kill = _scoreTracker != null ? _scoreTracker.GetMonsterScoreOf(player) : 0;
        int total = _scoreTracker != null ? _scoreTracker.GetTotalScoreOf(player) : 0;
        string stageId = NetStageId.ToString();
        int sessionSec = Mathf.RoundToInt(NetSessionElapsedSeconds);

        var data = PlayerManager.Inst.GetPlayerData(player);
        if (data == null)
            return null;

        var record = new PlayerSessionRecord(data.NickName);
        record.SessionDurationSec = sessionSec;
        record.Stage = stageId;
        record.TotalScore = total;

        Debug.Log($"<color=#FFFF00> 플레이어 기록 수집 - " +
        $"플레이어 닉네임: {record.NickName} " +
        $"총점: {total} " +
        $"스테이지: {stageId} " +
        $"세션: {sessionSec}" +
        "</color>");
        
        return record;
    }

    /// <summary>
    /// 플레이어 기록들을 순차적으로 처리
    /// </summary>
    private async Awaitable ProcessPlayerRecordsSequentially(List<(PlayerRef player, PlayerSessionRecord record)> playerRecords)
    {
        foreach (var (player, record) in playerRecords)
        {
            // 각 클라이언트가 자신의 계정으로 직접 제출하도록 요청
            RPC_RequestClientSubmitRecord(
                player,
                record.NickName,
                record.SessionDurationSec,
                record.Stage,
                record.TotalScore
            );

            await Awaitable.WaitForSecondsAsync(0.25f);
        }
    }

    /// <summary>
    /// 단일 플레이어 기록을 비동기로 처리
    /// </summary>
    private async Awaitable ProcessSinglePlayerRecordAsync(PlayerRef player, PlayerSessionRecord record)
    {
        if (GlobalSetting.Inst.IsEnableBackend == false)
        {
            Debug.LogWarning("백엔드가 비활성화되어 점수 제출을 건너뜁니다.");
            return;
        }

        // 데이터베이스에 기록 저장을 비동기 대기
        var saveCompleted = new AwaitableCompletionSource<bool>();
        
        string tableName = BackEndWorkFlow.Inst.TABLE_NAME;
        UserData.InsertSessionAsync(tableName, record, callback =>
        {
            if (callback.IsSuccess())
            {
                string insertInDate = callback.GetInDate();
                Debug.Log($"<color=#00FF00>게임 기록 저장 성공 - " +
                $"플레이어: {player} " +
                $"InDate: {insertInDate} " +
                $"총점: {record.TotalScore}" +
                "</color>");
                
                saveCompleted.TrySetResult(true);
            }
            else
            {
                Debug.LogError($"[GameProgressTracker] 게임 기록 저장 실패 - Player: {player}, Error: {callback}");
                saveCompleted.TrySetResult(false);
            }
        });

        bool saveSuccess = await saveCompleted.Awaitable;
        
        if (saveSuccess)
        {
            // 서버에서 직접 리더보드 업데이트 (클라이언트 닉네임 포함)
            await UpdateLeaderboardWithBestScoreAsync(record.NickName);
        }
    }



    /// <summary>
    /// 서버가 각 클라이언트에게 자신의 기록 제출을 요청 (Target: All, 클라에서 필터)
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RequestClientSubmitRecord(
        PlayerRef targetPlayer,
        string nickName,
        int sessionDurationSec,
        string stageId,
        int totalScore
    )
    {
        // 요청 대상이 아닌 클라에서는 무시
        if (Runner.LocalPlayer == targetPlayer)
        {
            var record = new PlayerSessionRecord(nickName);
            record.SessionDurationSec = sessionDurationSec;
            record.Stage = stageId;
            record.TotalScore = totalScore;

            _ = ClientInsertAndUpdateLeaderboardAsync(record);
        }
    }

    /// <summary>
    /// 클라이언트에서 자신의 계정으로 기록 저장 및 리더보드 업데이트
    /// </summary>
    private async Awaitable ClientInsertAndUpdateLeaderboardAsync(PlayerSessionRecord record)
    {
        if (GlobalSetting.Inst.IsEnableBackend == false)
        {
            return;
        }

        var saveCompleted = new AwaitableCompletionSource<bool>();
        string tableName = BackEndWorkFlow.Inst.TABLE_NAME;

        UserData.InsertSessionAsync(tableName, record, callback =>
        {
            if (callback.IsSuccess())
            {
                saveCompleted.TrySetResult(true);
            }
            else
            {
                Debug.LogError($"[GameProgressTracker] 클라이언트 기록 저장 실패 - Error: {callback}");
                saveCompleted.TrySetResult(false);
            }
        });

        bool saveSuccess = await saveCompleted.Awaitable;

        if (saveSuccess)
        {
            await UpdateLeaderboardWithBestScoreAsync(record.NickName);
        }
    }

    /// <summary>
    /// 닉네임의 데이터베이스 전체 기록 중 최고 점수로 리더보드 업데이트
    /// </summary>
    private async Awaitable UpdateLeaderboardWithBestScoreAsync(string clientNickName)
    {
        if (string.IsNullOrEmpty(clientNickName))
            return;

        string tableName = BackEndWorkFlow.Inst.TABLE_NAME;
        
        // 최고 점수 조회를 비동기 대기
        var bestScoreCompleted = new AwaitableCompletionSource<(bool success, PlayerSessionRecord record)>();
        
        UserData.GetBestScoreByNickNameAsync(tableName, clientNickName, (success, bestRecord, callback) =>
        {
            bestScoreCompleted.TrySetResult((success, bestRecord));
        });

        var (bestScoreSuccess, bestRecord) = await bestScoreCompleted.Awaitable;
        
        if (bestScoreSuccess && bestRecord != null)
        {
            // 리더보드 업데이트를 비동기 대기
            var leaderboardCompleted = new AwaitableCompletionSource<bool>();
            string leaderboardUuid = BackEndWorkFlow.Inst.LeaderboardUUID;
            
            var leaderboardRecord = new PlayerSessionRecord(clientNickName);
            leaderboardRecord.SessionDurationSec = bestRecord.SessionDurationSec;
            leaderboardRecord.Stage = bestRecord.Stage;
            leaderboardRecord.TotalScore = bestRecord.TotalScore;
            leaderboardRecord.InDate = bestRecord.InDate;
            
            LeaderBoard.Inst.UpdateLeaderboardAsync(leaderboardUuid, tableName, bestRecord.InDate, leaderboardRecord, leaderboardCallback =>
            {
                if (leaderboardCallback != null && leaderboardCallback.IsSuccess())
                {
                    Debug.Log($"<color=#00FF00>[서버] {clientNickName} 최고점수 {bestRecord.TotalScore}로 리더보드 업데이트 성공</color>");
                    leaderboardCompleted.TrySetResult(true);
                }
                else
                {
                    Debug.LogWarning($"[서버] {clientNickName} 리더보드 업데이트 실패 - {leaderboardCallback?.ToString()}");
                    leaderboardCompleted.TrySetResult(false);
                }
            });

            await leaderboardCompleted.Awaitable;
        }
        else
        {
            Debug.LogError($"[서버] {clientNickName} 최고 점수 조회 실패");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ResetStage()
    {
        _isStageActive = false;
        NetStageElapsedSeconds = 0f;
    }
}


