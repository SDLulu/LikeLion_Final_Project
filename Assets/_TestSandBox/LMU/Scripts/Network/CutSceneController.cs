using System.Collections.Generic;
using DG.Tweening;
using LMCore;
using Unity.Cinemachine;
using UnityEngine;

public class CutSceneController : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [field: SerializeField] public Transform StartPoint { get; private set; }
    [field: SerializeField] public Transform EndPoint { get; private set; }
    [field: SerializeField] public CinemachineCamera CutSceneCamera { get; private set; }
    [field: SerializeField] public UI_StageProgress2 UIStageProgress { get; private set; }

    public Vector3 GetStartPos() => StartPoint.position;
    public Vector3 GetEndPos() => EndPoint.position;

    private List<Tween> _cutTweens = new();
    private List<GameObject> _cutPlayers = new();

    private GameProgressTracker _progressTracker;
    private void Awake()
    {
        _progressTracker = LobbyManager.Inst.ProgressTracker;
    }

    private void OnDestroy()
    {
        _progressTracker = null;
    }

    public void FocusCutSceneCamera()
    {
        CutSceneCamera.Priority = 100;
    }

    public void DefocusCutSceneCamera()
    {
        CutSceneCamera.Priority = -100;
    }

    public void ActiveCutSceneResult(bool value)
    {
        UIStageProgress.gameObject.SetActive(value);
    }

    public async Awaitable PlayCutScene(int playerCount, float cutDuration)
    {
        if (StartPoint == null || EndPoint == null)
        {
            Debug.LogError("CutScene 시작 위치 또는 끝 위치가 설정되지 않았습니다.");
            return;
        }


        var cutsPlayerPrefab = Resources.Load<GameObject>("Prefabs/CutsPlayer");

        CutTweenClear();
        float intervalSeconds = 0.15f;
        for(int i = 0; i < playerCount; i++)
        {
            var obj = Instantiate(cutsPlayerPrefab);
            _cutPlayers.Add(obj);
            obj.transform.position = StartPoint.position;
            var tween = obj.transform.DOMove(EndPoint.position, cutDuration);
            _cutTweens.Add(tween);
            await Awaitable.WaitForSecondsAsync(intervalSeconds);
        }

        await Awaitable.WaitForSecondsAsync(cutDuration);

        CutTweenClear();
        foreach (var player in _cutPlayers)
            GameObject.Destroy(player);

        await Awaitable.NextFrameAsync();

        void CutTweenClear()
        {
            foreach (var tween in _cutTweens)
                tween.Kill();
            _cutTweens.Clear();
        }
    }

    public async Awaitable WaitForNext(int playerCount)
    {
        await WaitForInputResponse(playerCount);
    }


     private bool _isWaiting = false;
    private async Awaitable WaitForInputResponse(int playerCount)
    {
        try
        {
            _isWaiting = true;
            // 스테이지 정보 업데이트
            var data = GetProgressData();
            UIStageProgress.UpdateTimeData(data.Item1, data.Item2, data.Item3, data.Item4);
            for (int i = 0; i < playerCount; i++)
            {
                Debug.Log($"<color=red>대기중: {i}</color>");
                await WaitForResponse();
                await Awaitable.NextFrameAsync();
            }
            await Awaitable.NextFrameAsync();
            _isWaiting = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    private AwaitableCompletionSource _responseTCS;
    private async Awaitable WaitForResponse()
    {
        _responseTCS = new AwaitableCompletionSource();
        await _responseTCS.Awaitable;
        _responseTCS = null;
    }

    public void Update()
    {
        if (_isWaiting && Input.GetKeyDown(KeyCode.K))
        {
            _responseTCS?.SetResult();
        }
    }

    public (double, double, double, string) GetProgressData()
    {
        var progressTracker = LobbyManager.Inst.ProgressTracker;
        return (
            progressTracker.NetStageElapsedSeconds,
            progressTracker.NetSessionElapsedSeconds,
            progressTracker.NetStageId.Length,
            progressTracker.NetStageId.ToString()
        );
    }
}
