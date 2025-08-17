using TMPro;
using UnityEngine;

public class UI_StageProgress2 : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private TMP_Text _sessionTimeText;
    [SerializeField] private TMP_Text _stagePlayingTimeText;
    [SerializeField] private TMP_Text _curStageText;
    [SerializeField] private TMP_Text _monsterKillCountText;
    [SerializeField] private TMP_Text _itemCollectCountText;
    [SerializeField] private bool _showSessionTime = true;


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
            _sessionTimeText.text = "전체 플레이 타임 : " + string.Format("{0:00}:{1:00}", sessionMinutes, sessionSeconds);
        }
        _stagePlayingTimeText.text = "클리어 타임 : " + string.Format("{0:00}:{1:00}", stageMinutes, stageSeconds);
        _curStageText.text = "스테이지 : " + currentStageId;
    }

    public void UpdateScoreData(int monsterKillCount, int itemCollectCount)
    {
        if (_monsterKillCountText != null)
            _monsterKillCountText.text = monsterKillCount.ToString();
        if (_itemCollectCountText != null)
            _itemCollectCountText.text = itemCollectCount.ToString();
    }

    public void SetShowSessionTime(bool show)
    {
        _showSessionTime = show;
        if (_sessionTimeText != null)
            _sessionTimeText.gameObject.SetActive(show);
    }

    public bool IsSessionTimeVisible()
    {
        return _showSessionTime;
    }
}
