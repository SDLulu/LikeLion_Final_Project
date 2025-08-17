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
    [Header("👤 플레이어 사운드")]
    [SerializeField] private List<SoundData> playerSounds = new List<SoundData>();

    [Header("👹 적 사운드")]
    [SerializeField] private List<SoundData> enemySounds = new List<SoundData>();

    [Header("🎁 아이템 사운드")]
    [SerializeField] private List<SoundData> itemSounds = new List<SoundData>();

    [Header("🌍 환경 사운드")]
    [SerializeField] private List<SoundData> environmentSounds = new List<SoundData>();
    
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
        
        // 각 섹션별로 사운드 추가
        AddSoundsToDictionary(playerSounds);
        AddSoundsToDictionary(enemySounds);
        AddSoundsToDictionary(itemSounds);
        AddSoundsToDictionary(environmentSounds);
    }

    private void AddSoundsToDictionary(List<SoundData> sounds)
    {
        foreach (var sound in sounds)
        {
            if (sound == null || sound.audioClip == null) continue;

            string keyByFileName = sound.audioClip.name;
            string configuredKey = sound.soundName;

            string primaryKey = !string.IsNullOrEmpty(configuredKey) ? configuredKey : keyByFileName;

            // 비어있는 경우 자동으로 파일명으로 키를 설정
            sound.soundName = primaryKey;

            // 기본 키로 매핑
            AddOrReplaceSoundMapping(primaryKey, sound);

            // 파일명으로도 매핑하여 파일명 호출 지원
            if (primaryKey != keyByFileName)
            {
                AddOrReplaceSoundMapping(keyByFileName, sound);
            }
        }
    }

    private void AddOrReplaceSoundMapping(string key, SoundData data)
    {
        if (string.IsNullOrEmpty(key) || data == null) return;
        soundDictionary[key] = data;
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
        // 먼저 전달된 키로 직접 중지 시도
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
            return;
        }

        // 전달된 키가 파일명 별칭일 수 있으므로, 사운드 데이터 조회 후 기본 키로 재시도
        if (soundDictionary != null && soundDictionary.TryGetValue(soundName, out var soundData))
        {
            string canonicalKey = soundData.soundName; // AddSoundsToDictionary에서 설정된 기본 키
            if (loopingAudioSources.TryGetValue(canonicalKey, out var src))
            {
                src.Stop();
                src.loop = false;
                loopingAudioSources.Remove(canonicalKey);
                
                if (activeAudioSources.Contains(src))
                {
                    activeAudioSources.Remove(src);
                }
                audioSourcePool.Enqueue(src);
                return;
            }
        }
#if UNITY_EDITOR
        Debug.LogWarning($"[AudioManager] 루프 사운드 중지 실패: '{soundName}' (사운드 데이터/루프 목록에서 찾지 못함)");
#endif
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
