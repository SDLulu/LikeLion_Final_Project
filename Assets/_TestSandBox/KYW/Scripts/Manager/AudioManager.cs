using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 🎵 3D 사운드 전용 매니저
public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class SoundData
    {
        public string soundName;        // 사운드 이름 (키)
        public AudioClip audioClip;     // 오디오 클립
        public float volume = 1f;       // 개별 볼륨
        public bool is3D = true;        // 3D 사운드 여부
    }
    
    [Header("🎵 사운드 이펙트 리스트")]
    [SerializeField] private List<SoundData> soundList = new List<SoundData>();
    
    [Header("🔊 설정")]
    [SerializeField] private int audioSourcePoolSize = 16;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 50f;
    [SerializeField] private float defaultVolume = 0.8f;
    
    // AudioSource 풀
    private Queue<AudioSource> audioSourcePool;
    private List<AudioSource> activeAudioSources;
    private Dictionary<string, SoundData> soundDictionary;
    
    // 지속 사운드 관리를 위한 딕셔너리
    private Dictionary<string, AudioSource> loopingAudioSources;
    
    // 싱글톤 인스턴스
    public static AudioManager Inst { get; private set; }
    
    private void Awake()
    {
        if (Inst == null)
        {
            Inst = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeSoundDictionary();
        InitializeAudioSourcePool();
        loopingAudioSources = new Dictionary<string, AudioSource>();
    }
    
    private void InitializeSoundDictionary()
    {
        soundDictionary = new Dictionary<string, SoundData>();
        foreach (var sound in soundList)
        {
            if (!string.IsNullOrEmpty(sound.soundName) && sound.audioClip != null)
            {
                soundDictionary[sound.soundName] = sound;
            }
        }
    }
    
    private void InitializeAudioSourcePool()
    {
        audioSourcePool = new Queue<AudioSource>();
        activeAudioSources = new List<AudioSource>();
        
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            var audioSourceObj = new GameObject($"AudioSource_{i}");
            audioSourceObj.transform.SetParent(transform);
            
            var audioSource = audioSourceObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.volume = defaultVolume;
            
            audioSourcePool.Enqueue(audioSource);
        }
    }
    
    // 사운드 재생 메서드들
    public void PlaySound(string soundName, Vector3 position)
    {
        if (soundDictionary.TryGetValue(soundName, out var soundData))
        {
            PlaySoundAtPosition(soundData, position);
        }
    }
    
    public void PlaySound(string soundName, Transform target)
    {
        PlaySound(soundName, target.position);
    }
    
    public void PlaySound2D(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out var soundData))
        {
            PlaySound2D(soundData);
        }
    }
    
    // 🆕 지속 사운드 재생 (루프)
    public void PlayLoopingSound(string soundName, Vector3 position)
    {
        if (soundDictionary.TryGetValue(soundName, out var soundData))
        {
            PlayLoopingSoundAtPosition(soundData, position);
        }
    }
    
    public void PlayLoopingSound(string soundName, Transform target)
    {
        PlayLoopingSound(soundName, target.position);
    }
    
    // 🆕 지속 사운드 중지
    public void StopLoopingSound(string soundName)
    {
        if (loopingAudioSources.TryGetValue(soundName, out var audioSource))
        {
            audioSource.Stop();
            audioSource.loop = false;
            loopingAudioSources.Remove(soundName);
            
            // 풀로 반환
            if (activeAudioSources.Contains(audioSource))
            {
                activeAudioSources.Remove(audioSource);
            }
            audioSourcePool.Enqueue(audioSource);
        }
    }
    
    // 🆕 모든 지속 사운드 중지
    public void StopAllLoopingSounds()
    {
        foreach (var kvp in loopingAudioSources)
        {
            var audioSource = kvp.Value;
            audioSource.Stop();
            audioSource.loop = false;
            
            if (activeAudioSources.Contains(audioSource))
            {
                activeAudioSources.Remove(audioSource);
            }
            audioSourcePool.Enqueue(audioSource);
        }
        loopingAudioSources.Clear();
    }
    
    // 내부 처리 메서드
    private void PlaySoundAtPosition(SoundData soundData, Vector3 position)
    {
        if (soundData.audioClip == null || audioSourcePool.Count == 0) return;
        
        var audioSource = GetAudioSourceFromPool();
        if (audioSource != null)
        {
            audioSource.transform.position = position;
            audioSource.clip = soundData.audioClip;
            audioSource.spatialBlend = soundData.is3D ? 1f : 0f;
            audioSource.volume = soundData.volume * defaultVolume;
            audioSource.loop = false; // 일회성 사운드
            audioSource.Play();
            
            StartCoroutine(ReturnToPoolAfterPlay(audioSource));
        }
    }
    
    private void PlaySound2D(SoundData soundData)
    {
        if (soundData.audioClip == null || audioSourcePool.Count == 0) return;
        
        var audioSource = GetAudioSourceFromPool();
        if (audioSource != null)
        {
            audioSource.transform.position = Vector3.zero;
            audioSource.clip = soundData.audioClip;
            audioSource.spatialBlend = 0f;
            audioSource.volume = soundData.volume * defaultVolume;
            audioSource.loop = false; // 일회성 사운드
            audioSource.Play();
            
            StartCoroutine(ReturnToPoolAfterPlay(audioSource));
        }
    }
    
    // 🆕 지속 사운드 재생 로직
    private void PlayLoopingSoundAtPosition(SoundData soundData, Vector3 position)
    {
        if (soundData.audioClip == null) return;
        
        // 이미 재생 중인 같은 사운드가 있다면 중지
        if (loopingAudioSources.ContainsKey(soundData.soundName))
        {
            StopLoopingSound(soundData.soundName);
        }
        
        // 풀에서 AudioSource 가져오기
        AudioSource audioSource;
        if (audioSourcePool.Count > 0)
        {
            audioSource = audioSourcePool.Dequeue();
        }
        else
        {
            // 풀이 비어있으면 새로 생성
            var audioSourceObj = new GameObject($"LoopingAudioSource_{soundData.soundName}");
            audioSourceObj.transform.SetParent(transform);
            audioSource = audioSourceObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = soundData.is3D ? 1f : 0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.volume = defaultVolume;
        }
        
        // 지속 사운드 설정
        audioSource.transform.position = position;
        audioSource.clip = soundData.audioClip;
        audioSource.spatialBlend = soundData.is3D ? 1f : 0f;
        audioSource.volume = soundData.volume * defaultVolume;
        audioSource.loop = true; // 루프 설정
        audioSource.Play();
        
        // 지속 사운드 목록에 추가
        loopingAudioSources[soundData.soundName] = audioSource;
        activeAudioSources.Add(audioSource);
    }
    
    private AudioSource GetAudioSourceFromPool()
    {
        if (audioSourcePool.Count > 0)
        {
            var audioSource = audioSourcePool.Dequeue();
            activeAudioSources.Add(audioSource);
            return audioSource;
        }
        
        var newAudioSourceObj = new GameObject($"AudioSource_Extra_{activeAudioSources.Count}");
        newAudioSourceObj.transform.SetParent(transform);
        
        var newAudioSource = newAudioSourceObj.AddComponent<AudioSource>();
        newAudioSource.playOnAwake = false;
        newAudioSource.spatialBlend = 1f;
        newAudioSource.rolloffMode = AudioRolloffMode.Linear;
        newAudioSource.minDistance = minDistance;
        newAudioSource.maxDistance = maxDistance;
        newAudioSource.volume = defaultVolume;
        
        activeAudioSources.Add(newAudioSource);
        return newAudioSource;
    }
    
    private IEnumerator ReturnToPoolAfterPlay(AudioSource audioSource)
    {
        yield return new WaitForSeconds(audioSource.clip.length + 0.1f);
        
        if (activeAudioSources.Contains(audioSource))
        {
            activeAudioSources.Remove(audioSource);
            audioSourcePool.Enqueue(audioSource);
        }
    }
    
    private void OnDestroy()
    {
        if (Inst == this)
        {
            Inst = null;
        }
    }
}
