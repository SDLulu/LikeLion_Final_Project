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
}
