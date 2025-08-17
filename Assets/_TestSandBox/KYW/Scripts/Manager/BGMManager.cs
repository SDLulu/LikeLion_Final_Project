using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 🎵 BGM 전용 매니저
public class BGMManager : MonoBehaviour
{
    private const string BGM_VOLUME_PREF_KEY = "BGMVolume";
    [System.Serializable]
    public class BGMData
    {
        public string stageName;        // 스테이지 이름 (키)
        public string bgmName;          // BGM 이름
        public AudioClip bgmClip;       // BGM 오디오 클립
        public float volume = 1f;       // 개별 볼륨
        [Range(0f, 1f)]
        public float fadeInTime = 1f;   // 페이드인 시간
        [Range(0f, 1f)]
        public float fadeOutTime = 1f;  // 페이드아웃 시간
    }
    
    [Header("🎵 BGM 리스트")]
    [SerializeField] private List<BGMData> bgmList = new List<BGMData>();
    
    [Header("🔊 설정")]
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private bool isMuted = false;
    [SerializeField] private bool isLooping = true;
    
    // AudioSource 컴포넌트
    private AudioSource bgmAudioSource;
    
    // 현재 재생 중인 BGM 정보
    private string currentBGMName;
    private Coroutine fadeCoroutine;
    
    // 싱글톤 인스턴스
    public static BGMManager Inst { get; private set; }
    
    // 현재 재생 중인 BGM 이름 (읽기 전용)
    public string CurrentBGMName { get { return currentBGMName; } }
    
    // BGM이 재생 중인지 확인 (읽기 전용)
    public bool IsPlaying { get { return bgmAudioSource != null && bgmAudioSource.isPlaying; } }
    
    // 뮤트 상태 (읽기 전용)
    public bool IsMuted { get { return isMuted; } }
    
    // 루프 상태 (읽기 전용)
    public bool IsLooping { get { return isLooping; } }
    
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
        
        InitializeBGM();

        float savedVolume = PlayerPrefs.GetFloat(BGM_VOLUME_PREF_KEY, 1f);
        if (savedVolume < 0f || savedVolume > 1f)
        {
            savedVolume = 1f;
        }
        SetMasterVolume(savedVolume);
    }
    
    private void InitializeBGM()
    {
        // AudioSource 컴포넌트 추가
        bgmAudioSource = gameObject.AddComponent<AudioSource>();
        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.loop = isLooping;
        bgmAudioSource.volume = masterVolume;
        
        // 2D 사운드로 설정 (BGM은 일반적으로 2D)
        bgmAudioSource.spatialBlend = 0f;
    }
    
    /// <summary>
    /// 특정 BGM을 재생합니다.
    /// </summary>
    /// <param name="bgmName">BGM 이름</param>
    /// <param name="fadeIn">페이드인 사용 여부</param>
    public void PlayBGM(string bgmName, bool fadeIn = true)
    {
        BGMData bgmData = GetBGMDataByBGMName(bgmName);
        if (bgmData == null || bgmData.bgmClip == null)
        {
            Debug.LogWarning($"[BGMManager] BGM을 찾을 수 없습니다: {bgmName}");
            return;
        }
        
        // 동일한 BGM이 이미 재생 중이면 재생 요청 무시
        if (IsPlaying)
        {
            if (currentBGMName == bgmName)
            {
                if (bgmAudioSource != null)
                {
                    if (bgmAudioSource.clip == bgmData.bgmClip)
                    {
                        Debug.Log($"[BGMManager] 동일한 BGM이 이미 재생 중입니다: {bgmName}");
                        return;
                    }
                }
            }
        }
        
        // 현재 재생 중인 BGM이 있다면 즉시 정지 (동일 AudioSource 충돌 방지)
        if (IsPlaying)
        {
            StopBGM(false);
        }
        
        // 새로운 BGM 설정
        currentBGMName = bgmName;
        bgmAudioSource.clip = bgmData.bgmClip;
        bgmAudioSource.volume = masterVolume;
        
        // 페이드인 적용
        if (fadeIn && bgmData.fadeInTime > 0f)
        {
            StartFadeIn(bgmData.fadeInTime, 1f);
        }
        else
        {
            bgmAudioSource.volume = masterVolume;
            bgmAudioSource.Play();
        }
        
        Debug.Log($"[BGMManager] BGM 재생 시작: {bgmName}");
    }
    
    /// <summary>
    /// 현재 재생 중인 BGM을 정지합니다.
    /// </summary>
    /// <param name="fadeOut">페이드아웃 사용 여부</param>
    public void StopBGM(bool fadeOut = true, float fadeTime = 1f)
    {
        if (IsPlaying == false)
        {
            return;
        }
        
        if (fadeOut)
        {
            BGMData currentBGM = GetBGMDataByBGMName(currentBGMName);
            StartFadeOut(fadeTime);
        }
        else
        {
            bgmAudioSource.Stop();
            currentBGMName = "";
            Debug.Log("[BGMManager] BGM 정지");
        }
    }
    
    /// <summary>
    /// BGM을 일시정지합니다.
    /// </summary>
    public void PauseBGM()
    {
        if (IsPlaying)
        {
            bgmAudioSource.Pause();
            Debug.Log("[BGMManager] BGM 일시정지");
        }
    }
    
    /// <summary>
    /// 일시정지된 BGM을 재개합니다.
    /// </summary>
    public void ResumeBGM()
    {
        if (bgmAudioSource.clip != null && bgmAudioSource.isPlaying == false)
        {
            bgmAudioSource.UnPause();
            Debug.Log("[BGMManager] BGM 재개");
        }
    }
    
    /// <summary>
    /// 마스터 볼륨을 설정합니다.
    /// </summary>
    /// <param name="volume">볼륨 (0f ~ 1f)</param>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = masterVolume;
        }
    }
    
    /// <summary>
    /// 특정 스테이지의 BGM 볼륨을 설정합니다.
    /// </summary>
    /// <param name="stageName">스테이지 이름</param>
    /// <param name="volume">볼륨 (0f ~ 1f)</param>
    public void SetBGMVolume(string stageName, float volume)
    {
        BGMData bgmData = GetBGMData(stageName);
        if (bgmData != null)
        {
            bgmData.volume = Mathf.Clamp01(volume);
            
            // 현재 재생 중인 BGM이라면 즉시 적용
            if (currentBGMName == stageName && IsPlaying)
            {
                bgmAudioSource.volume = masterVolume;
            }
        }
    }
    
    /// <summary>
    /// BGM을 뮤트/언뮤트합니다.
    /// </summary>
    /// <param name="mute">뮤트 여부</param>
    public void SetMute(bool mute)
    {
        isMuted = mute;
        if (bgmAudioSource != null)
        {
            bgmAudioSource.mute = mute;
        }
        Debug.Log($"[BGMManager] BGM 뮤트: {mute}");
    }
    
    /// <summary>
    /// BGM 루프 설정을 변경합니다.
    /// </summary>
    /// <param name="loop">루프 여부</param>
    public void SetLooping(bool loop)
    {
        isLooping = loop;
        if (bgmAudioSource != null)
        {
            bgmAudioSource.loop = loop;
        }
        Debug.Log($"[BGMManager] BGM 루프: {loop}");
    }
    
    /// <summary>
    /// 특정 스테이지의 BGM 데이터를 가져옵니다.
    /// </summary>
    /// <param name="stageName">스테이지 이름</param>
    /// <returns>BGM 데이터</returns>
    private BGMData GetBGMData(string stageName)
    {
        if (string.IsNullOrEmpty(stageName))
        {
            return null;
        }
        
        foreach (var bgm in bgmList)
        {
            if (bgm.stageName == stageName)
            {
                return bgm;
            }
        }
        return null;
    }

    /// <summary>
    /// BGM 이름으로 BGM 데이터를 가져옵니다.
    /// </summary>
    /// <param name="bgmName">BGM 이름</param>
    /// <returns>BGM 데이터</returns>
    private BGMData GetBGMDataByBGMName(string bgmName)
    {
        if (string.IsNullOrEmpty(bgmName))
        {
            return null;
        }

        foreach (var bgm in bgmList)
        {
            if (bgm.bgmName == bgmName)
            {
                return bgm;
            }
        }

        return null;
    }
    
    /// <summary>
    /// 페이드인 효과를 시작합니다.
    /// </summary>
    /// <param name="fadeTime">페이드 시간</param>
    /// <param name="targetVolume">목표 볼륨</param>
    private void StartFadeIn(float fadeTime, float targetVolume)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        fadeCoroutine = StartCoroutine(FadeInCoroutine(fadeTime, targetVolume));
    }
    
    /// <summary>
    /// 페이드아웃 효과를 시작합니다.
    /// </summary>
    /// <param name="fadeTime">페이드 시간</param>
    private void StartFadeOut(float fadeTime)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        fadeCoroutine = StartCoroutine(FadeOutCoroutine(fadeTime));
    }
    
    /// <summary>
    /// 페이드인 코루틴
    /// </summary>
    private IEnumerator FadeInCoroutine(float fadeTime, float targetVolume)
    {
        float startVolume = 0f;
        float currentTime = 0f;
        
        bgmAudioSource.volume = startVolume;
        bgmAudioSource.Play();
        
        while (currentTime < fadeTime)
        {
            currentTime += Time.deltaTime;
            float normalizedTime = currentTime / fadeTime;
            bgmAudioSource.volume = Mathf.Lerp(startVolume, targetVolume * masterVolume, normalizedTime);
            yield return null;
        }
        
        bgmAudioSource.volume = targetVolume * masterVolume;
        fadeCoroutine = null;
    }
    
    /// <summary>
    /// 페이드아웃 코루틴
    /// </summary>
    private IEnumerator FadeOutCoroutine(float fadeTime)
    {
        float startVolume = bgmAudioSource.volume;
        float currentTime = 0f;
        
        while (currentTime < fadeTime)
        {
            currentTime += Time.deltaTime;
            float normalizedTime = currentTime / fadeTime;
            bgmAudioSource.volume = Mathf.Lerp(startVolume, 0f, normalizedTime);
            yield return null;
        }
        
        bgmAudioSource.Stop();
        currentBGMName = "";
        fadeCoroutine = null;
        Debug.Log("[BGMManager] BGM 페이드아웃 완료");
    }
    
    /// <summary>
    /// 모든 BGM을 정지하고 리소스를 정리합니다.
    /// </summary>
    public void StopAllBGM()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
            bgmAudioSource.clip = null;
        }
        
        currentBGMName = "";
        Debug.Log("[BGMManager] 모든 BGM 정지");
    }
    
    /// <summary>
    /// 현재 재생 중인 BGM의 진행 시간을 가져옵니다.
    /// </summary>
    /// <returns>진행 시간 (초)</returns>
    public float GetCurrentBGMTime()
    {
        return bgmAudioSource != null ? bgmAudioSource.time : 0f;
    }
    
    /// <summary>
    /// 현재 재생 중인 BGM의 총 길이를 가져옵니다.
    /// </summary>
    /// <returns>총 길이 (초)</returns>
    public float GetCurrentBGMLength()
    {
        return bgmAudioSource != null && bgmAudioSource.clip != null ? bgmAudioSource.clip.length : 0f;
    }
    
    private void OnDestroy()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
    }
}
