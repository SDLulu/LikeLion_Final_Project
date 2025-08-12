using System;
using Fusion;
using LMCore;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameProgressTracker : BaseManager<GameProgressTracker>
{
    [Header("디버그")]
    [SerializeField] private bool _isLogDebug = true;

    private bool _isTracking = false;
    private double _startTime = 0.0d;
    private double _elapsedSeconds = 0.0d;
    private string _currentStageId = "";
    private NetworkRunner _runner = null;

    public string CurrentStageId
    {
        get
        {
            return _currentStageId;
        }
        private set
        {
            _currentStageId = value;
        }
    }

    public double ElapsedSeconds
    {
        get
        {
            return _elapsedSeconds;
        }
        private set
        {
            _elapsedSeconds = value;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        NetworkEventSystem.Inst.OnSceneLoadDoneEvent += OnSceneLoadDone;
        NetworkEventSystem.Inst.OnGameStateChangedEvent += OnGameStateChanged;
        NetworkEventSystem.Inst.OnAllManagersReady += OnAllManagersReady;
        NetworkEventSystem.Inst.OnStageLoadDoneEvent += OnStageLoadDone;
        UpdateStageIdFromActiveScene();
    }


    private void OnStageLoadDone(Stage.Data stageInfo)
    {
        if (stageInfo != null)
        {
            CurrentStageId = stageInfo.ToString();
        }
        else
        {
            UpdateStageIdFromActiveScene();
        }

        if (_isLogDebug)
        {
            Debug.Log($"[GameProgressTracker] StageLoadDone - StageId 업데이트: {CurrentStageId}");
        }
    }

    private void Update()
    {
        if (_isTracking)
        {
            if (_runner != null)
            {
                ElapsedSeconds = _runner.SimulationTime - _startTime;
            }
            else
            {
                ElapsedSeconds = Time.realtimeSinceStartupAsDouble - _startTime;
            }
        }
    }

    private void OnAllManagersReady()
    {
        if (_isLogDebug)
        {
            Debug.Log("[GameProgressTracker] 모든 필수 매니저 준비 완료");
        }
    }

    private void OnSceneLoadDone(NetworkRunner runner, string sceneName)
    {
        _runner = runner;
        UpdateStageIdFromActiveScene();

        if (_isLogDebug)
        {
            Debug.Log($"[GameProgressTracker] 씬 로드 완료 - Scene: {sceneName}, StageId: {CurrentStageId}");
        }
    }

    private void OnGameStateChanged(E_StateName previousState, E_StateName currentState)
    {
        if (_runner != null)
        {
            if (_runner.IsServer == false)
            {
                return;
            }
        }

        if (currentState == E_StateName.GameStagePlayingState)
        {
            StartTracking();
            return;
        }

        if (currentState == E_StateName.GameStageCompletedState || currentState == E_StateName.GameStageFailedState)
        {
            if (_isTracking)
            {
                StopTrackingAndSubmit(currentState);
            }

            return;
        }
    }

    private void UpdateStageIdFromActiveScene()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;

        if (string.IsNullOrEmpty(activeSceneName))
        {
            CurrentStageId = "Unknown";
            return;
        }

        CurrentStageId = activeSceneName;
    }

    /// <summary>
    /// 플레이 시간 측정을 시작
    /// </summary>
    private void StartTracking()
    {
        _isTracking = true;
        ElapsedSeconds = 0.0d;

        if (_runner != null)
        {
            _startTime = _runner.SimulationTime;
        }
        else
        {
            _startTime = Time.realtimeSinceStartupAsDouble;
        }

        if (_isLogDebug)
        {
            Debug.Log($"[GameProgressTracker] 트래킹 시작 - Stage: {CurrentStageId}");
        }
    }

    /// <summary>
    /// 플레이 시간 측정을 종료하고 결과를 제출
    /// </summary>
    private void StopTrackingAndSubmit(E_StateName endState)
    {
        _isTracking = false;

        double totalSeconds = ElapsedSeconds;
        string endReason = endState == E_StateName.GameStageCompletedState ? "Completed" : "Failed";

        if (_isLogDebug)
        {
            Debug.Log(
                $"[GameProgressTracker] 트래킹 종료 - Stage: {CurrentStageId}, Time: {totalSeconds:F3}s, End: {endReason}"
            );
        }

        TrySubmitRecord(CurrentStageId, totalSeconds, endReason);
    }

    /// <summary>
    /// 기록을 백엔드로 제출(백엔드 비활성화면 건너뜀)
    /// </summary>
    private void TrySubmitRecord(string stageId, double elapsedSeconds, string endReason)
    {
        if (GlobalSetting.Inst.IsEnableBackend == false)
        {
            if (_isLogDebug)
            {
                Debug.Log("[GameProgressTracker] 백엔드 비활성화 - 제출 생략");
            }
            return;
        }

        BackEndWorkFlow.Inst.SubmitStageRunRecord(
            stageId,
            elapsedSeconds,
            endReason,
            onSuccess: () =>
            {
                if (_isLogDebug)
                {
                    Debug.Log("[GameProgressTracker] 기록 제출 성공");
                }
            },
            onFail: (code, message) =>
            {
                if (_isLogDebug)
                {
                    Debug.LogError($"[GameProgressTracker] 기록 제출 실패 - {code} : {message}");
                }
            }
        );
    }
}


