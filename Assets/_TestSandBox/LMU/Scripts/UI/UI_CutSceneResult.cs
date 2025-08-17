using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CutSceneResult : MonoBehaviour
{
    [System.Serializable]
    public class ToggleInfo
    {
        public Toggle Toggle;
        public UI_Score ScorePanel;
    }

    public class ScoreData
    {
        public int Score { get; set; }
        public int Rank { get; set; }
    }

    [Header("인스펙터 참조")]
    [SerializeField] private ToggleGroup _toggleGroup;
    [SerializeField] private TMP_Text _curStateText;
    [SerializeField] private TMP_Text _curStageTime;
    [SerializeField] private TMP_Text _nextStage;
    [SerializeField] private TMP_Text _allStageTime;
    [SerializeField] private List<ToggleInfo> _toggleInfos = new();

    private bool _isWaiting = false;

    // 플레이어에게 컷신을 토글 순서대로 보여주는 함수
    public async Awaitable WaitForInputResponse(ScoreData[] scoreDatas)
    {
        try
        {
            _isWaiting = true;
            int index = 0;
            foreach (var info in _toggleInfos)
            {
                info.Toggle.isOn = true;
                info.ScorePanel.gameObject.SetActive(true);
                info.ScorePanel.UpdateData(null);
                index++;
                Debug.Log($"<color=red>대기중: {index}</color>");
                await WaitForResponse();
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
}
