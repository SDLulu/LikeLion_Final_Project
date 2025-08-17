using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면 해상도 설정을 관리하는 UI 클래스
/// </summary>
public class UI_Setting : MonoBehaviour
{
    [Header("해상도 설정 UI")]
    [SerializeField] private Button _leftArrowButton;
    [SerializeField] private Button _rightArrowButton;
    [SerializeField] private TextMeshProUGUI _resolutionText;
    [SerializeField] private Button _applyButton;

    [Header("BGM 설정 UI")]
    [SerializeField] private Slider _bgmSlider;
    private readonly string[] _resolutionOptions = { "전체화면", "1600 x 900", "1280 x 720" };
    private int _currentOptionIndex = 0;
    private const string RESOLUTION_PREF_KEY = "ResolutionSetting";
    private const string BGM_VOLUME_PREF_KEY = "BGMVolume";

    public static void ApplyResolution()
    {
        int savedResolution = PlayerPrefs.GetInt(RESOLUTION_PREF_KEY, 0);
        
        if (savedResolution < 0 || savedResolution > 2)
            savedResolution = 0;

        switch (savedResolution)
        {
            case 0:
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
                break;
                
            case 1: 
                Screen.SetResolution(1600, 900, false);
                break;
                
            case 2:
                Screen.SetResolution(1280, 720, false);
                break;
        }
    }

    public static void ApplyBGMVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat(BGM_VOLUME_PREF_KEY, 1f);
        if (savedVolume < 0f || savedVolume > 1f)
            savedVolume = 1f;
        BGMManager.Inst.SetMasterVolume(savedVolume);
    }

    private void Awake()
    {
        _leftArrowButton.onClick.AddListener(OnLeftArrowClicked);
        _rightArrowButton.onClick.AddListener(OnRightArrowClicked);
        _applyButton?.onClick.AddListener(OnApplyButtonClicked);
        _bgmSlider.onValueChanged.AddListener(OnBGMSliderValueChanged);
        LoadSavedResolution();
        UpdateResolutionText();
        LoadSavedBGMVolume();
    }

    private void OnEnable()
    {
        LoadSavedResolution();
        UpdateResolutionText();
        LoadSavedBGMVolume();
    }

    private void LoadSavedResolution()
    {
        _currentOptionIndex = PlayerPrefs.GetInt(RESOLUTION_PREF_KEY, 0);
        
        // 유효하지 않은 인덱스일 경우 기본값으로 설정
        if (_currentOptionIndex < 0 || _currentOptionIndex >= _resolutionOptions.Length)
        {
            _currentOptionIndex = 0;
        }
    }

    private void SaveResolution()
    {
        PlayerPrefs.SetInt(RESOLUTION_PREF_KEY, _currentOptionIndex);
        PlayerPrefs.Save();
    }

    private void LoadSavedBGMVolume()
    {
        float savedVolume = PlayerPrefs.GetFloat(BGM_VOLUME_PREF_KEY, 1f);
        if (savedVolume < 0f || savedVolume > 1f)
        {
            savedVolume = 1f;
        }

        _bgmSlider.value = savedVolume;
    }

    private void SaveBGMVolume()
    {
        PlayerPrefs.SetFloat(BGM_VOLUME_PREF_KEY, _bgmSlider.value);
        PlayerPrefs.Save();
    }

    private void OnBGMSliderValueChanged(float value)
    {
        BGMManager.Inst.SetMasterVolume(value);
    }

    private void ApplyCurrentBGMVolume()
    {
        BGMManager.Inst.SetMasterVolume(_bgmSlider.value);
    }

    private void ApplyCurrentResolution()
    {
        switch (_currentOptionIndex)
        {
            case 0: // 전체화면
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
                break;
                
            case 1: // 1600 x 900
                Screen.SetResolution(1600, 900, false);
                break;
                
            case 2: // 1280 x 720
                Screen.SetResolution(1280, 720, false);
                break;
        }
    }

    private void UpdateResolutionText()
    {
        _resolutionText.text = _resolutionOptions[_currentOptionIndex];
    }

    private void OnLeftArrowClicked()
    {
        _currentOptionIndex--;
        if (_currentOptionIndex < 0)
        {
            _currentOptionIndex = _resolutionOptions.Length - 1;
        }
        UpdateResolutionText();
    }

    private void OnRightArrowClicked()
    {
        _currentOptionIndex++;
        if (_currentOptionIndex >= _resolutionOptions.Length)
        {
            _currentOptionIndex = 0;
        }
        UpdateResolutionText();
    }



    private void OnApplyButtonClicked()
    {
        ApplyCurrentResolution();
        SaveResolution();
        ApplyCurrentBGMVolume();
        SaveBGMVolume();
    }

    private void OnDestroy()
    {
        _leftArrowButton.onClick.RemoveListener(OnLeftArrowClicked);
        _rightArrowButton.onClick.RemoveListener(OnRightArrowClicked);
        _applyButton?.onClick.RemoveListener(OnApplyButtonClicked);
        _bgmSlider.onValueChanged.RemoveListener(OnBGMSliderValueChanged);
    }
}
