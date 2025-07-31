using System.Collections.Generic;
using GoogleSheet.Core.Type;
using LMCore;
using UnityEngine;

[UGS(typeof(E_Sound))]
public enum E_Sound
{
    BGM,
    SFX,
}
public class SoundManager : BaseManager<SoundManager>
{
    private Dictionary<string, AudioClip> _clips = new();

    [field: SerializeField] public float MasterVolume { get; private set; } = 1f;
    [field: SerializeField] public float BGMVolume { get; private set; } = 0.5f;
    [field: SerializeField] public float SFXVolume { get; private set; } = 0.5f;

    [SerializeField] private AudioSource _bgmSource;
    public AudioSource BGMSource 
    {
        get
        {
            if (_bgmSource == null)
            {
                _bgmSource = this.gameObject.AddComponent<AudioSource>();
            }
            return _bgmSource;
        }
    }
    [SerializeField] private AudioSource _sfxSource;
    public AudioSource SFXSource 
    {
        get
        {
            if (_sfxSource == null)
            {
                _sfxSource = this.gameObject.AddComponent<AudioSource>();
            }
            return _sfxSource;
        }
    }

    private void OnDestroy()
    {
        _clips.Clear();
        _bgmSource = null;
        _sfxSource = null;
        _clips = null;
    }

    public void PlayBGM(string name)
    {
        BGMSource.clip = GetClip(name);
        BGMSource.volume = BGMVolume * MasterVolume;
        BGMSource.Play();
    }

    public void PlaySFX(string name)
    {
        SFXSource.clip = GetClip(name);
        SFXSource.volume = SFXVolume * MasterVolume;
        SFXSource.Play();
    }

    public AudioClip GetClip(string name)
    {
        if (_clips.TryGetValue(name, out var clip))
        {
            return clip;
        }
        else
        {
            var data = DataManager.Inst.GetSoundData(name);
            clip = Resources.Load<AudioClip>(data.Path);
            if (clip == null)
            {
                Debug.LogError($"사운드 파일을 찾을 수 없습니다. name: {name}");
                return null;
            }
            _clips.Add(name, clip);
            return clip;
        }
    }

}
