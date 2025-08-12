using Fusion;
using LMCore;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_TestVoiceSlot : MonoBehaviour
{
    public PlayerRef Owner { get;  set; }

    [Header("인스펙터 참조")]
    [SerializeField] private Image _voiceActiveImage;
    [SerializeField] private Image _voiceInactiveImage;
    [SerializeField] private Slider _voiceSlider;

    private float _lastVoiceValue = 0.0f;

    private void Awake()
    {
        // 활성화 이미지 클릭
        var activeT = _voiceActiveImage.GetOrAddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry(){eventID = EventTriggerType.PointerDown};
        entry.callback.AddListener(OnClickActiveImage);
        activeT.triggers.Add(entry);

       // 비활성화 이미지 클릭 이벤트 등록
        var inActiveT = _voiceInactiveImage.GetOrAddComponent<EventTrigger>();
        var inactiveEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        inactiveEntry.callback.AddListener((data) => OnClickInactiveImage(data));
        inActiveT.triggers.Add(inactiveEntry);

        // 초기상태 설정
        _lastVoiceValue  = 0.5f;
        _voiceSlider.onValueChanged.AddListener(OnVoiceSliderValueChanged);
        OnClickInactiveImage(null);
        OnVoiceSliderValueChanged(0.5f);
    }

    private void OnClickActiveImage(BaseEventData eventData)
    {
        _voiceActiveImage.gameObject.SetActive(false);
        _voiceInactiveImage.gameObject.SetActive(true);

        OnVoiceSliderValueChanged(_lastVoiceValue);

        // UI -> 음소거 해제
        var pVoice = PlayerManager.Inst.GetPlayerVoice(Owner);
        if (pVoice != null)
        {
            pVoice.SetMutedByUI(false);
        }
    }

    private void OnClickInactiveImage(BaseEventData eventData)
    {
        _voiceActiveImage.gameObject.SetActive(true);
        _voiceInactiveImage.gameObject.SetActive(false);

        _lastVoiceValue = _voiceSlider.value;
        OnVoiceSliderValueChanged(0.0f);

        // UI -> 음소거 설정
        var pVoice = PlayerManager.Inst.GetPlayerVoice(Owner);
        if (pVoice != null)
        {
            pVoice.SetMutedByUI(true);
        }
    }

    public void UpdateData(PlayerData playerData)
    {
        Owner = playerData.Object.InputAuthority;
        // UI 초기값을 실제 스피커 볼륨과 동기화
        var pVoice = PlayerManager.Inst.GetPlayerVoice(Owner);
        if (pVoice != null)
        {
            float volume = pVoice.GetSpeakerVolume();
            if (volume > 0.0f)
            {
                _voiceSlider.SetValueWithoutNotify(volume);
            }
        }
    }

    private void OnVoiceSliderValueChanged(float value)
    {
        _voiceSlider.value = value;

        // UI -> 볼륨 반영
        var pVoice = PlayerManager.Inst.GetPlayerVoice(Owner);
        if (pVoice != null)
        {
            pVoice.SetSpeakerVolume(_voiceSlider.value);
        }
    }

}
