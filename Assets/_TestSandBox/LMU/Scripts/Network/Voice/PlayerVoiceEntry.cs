using Fusion;
using Photon.Voice.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개별 플레이어의 보이스 UI와 오디오 제어를 담당합니다.
/// </summary>
public class PlayerVoiceEntry : MonoBehaviour
{
    [Header("플레이어 이름 텍스트")]
    [SerializeField] private TextMeshProUGUI _playerNameText;

    [Header("목소리 볼륨 슬라이더")]
    [SerializeField] private Slider _voiceSlider;

    [Header("플레이어 보이스 AudioSource")]
    [SerializeField] private AudioSource _voiceAudioSource;

    private bool _initialized;

    private void Start()
    {
        if (_voiceSlider != null)
        {
            _voiceSlider.onValueChanged.AddListener((value) =>
            {
                if (_voiceAudioSource != null)
                {
                    _voiceAudioSource.volume = value;
                }
            });
        }
    }

    /// <summary>
    /// 플레이어의 NetworkObject를 통해 닉네임과 음성 오디오 소스를 연결합니다.
    /// </summary>
    public void Initialize(NetworkObject playerObject)
    {
        if (playerObject == null)
        {
            return;
        }

        // 닉네임 표시: PlayerData의 Networked 닉네임 사용
        PlayerData playerData = playerObject.GetComponent<PlayerData>();
        if (playerData != null)
        {
            if (_playerNameText != null)
            {
                _playerNameText.SetText(playerData.NickName);
            }
        }

        // Fusion + Photon Voice: Speaker 검색 (자식 포함)
        Speaker speaker = playerObject.GetComponentInChildren<Speaker>(true);
        if (speaker != null)
        {
            AudioSource attachedAudio = speaker.GetComponent<AudioSource>();
            if (attachedAudio == null)
            {
                attachedAudio = speaker.GetComponentInChildren<AudioSource>(true);
            }

            if (attachedAudio != null)
            {
                _voiceAudioSource = attachedAudio;
            }
        }

        _initialized = true;
    }
}


