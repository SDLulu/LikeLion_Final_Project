using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 오디오 믹서와 연동되는 전역 오디오 설정 컨트롤러
/// </summary>
public class AudioSettingsController : MonoBehaviour
{
    [Header("메인 오디오 믹서")]
    [SerializeField] private AudioMixer mainMixer;

    private const string MasterParam = "MasterVolume";
    private const string BgmParam = "BGMVolume";
    private const string SfxParam = "SFXVolume";
    private const string VoiceParam = "VoiceVolume";

    /// <summary>
    /// Master 볼륨 (0.0~1.0)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        SetVolume(MasterParam, volume);
    }

    /// <summary>
    /// BGM 볼륨 (0.0~1.0)
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        SetVolume(BgmParam, volume);
    }

    /// <summary>
    /// SFX 볼륨 (0.0~1.0)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        SetVolume(SfxParam, volume);
    }

    /// <summary>
    /// Voice 볼륨 (0.0~1.0)
    /// </summary>
    public void SetVoiceVolume(float volume)
    {
        SetVolume(VoiceParam, volume);
    }

    private void SetVolume(string parameter, float volume)
    {
        if (mainMixer == null)
        {
            return;
        }

        float db;
        if (volume <= 0f)
        {
            db = -80f;
        }
        else
        {
            db = Mathf.Log10(volume) * 20f;
        }

        mainMixer.SetFloat(parameter, db);
    }
}


