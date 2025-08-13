using TMPro;
using UnityEngine;

public class UI_StageProgress : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private TMP_Text _sessionTimeText;
    [SerializeField] private TMP_Text _stagePlayingTimeText;
    [SerializeField] private TMP_Text _curStageText;
    [SerializeField] private TMP_Text _monsterKillCountText;
    [SerializeField] private TMP_Text _itemCollectCountText;
    [SerializeField] private bool _showSessionTime = true;

    private void Awake()
    {
        NetworkEventSystem.Inst.OnCutSceneActiveEvent += OnCutSceneActive;
    }
    private void OnCutSceneActive(bool isActive)
    {
        this.gameObject.SetActive(isActive == false);
    }

    public void UpdateTimeData(double sessionElapsedSeconds, double stageElapsedSeconds, double totalSeconds, string currentStageId)
    {
        int sessionWhole = (int)sessionElapsedSeconds;
        int stageWhole = (int)stageElapsedSeconds;

        int sessionMinutes = sessionWhole / 60;
        int sessionSeconds = sessionWhole % 60;
        int stageMinutes = stageWhole / 60;
        int stageSeconds = stageWhole % 60;

        if (_showSessionTime)
        {
            _sessionTimeText.text = string.Format("{0:00}:{1:00}", sessionMinutes, sessionSeconds);
        }
        _stagePlayingTimeText.text = string.Format("{0:00}:{1:00}", stageMinutes, stageSeconds);
        _curStageText.text = currentStageId;
    }

    public void UpdateScoreData(int monsterKillCount, int itemCollectCount)
    {
        _monsterKillCountText.text = monsterKillCount.ToString();
        _itemCollectCountText.text = itemCollectCount.ToString();
    }

    public void SetShowSessionTime(bool show)
    {
        _showSessionTime = show;
        if (_sessionTimeText != null)
        {
            _sessionTimeText.gameObject.SetActive(show);
        }
    }

    public bool IsSessionTimeVisible()
    {
        return _showSessionTime;
    }
}
