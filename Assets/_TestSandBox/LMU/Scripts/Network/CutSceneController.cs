using System.Collections.Generic;
using DG.Tweening;
using LMCore;
using Unity.Cinemachine;
using UnityEngine;

public class CutSceneController : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [field: SerializeField] public UI_CutSceneResult UICutSceneResult { get; private set; }
    [field: SerializeField] public Transform StartPoint { get; private set; }
    [field: SerializeField] public Transform EndPoint { get; private set; }
    [field: SerializeField] public CinemachineCamera CutSceneCamera { get; private set; }

    public Vector3 GetStartPos() => StartPoint.position;
    public Vector3 GetEndPos() => EndPoint.position;

    private List<Tween> _cutTweens = new();
    private List<GameObject> _cutPlayers = new();

    private void Awake()
    {
        if (UICutSceneResult != null)
            UICutSceneResult.gameObject.SetActive(false);
        DefocusCutSceneCamera();
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
        UICutSceneResult.gameObject.SetActive(value);
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

    public async Awaitable WaitForInputResponse()
    {
        await UICutSceneResult.WaitForInputResponse(null);
    }
}
