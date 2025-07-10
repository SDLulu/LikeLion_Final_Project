using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameStateTest : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private RectTransform _testHolder;
    [SerializeField] private TMP_Text _stateText;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private Button _nextStageButton;
    [SerializeField] private Button _failButton;


    private void Awake()
    {
#if UNITY_EDITOR
        _testHolder.gameObject.SetActive(true);
        _nextStageButton.onClick.AddListener(OnClickNextStageButton);
        _failButton.onClick.AddListener(OnClickFailButton);
#else
        _testHolder.gameObject.SetActive(false);
#endif
    }

    private void OnDestroy()
    {
        _nextStageButton.onClick.RemoveListener(OnClickNextStageButton);
        _failButton.onClick.RemoveListener(OnClickFailButton);
    }

    private void OnClickNextStageButton()
    {
        GameStates.Inst.DelayForceActiveState<GameStageCompletedState>();
    }

    private void OnClickFailButton()
    {
        GameStates.Inst.DelayForceActiveState<GameStageFailedState>();
    }
}
