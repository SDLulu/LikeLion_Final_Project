using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class UI_GameStateTest : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private RectTransform _testHolder;
    [SerializeField] private Button _nextStageButton;
    [SerializeField] private Button _failButton;

    [SerializeField] private float _keyCooldownTime = 5.0f;
    private float _nextStageLastTime;
    private float _failLastTime;
    private float _emptyLastTime;


    private void Awake()
    {
        if (GlobalSetting.Inst.IsShowGameUI == false)
        {
            _testHolder.gameObject.SetActive(false);
            return;
        }

        NetworkEventSystem.Inst.OnGameStateChangedEvent -= OnGameStateChangedEvent;
        NetworkEventSystem.Inst.OnGameStateChangedEvent += OnGameStateChangedEvent;

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

    private void OnClickEmptyButton()
    {
        GameStates.Inst.DelayForceActiveState<EmptyState>();
    }

    private E_StateName _curState;
    private void OnGameStateChangedEvent(Fusion.NetworkRunner runner, E_StateName prevState, E_StateName state)
    {
        _curState = state;
    }
    private void Update()
    {
        if (_curState != E_StateName.PlayingState)
            return;

        if (Input.GetKeyDown(KeyCode.N) && Time.time >= _nextStageLastTime + _keyCooldownTime)
        {
            _nextStageLastTime = Time.time;
            OnClickNextStageButton();
        }
        
        if (Input.GetKeyDown(KeyCode.Backspace) && Time.time >= _failLastTime + _keyCooldownTime)
        {
            _failLastTime = Time.time;
            OnClickFailButton();
        }

        if (Input.GetKeyDown(KeyCode.L) && Time.time >= _emptyLastTime + _keyCooldownTime)
        {
            _emptyLastTime = Time.time;
            OnClickEmptyButton();
        }

    }
}
