using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameProgressTracker : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_StageProgress _uiStageProgress;
    [SerializeField] private double _targetSeconds = 0.0d;

    [Header("디버그 용")]
    [SerializeField] private bool _isTracking = false;
    [SerializeField] private double _startTime = 0.0d;
    [SerializeField] private NetworkRunner _runner = null;
    [SerializeField] private Stage.Data _curStageInfo = null;

	[Header("기록")]
	[SerializeField] private bool _isSessionActive = false;
	[SerializeField] private double _sessionStartTime = 0.0d;
	[SerializeField] private double _lastSessionElapsedSeconds = 0.0d;

	// 컷신 제외용 누적
	private bool _isCutsceneActive = false;
	private double _cutsceneStartTime = 0.0d;
	private double _accumulatedCutsceneSecondsForStage = 0.0d;

	private readonly List<StageRunRecord> _stageRunRecords = new List<StageRunRecord>();
    public bool IsServerAuthority => _runner != null && _runner.IsServer;

    private void Awake()
    {
        NetworkEventSystem.Inst.OnSceneLoadDoneEvent += OnSceneLoadDone;
        NetworkEventSystem.Inst.OnGameStateChangedEvent += OnGameStateChanged;
        NetworkEventSystem.Inst.OnStageLoadDoneEvent += OnStageLoadDone;
        NetworkEventSystem.Inst.OnShutdownEvent += OnShutdown;
    }

    private void OnSceneLoadDone(NetworkRunner runner, string sceneName)
    {
        if (_runner == null)
        {
            _runner = runner;
            Debug.Log($"[GameProgressTracker] 러너 초기화 완료 - Scene: {sceneName}");
        }
    }

    private void OnStageLoadDone(Stage.Data stageInfo)
    {
        _curStageInfo = stageInfo;
        Debug.Log($"[GameProgressTracker] StageLoadDone 업데이트: {_curStageInfo.CurrentStage}");
    }

    private void Update()
    {
		if (_isTracking == false || _runner == null || _curStageInfo == null)
            return;

        double stageElapsedSeconds = GetCurrentStageElapsedExcludingCutscenes();
        double sessionElapsedSeconds = GetCurrentSessionElapsedIncludingCutscenes();
        _uiStageProgress.UpdateTimeData(sessionElapsedSeconds, stageElapsedSeconds, _targetSeconds, _curStageInfo.CurrentStage);
    }

    private void OnGameStateChanged(E_StateName preState, E_StateName curState)
    {
        // 세션 시작
        if (curState == E_StateName.PlayingState && _isSessionActive == false)
        {
            StartSession();
        }
        
        // 트래킹 시작
        if (curState == E_StateName.PlayingState)
        {
            StartTracking();
        }

        if (curState == E_StateName.CompletedState || curState == E_StateName.FailedState)
        {
            if (_isTracking)
                StopTrackingAndSubmit(curState);
        }
    }

    private void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        if (_isSessionActive)
        {
            EndSession();
        }
    }

    /// <summary>
    /// 플레이 시간 측정을 시작
    /// </summary>
    private void StartTracking()
    {
        if (_runner == null)
            return;

        _isTracking = true;
		_startTime = _runner.SimulationTime;
		_accumulatedCutsceneSecondsForStage = 0.0d;
		_isCutsceneActive = false;

        Debug.Log($"[GameProgressTracker] 트래킹 시작 - Stage: {_curStageInfo.CurrentStage}");
    }

    /// <summary>
    /// 플레이 시간 측정을 종료하고 결과를 제출
    /// </summary>
    private void StopTrackingAndSubmit(E_StateName endState)
    {
        _isTracking = false;

		// 진행 중 컷신이 켜져 있으면 마무리
		if (_isCutsceneActive)
		{
			double now = _runner.SimulationTime;
			_accumulatedCutsceneSecondsForStage += now - _cutsceneStartTime;
			_isCutsceneActive = false;
		}

		double elapsedSeconds = GetCurrentStageElapsedExcludingCutscenes();
        string endReason = endState == E_StateName.CompletedState ? "Completed" : "Failed";
        Debug.Log($"[GameProgressTracker] 트래킹 종료 - Stage: {_curStageInfo.CurrentStage}, Time: {elapsedSeconds:F3}s, End: {endReason}");

		if (_curStageInfo != null)
		{
			_stageRunRecords.Add(new StageRunRecord
			{
				StageId = _curStageInfo.CurrentStage,
				ElapsedSeconds = elapsedSeconds,
				EndReason = endReason,
			});
		}

        TrySubmitRecord(_curStageInfo.CurrentStage, elapsedSeconds, endReason);
    }

	/// <summary>
	/// 현재 스테이지의 실제 플레이 시간(컷신 제외)을 계산
	/// </summary>
	private double GetCurrentStageElapsedExcludingCutscenes()
	{
		if (_runner == null)
			return 0.0d;
		double now = _runner.SimulationTime;
		double cutsceneTotal = _accumulatedCutsceneSecondsForStage;
		if (_isCutsceneActive)
		{
			cutsceneTotal += now - _cutsceneStartTime;
		}
		double elapsed = now - _startTime - cutsceneTotal;
		if (elapsed < 0.0d)
		{
			elapsed = 0.0d;
		}
		return elapsed;
	}

	/// <summary>
	/// 세션(컷신 포함) 시작
	/// </summary>
	private void StartSession()
	{
		if (_runner == null)
			return;
		_isSessionActive = true;
		_sessionStartTime = _runner.SimulationTime;
		_lastSessionElapsedSeconds = 0.0d;
	}

	/// <summary>
	/// 세션(컷신 포함) 종료 및 총 시간 계산
	/// </summary>
	private void EndSession()
	{
		if (_isSessionActive == false || _runner == null)
			return;
		_lastSessionElapsedSeconds = _runner.SimulationTime - _sessionStartTime;
		_isSessionActive = false;
		Debug.Log($"[GameProgressTracker] 세션 종료 - Total: {_lastSessionElapsedSeconds:F3}s");
	}

    /// <summary>
    /// 현재 진행 중인 세션의 경과 시간(컷신 포함). 세션이 비활성화면 마지막 기록값 반환
    /// </summary>
    private double GetCurrentSessionElapsedIncludingCutscenes()
    {
        if (_runner == null)
            return 0.0d;

        if (_isSessionActive)
        {
            double now = _runner.SimulationTime;
            double elapsed = now - _sessionStartTime;
            if (elapsed < 0.0d)
            {
                elapsed = 0.0d;
            }
            return elapsed;
        }

        return _lastSessionElapsedSeconds;
    }

    /// <summary>
    /// 스테이지별 기록(서버 권한 기준 권위). 클라이언트는 로컬 미러.
    /// </summary>
    public IReadOnlyList<StageRunRecord> StageRunRecords
    {
        get { return _stageRunRecords; }
    }

    /// <summary>
    /// 마지막 세션 총 시간(컷신 포함)
    /// </summary>
    public double LastSessionElapsedSeconds
    {
        get { return _lastSessionElapsedSeconds; }
    }

    /// <summary>
    /// 스테이지 러닝 기록 데이터
    /// </summary>
    [Serializable]
    public class StageRunRecord
    {
        public string StageId;
        public double ElapsedSeconds;
        public string EndReason;
    }

    /// <summary>
    /// 기록을 백엔드로 제출 - Only Server
    /// </summary>
    private void TrySubmitRecord(string stageId, double elapsedSeconds, string endReason)
    {
        if (IsServerAuthority == false)
            return;

        if (GlobalSetting.Inst.IsEnableBackend == false)
        {
            Debug.Log("[GameProgressTracker] 백엔드 비활성화 - 제출 생략");
            return;
        }

        BackEndWorkFlow.Inst.SubmitStageRunRecord(
            stageId,
            elapsedSeconds,
            endReason,
            onSuccess: () =>
            {
                Debug.Log("[GameProgressTracker] 기록 제출 성공");
            },
            onFail: (code, message) =>
            {
                Debug.LogError($"[GameProgressTracker] 기록 제출 실패 - {code} : {message}");
            }
        );
    }
}


