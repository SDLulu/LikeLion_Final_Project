using Fusion;
using Photon.Voice.Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerVoice : NetworkBehaviour
{
    [Header("인스펙터 참조")]
    [SerializeField] private VoiceNetworkObject _voiceNetOBj;
    [SerializeField] private InputActionReference _voiceAction;
    [field: SerializeField] public UI_TestVoiceSlot UIVoiceSlot { get; set; }

    public bool IsVoiceActive => _voiceAction.action.IsPressed();
    private bool _uiMuted = false;
    private void Awake()
    {
        if (GlobalSetting.Inst.IsEnableVoice)
        {
            _voiceNetOBj.gameObject.SetActive(true);
            _voiceAction.action.Enable();
        }
        else
        {
            _voiceNetOBj.gameObject.SetActive(false);
            _voiceAction.action.Disable();
        }
    }

    public override void Spawned()
    {
        if(NetworkEventSystem.Inst.IsReady)
        {
            OnInit();
        }
        else
        {
            NetworkEventSystem.Inst.OnAllManagersReady += OnInit;
        }
    }

    private void OnInit()
    {

    }

    private void OnEnable()
    {
        if (GlobalSetting.Inst.IsEnableVoice == false)
            return;
        _voiceAction.action.Enable();
    }
    private void OnDisable()
    {
        _voiceAction.action.Disable();
    }   

    private void Update()
    {
        if (GlobalSetting.Inst.IsEnableVoice == false)
        {
            return;
        }

        // 로컬 플레이어: 입력에 따라 송출 여부 제어 (UI 음소거가 false일 때만 송출)
        if (Object != null)
        {
            if (Object.HasInputAuthority)
            {
                if (_voiceNetOBj != null && _voiceNetOBj.RecorderInUse != null)
                {
                    bool shouldTransmit = IsVoiceActive && (_uiMuted == false);
                    _voiceNetOBj.RecorderInUse.TransmitEnabled = shouldTransmit;
                }
            }
        }
    }

    /// <summary>
    /// UI에서 음소거 상태를 설정합니다. 로컬이면 마이크 송출도 함께 제어합니다.
    /// </summary>
    public void SetMutedByUI(bool muted)
    {
        _uiMuted = muted;

        if (_voiceNetOBj == null)
        {
            return;
        }

        var speaker = _voiceNetOBj.SpeakerInUse != null 
            ? _voiceNetOBj.SpeakerInUse.GetComponent<AudioSource>() 
            : null;

        if (speaker != null)
        {
            speaker.mute = muted;
        }

        if (Object != null)
        {
            if (Object.HasInputAuthority && _voiceNetOBj.RecorderInUse != null)
            {
                _voiceNetOBj.RecorderInUse.TransmitEnabled = (_uiMuted == false) && IsVoiceActive;
            }
        }
    }

    /// <summary>
    /// 해당 플레이어의 스피커 볼륨을 설정합니다. (원격 플레이어 음량 조절)
    /// </summary>
    public void SetSpeakerVolume(float volume)
    {
        if (_voiceNetOBj == null)
        {
            return;
        }

        var speaker = _voiceNetOBj.SpeakerInUse != null 
            ? _voiceNetOBj.SpeakerInUse.GetComponent<AudioSource>() 
            : null;

        if (speaker != null)
        {
            speaker.volume = Mathf.Clamp01(volume);
        }
    }

    /// <summary>
    /// 현재 스피커 볼륨을 반환합니다. 스피커가 없으면 0 반환.
    /// </summary>
    public float GetSpeakerVolume()
    {
        if (_voiceNetOBj == null || _voiceNetOBj.SpeakerInUse == null)
        {
            return 0.0f;
        }

        var speaker = _voiceNetOBj.SpeakerInUse.GetComponent<AudioSource>();
        if (speaker == null)
        {
            return 0.0f;
        }
        return speaker.volume;
    }
}
