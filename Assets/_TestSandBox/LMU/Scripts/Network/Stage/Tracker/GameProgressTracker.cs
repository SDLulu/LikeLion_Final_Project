using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameProgressTracker : NetworkBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private UI_StageProgress _uiStageProgress;
	[Networked] public float NetStageElapsedSeconds { get; private set; }
	[Networked] public float NetSessionElapsedSeconds { get; private set; }
	[Networked] public NetworkString<_32> NetStageId { get; private set; }

    private bool _isSessionActive = false;
    private bool _isStageActive = false;
    private bool _isCutSceneActive = false;

    public override void Spawned()
    {
        base.Spawned();
        _isSessionActive = true;

        NetworkEventSystem.Inst.OnSceneLoadDoneEvent += OnSceneLoadDone;
        NetworkEventSystem.Inst.OnGameStateChangedEvent += OnGameStateChanged;
        NetworkEventSystem.Inst.OnStageLoadDoneEvent += OnStageLoadDone;
        NetworkEventSystem.Inst.OnCutSceneActiveEvent += OnCutSceneActive;
        NetworkEventSystem.Inst.OnShutdownEvent += OnShutdown;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _isSessionActive = false;
        _isStageActive = false;
        _isCutSceneActive = false;
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
        // 필요 시 상태 전환에 맞춘 추가 처리 가능
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

    private void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        _isSessionActive = false;
        _isStageActive = false;
    }
}


