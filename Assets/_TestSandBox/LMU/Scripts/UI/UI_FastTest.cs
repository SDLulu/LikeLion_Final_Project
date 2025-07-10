using UnityEngine;
using UnityEngine.UI;

public class UI_FastTest : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private RectTransform _fastTestHolder;
    [SerializeField] private Button _fastTestStartButton;

    private void Awake()
    {
#if UNITY_EDITOR
        _fastTestHolder.gameObject.SetActive(true);
        _fastTestStartButton.onClick.AddListener(OnFastTestStartButtonClick);
#else
        _fastTestHolder.gameObject.SetActive(false);
#endif
    }

    private async void OnFastTestStartButtonClick()
    {
        var title = GetComponent<UI_Title>();
        if (title == null)
        {
            Debug.LogError("UI_Title 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        await title.OnClickCreateRoomBtn();
        await Awaitable.NextFrameAsync();
        var playerM = FindAnyObjectByType<PlayerManager>();
        var result = await playerM.TryStartGameAsync(true); 
        if (result)
        {
            GameStates.Inst.DelayForceActiveState<GameStageWaitingState>();
        }
    }
}
