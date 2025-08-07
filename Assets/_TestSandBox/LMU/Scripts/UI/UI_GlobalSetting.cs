using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_GlobalSetting : MonoBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private RectTransform _fastTestHolder;
    [SerializeField] private TMP_Dropdown _sceneDropdown;
    [SerializeField] private TMP_Text _currentFocusSceneText;
    [SerializeField] private Button _sceneListRefreshbutton;

    [Header("디버그용")]
    [SerializeField] private GlobalSetting _globalSetting;

    public void ActiveUI(bool active)
    {
        if (_globalSetting._isShowGlobalSettingUI == false)
        {
            this.gameObject.SetActive(false);
            return;
        }

#if UNITY_EDITOR
        this.gameObject.SetActive(active);
#else
        this.gameObject.SetActive(false);
#endif
    }

    private void Awake()
    {
        _globalSetting = GetComponentInParent<GlobalSetting>();
        if (_globalSetting._isShowGlobalSettingUI == false)
        {
            this.gameObject.SetActive(false);
            return;
        }

#if UNITY_EDITOR
        this.gameObject.SetActive(true);
#else
        this.gameObject.SetActive(false);
#endif
        _fastTestHolder.gameObject.SetActive(true);

        _sceneListRefreshbutton.onClick.AddListener(OnSceneListRefreshButtonClick);
        _sceneDropdown.onValueChanged.AddListener(OnSceneDropdownValueChanged);

        LoadSceneInfoList();
    }

    private void OnDestroy()
    {
        _globalSetting = null;
        _sceneListRefreshbutton.onClick.RemoveAllListeners();
        _sceneDropdown.onValueChanged.RemoveAllListeners();
    }

    private void LoadSceneInfoList()
    {
        #if UNITY_EDITOR
        if (_globalSetting.SettingData.IsLoadScenes)
        {
            _sceneDropdown.options.Clear();
            foreach (var scene in _globalSetting.SettingData.SceneInfoList)
            {
                _sceneDropdown.options.Add(new TMP_Dropdown.OptionData(scene.name));
            }
        }
        else
        {
            _globalSetting.SettingData.RegisterAllSceneInfo();
            _sceneDropdown.options.Clear();
            foreach (var scene in _globalSetting.SettingData.SceneInfoList)
            {
                _sceneDropdown.options.Add(new TMP_Dropdown.OptionData(scene.name));
            }
        }

        if (_globalSetting.SettingData.IsFocusScene)
        {
            int value = _globalSetting.SettingData.SceneInfoList.IndexOf(_globalSetting.SettingData.FocusScene);
            OnSceneDropdownValueChanged(value);
        }
        else
        {
            _currentFocusSceneText.text = "None";
        }
        #endif
    }

    private void OnSceneListRefreshButtonClick()
    {
        #if UNITY_EDITOR
        _globalSetting.SettingData.RegisterAllSceneInfo();
        _sceneDropdown.options.Clear();
        foreach (var scene in _globalSetting.SettingData.SceneInfoList)
        {
            _sceneDropdown.options.Add(new TMP_Dropdown.OptionData(scene.name));
        }

        // FocusScene으로 설정
        int value = _globalSetting.SettingData.SceneInfoList.IndexOf(_globalSetting.SettingData.FocusScene);
        OnSceneDropdownValueChanged(value);
        #endif
    }

    private void OnSceneDropdownValueChanged(int value)
    {
        #if UNITY_EDITOR
        if(value < 0 || value >= _globalSetting.SettingData.SceneInfoList.Count)
        {
            return;
        }
        _globalSetting.SettingData.FocusScene = _globalSetting.SettingData.SceneInfoList[value];
        _currentFocusSceneText.text = $"{_sceneDropdown.options[value].text}";
        _globalSetting.SettingData.IsFocusScene = true;
        #endif
    }

















    private void OnClickFastTestBtn()
    {
        _fastTestHolder.gameObject.SetActive(true);
    }

    private async void OnFastTestStartButtonClick()
    {
        try
        {
            var title = LobbyUI_Manager.Inst.UIEnterOnline;
            if (title == null)
            {
                Debug.LogError("UI_Title 컴포넌트를 찾을 수 없습니다.");
                return;
            }

            // 네트워크 연결
            await title.RunFastMode();
            await Awaitable.NextFrameAsync();

            // PlayerManager 폴링 대기
            PlayerManager playerM = null;
            float waitTime = 0f;
            const float maxWaitTime = 10f;

            while (playerM == null && waitTime < maxWaitTime)
            {
                await Awaitable.WaitForSecondsAsync(0.2f);
                waitTime += 0.2f;
                playerM = FindAnyObjectByType<PlayerManager>();
            }

            if (playerM == null)
            {
                Debug.LogError("PlayerManager를 찾을 수 없습니다.");
                return;
            }

            // 서버(Host)인 경우에만 게임 시작 처리
            if (playerM.Runner.IsServer)
            {
                await StartGameAsHost(playerM);
            }
            else
            {
                await WaitForGameStart();
                await Awaitable.WaitForSecondsAsync(0.5f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FastTest 실행 중 오류 발생: {e.Message}");
        }
    }

    /// <summary>
    /// 호스트에서 게임 시작 처리
    /// </summary>
    private async Awaitable StartGameAsHost(PlayerManager playerM)
    {
        var result = await playerM.TryStartGameAsync(true);
        if (result)
        {
            // 현재 씬이 DevGame 씬으로 전환될때까지 대기
            while(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "DevGame")
            {
                await Awaitable.WaitForSecondsAsync(0.1f);
            }
            GameStates.Inst.DelayForceActiveState<GameStageWaitingState>();
        }
    }

    /// <summary>
    /// 클라이언트에서 게임 시작 대기
    /// </summary>
    private async Awaitable WaitForGameStart(float maxWaitTime = 30f)
    {
        float waitTime = 0f;

        while (waitTime < maxWaitTime)
        {
            await Awaitable.WaitForSecondsAsync(0.5f);
            waitTime += 0.5f;

            // 게임 상태가 변경되었는지 확인
            if (GameStates.Inst != null && GameStates.Inst.StateMachine != null)
            {
                var currentState = GameStates.Inst.StateMachine.ActiveState;
                if (currentState is GameStageWaitingState)
                {
                    Debug.Log("FastTest 완료 - 게임 시작됨");
                    return;
                }
            }
        }

        Debug.LogWarning("게임 시작 대기 시간");
    }

    /// <summary>
    /// 로컬에서 안전한 UI 제어 (폴백용)
    /// </summary>
    private void TrySetLobbyUILocal(bool active)
    {
        try
        {
            if (LobbyUI_Manager.Inst != null && LobbyUI_Manager.Inst.UILobby != null)
            {
                LobbyUI_Manager.Inst.UILobby.gameObject.SetActive(active);
                Debug.Log($"로컬 UI 제어: 로비 UI {(active ? "활성화" : "비활성화")}");
            }
            else
            {
                Debug.LogWarning("UI_Controller 또는 uiLobby가 null입니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"로컬 UI 제어 중 오류: {e.Message}");
        }
    }

}
