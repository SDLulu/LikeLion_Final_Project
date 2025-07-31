using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class CutSceneController : MonoBehaviour
{
    private void OnValidate()
    {
        if (UICutSceneResult != null)
            UICutSceneResult.gameObject.SetActive(false);
    }
    [Header("인스펙터 참조")]
    [field: SerializeField] public UI_CutSceneResult UICutSceneResult { get; private set; }
    [field: SerializeField] public Transform StartPoint { get; private set; }
    [field: SerializeField] public Transform EndPoint { get; private set; }
    [field: SerializeField] public CinemachineCamera CutSceneCamera { get; private set; }

    public Vector3 GetStartPoint() => StartPoint.position;
    public Vector3 GetEndPoint() => EndPoint.position;

    private List<Tween> cutTweens = new();
    private List<GameObject> cutPlayers = new();

    private void Awake()
    {
        if (UICutSceneResult != null)
            UICutSceneResult.gameObject.SetActive(false);
        DefocusCutSceneCamera();
    }

    public void FocusCutSceneCamera()
    {
        Debug.Log("FocusCutSceneCamera - FocusCutSceneCamera - FocusCutSceneCamera");
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


    public async Awaitable PlayCutScene(List<PlayerData> alivePlayers, float cutDuration)
    {
        if (StartPoint == null || EndPoint == null)
        {
            Debug.LogError("CutScene 시작 위치 또는 끝 위치가 설정되지 않았습니다.");
            return;
        }

        var cutsPlayerPrefab = Resources.Load<GameObject>("Prefabs/CutsPlayer");

        CutTweenClear();
        float intervalSeconds = 0.15f;
        for(int i = 0; i < alivePlayers.Count; i++)
        {
            var obj = Instantiate(cutsPlayerPrefab);
            cutPlayers.Add(obj);
            obj.transform.position = StartPoint.position;
            var tween = obj.transform.DOMove(EndPoint.position, cutDuration);
            cutTweens.Add(tween);
            await Awaitable.WaitForSecondsAsync(intervalSeconds);
        }

        await Awaitable.WaitForSecondsAsync(cutDuration);

        CutTweenClear();
        foreach (var player in cutPlayers)
            GameObject.Destroy(player);

        await Awaitable.NextFrameAsync();

        void CutTweenClear()
        {
            foreach (var tween in cutTweens)
                tween.Kill();
            cutTweens.Clear();
        }
    }
}
