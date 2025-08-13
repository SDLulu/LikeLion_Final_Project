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
    private Dictionary<PlayerRef, string> _playerInDateMap = new Dictionary<PlayerRef, string>();
    private Dictionary<PlayerRef, int> _playerBestTotalMap = new Dictionary<PlayerRef, int>();

    public override void Spawned()
    {
        base.Spawned();
        _isSessionActive = true;

        NetworkEventSystem.Inst.OnSceneLoadDoneEvent += OnSceneLoadDone;
        NetworkEventSystem.Inst.OnGameStateChangedEvent += OnGameStateChanged;
        NetworkEventSystem.Inst.OnStageLoadDoneEvent += OnStageLoadDone;
        NetworkEventSystem.Inst.OnCutSceneActiveEvent += OnCutSceneActive;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _isSessionActive = false;
        _isStageActive = false;
        _isCutSceneActive = false;
        if (NetworkEventSystem.HasInstance)
        {
            NetworkEventSystem.Inst.OnSceneLoadDoneEvent -= OnSceneLoadDone;
            NetworkEventSystem.Inst.OnGameStateChangedEvent -= OnGameStateChanged;
            NetworkEventSystem.Inst.OnStageLoadDoneEvent -= OnStageLoadDone;
            NetworkEventSystem.Inst.OnCutSceneActiveEvent -= OnCutSceneActive;
        }
        base.Despawned(runner, hasState);
    }

    public override void FixedUpdateNetwork()
    {
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

    private void OnSceneLoadDone(NetworkRunner runner, string sceneName)
    {
        // 세션 타임은 게임 시작부터 종료까지 계속 진행
        _isSessionActive = true;
    }

    private void OnGameStateChanged(NetworkRunner runner, E_StateName prev, E_StateName curr)
    {
        if (runner.IsServer)
        {
            if (curr == E_StateName.FailedState)
            {
                TrySubmitAllPlayers(runner, true);
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
        else
        {
            // 컷씬 종료 후에는 다음 스테이지 진행 중일 수 있으므로 스테이지 타임 재개는
            // 스테이지 로드 완료 이벤트에서 보장
        }
    }


    private void TrySubmitSinglePlayer(NetworkRunner runner, PlayerRef player)
	{
		if (runner.IsServer == false)
		{
			return;
		}

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
        // 닉네임은 네트워크 플레이어 데이터에서 가져옵니다
        var playerObj = runner.GetPlayerObject(player);
        if (playerObj != null)
        {
            var pdata = playerObj.GetComponent<PlayerData>();
            if (pdata != null)
            {
                record.NickName = pdata.NickName;
            }
        }

        SubmitBestRecord(player, record, total);
	}

    private void TrySubmitAllPlayers(NetworkRunner runner, bool forceInsert)
	{
		if (runner.IsServer == false)
		{
			return;
		}

        foreach (var kv in runner.ActivePlayers)
		{
			TrySubmitSinglePlayer(runner, kv);
		}
	}

    private void SubmitBestRecord(PlayerRef player, PlayerSessionRecord newRecord, int newTotal)
	{
		if (GlobalSetting.Inst.IsEnableBackend == false)
		{
			return;
		}

        // 유저당 1행 정책: 서버 세션 내에서는 inDate 캐시를 사용해 비교 업데이트
		string table = "PlayerSession";
        if (_playerInDateMap.ContainsKey(player) == false)
		{
            string inserted = UserData.InsertSession(table, newRecord);
            if (string.IsNullOrEmpty(inserted) == false)
            {
                _playerInDateMap[player] = inserted;
                _playerBestTotalMap[player] = newTotal;
            }
            return;
		}

        int prevTotal = 0;
        if (_playerBestTotalMap.ContainsKey(player))
        {
            prevTotal = _playerBestTotalMap[player];
        }

        if (newTotal > prevTotal)
        {
            string inDate = _playerInDateMap[player];
            bool ok = UserData.UpdateSession(table, inDate, newRecord);
            if (ok)
            {
                _playerBestTotalMap[player] = newTotal;
            }
        }
	}
}


