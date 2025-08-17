using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ✨ 이펙트 전용 매니저
public class EffectManager : MonoBehaviour
{
    [System.Serializable]
    public class EffectData
    {
        public string effectName;       // 이펙트 이름 (키)
        public GameObject effectPrefab; // 이펙트 프리팹
        public float duration = 1f;     // 지속시간 (0이면 자동 계산)
    }
    
    [Header("✨ 이펙트 프리팹 리스트")]
    [Header("👤 플레이어 이펙트")]
    [SerializeField] private List<EffectData> playerEffects = new List<EffectData>();

    [Header("👹 적 이펙트")]
    [SerializeField] private List<EffectData> enemyEffects = new List<EffectData>();

    [Header("🎁 아이템 이펙트")]
    [SerializeField] private List<EffectData> itemEffects = new List<EffectData>();

    [Header("🌍 환경 이펙트")]
    [SerializeField] private List<EffectData> environmentEffects = new List<EffectData>();
    
    [Header("🎯 풀링 설정")]
    [SerializeField] private int poolSize = 10;
    
    // 이펙트 풀 (프리팹별로 관리)
    private Dictionary<GameObject, Queue<GameObject>> effectPools;
    private List<GameObject> activeEffects;
    private Dictionary<string, EffectData> effectDictionary;
    
    // 🆕 지속 이펙트 관리를 위한 딕셔너리
    private Dictionary<string, GameObject> loopingEffects;
    
    // 싱글톤 인스턴스
    public static EffectManager Inst { get; private set; }
    
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
        
        InitializeEffectDictionary();
        InitializeEffectPools();
        loopingEffects = new Dictionary<string, GameObject>();
    }
    
    private void InitializeEffectDictionary()
    {
        effectDictionary = new Dictionary<string, EffectData>();
        
        // 각 섹션별로 이펙트 추가
        AddEffectsToDictionary(playerEffects);
        AddEffectsToDictionary(enemyEffects);
        AddEffectsToDictionary(itemEffects);
        AddEffectsToDictionary(environmentEffects);
    }

    private void AddEffectsToDictionary(List<EffectData> effects)
    {
        foreach (var effect in effects)
        {
            if (effect == null || effect.effectPrefab == null) continue;

            string keyByFileName = effect.effectPrefab.name;
            string configuredKey = effect.effectName;

            string primaryKey = !string.IsNullOrEmpty(configuredKey) ? configuredKey : keyByFileName;

            // 비어있는 경우 자동으로 파일명으로 키를 설정
            effect.effectName = primaryKey;

            // 기본 키로 매핑
            AddOrReplaceEffectMapping(primaryKey, effect);

            // 파일명으로도 매핑하여 파일명 호출 지원
            if (primaryKey != keyByFileName)
            {
                AddOrReplaceEffectMapping(keyByFileName, effect);
            }
        }
    }

    private void AddOrReplaceEffectMapping(string key, EffectData data)
    {
        if (string.IsNullOrEmpty(key) || data == null) return;
        effectDictionary[key] = data;
    }
    
    private void InitializeEffectPools()
    {
        effectPools = new Dictionary<GameObject, Queue<GameObject>>();
        activeEffects = new List<GameObject>();
        
        // 각 섹션별로 풀 생성
        CreatePoolsFromList(playerEffects);
        CreatePoolsFromList(enemyEffects);
        CreatePoolsFromList(itemEffects);
        CreatePoolsFromList(environmentEffects);
    }

    private void CreatePoolsFromList(List<EffectData> effects)
    {
        foreach (var effectData in effects)
        {
            if (effectData.effectPrefab != null)
            {
                CreatePool(effectData.effectPrefab);
            }
        }
    }
    
    // 특정 프리팹의 풀 생성
    private void CreatePool(GameObject prefab)
    {
        if (prefab == null) return;
        
        var pool = new Queue<GameObject>();
        
        for (int i = 0; i < poolSize; i++)
        {
            var effect = Instantiate(prefab, transform);
            effect.SetActive(false);
            pool.Enqueue(effect);
        }
        
        effectPools[prefab] = pool;
    }
    
    // 이펙트 재생 메서드들
    public void PlayEffect(string effectName, Vector3 position)
    {
        if (effectDictionary.TryGetValue(effectName, out var effectData))
        {
            PlayEffect(effectData, position);
        }
    }
    
    public void PlayEffect(string effectName, Transform target)
    {
        PlayEffect(effectName, target.position);
    }
    
    public void PlayEffect(string effectName, Vector3 position, Quaternion rotation)
    {
        if (effectDictionary.TryGetValue(effectName, out var effectData))
        {
            PlayEffect(effectData, position, rotation);
        }
    }
    
    // 🆕 지속 이펙트 재생 (루프)
    public void PlayLoopingEffect(string effectName, Vector3 position)
    {
        if (effectDictionary.TryGetValue(effectName, out var effectData))
        {
            PlayLoopingEffect(effectData, position);
        }
    }
    
    public void PlayLoopingEffect(string effectName, Transform target)
    {
        PlayLoopingEffect(effectName, target.position);
    }
    
    public void PlayLoopingEffect(string effectName, Vector3 position, Quaternion rotation)
    {
        if (effectDictionary.TryGetValue(effectName, out var effectData))
        {
            PlayLoopingEffect(effectData, position, rotation);
        }
    }
    
    // 🆕 지속 이펙트 중지
    public void StopLoopingEffect(string effectName)
    {
        if (loopingEffects.TryGetValue(effectName, out var effect))
        {
            effect.SetActive(false);
            loopingEffects.Remove(effectName);
            
            // 풀로 반환
            if (activeEffects.Contains(effect))
            {
                activeEffects.Remove(effect);
            }
            
            // 프리팹에 해당하는 풀 찾기
            foreach (var kvp in effectPools)
            {
                if (kvp.Key.name == effect.name.Replace("(Clone)", ""))
                {
                    kvp.Value.Enqueue(effect);
                    break;
                }
            }
        }
    }
    
    // 🆕 모든 지속 이펙트 중지
    public void StopAllLoopingEffects()
    {
        foreach (var kvp in loopingEffects)
        {
            var effect = kvp.Value;
            effect.SetActive(false);
            
            if (activeEffects.Contains(effect))
            {
                activeEffects.Remove(effect);
            }
            
            // 프리팹에 해당하는 풀 찾기
            foreach (var poolKvp in effectPools)
            {
                if (poolKvp.Key.name == effect.name.Replace("(Clone)", ""))
                {
                    poolKvp.Value.Enqueue(effect);
                    break;
                }
            }
        }
        loopingEffects.Clear();
    }
    
    // 내부 처리 메서드
    private void PlayEffect(EffectData effectData, Vector3 position)
    {
        PlayEffect(effectData, position, Quaternion.identity);
    }
    
    private void PlayEffect(EffectData effectData, Vector3 position, Quaternion rotation)
    {
        if (effectData.effectPrefab == null) return;
        
        var effect = GetFromPool(effectData.effectPrefab);
        if (effect != null)
        {
            effect.transform.position = position;
            effect.transform.rotation = rotation;
            effect.SetActive(true);
            
            StartCoroutine(ReturnToPoolAfterEffect(effect, effectData));
        }
    }
    
    // 🆕 지속 이펙트 재생 로직
    private void PlayLoopingEffect(EffectData effectData, Vector3 position)
    {
        PlayLoopingEffect(effectData, position, Quaternion.identity);
    }
    
    private void PlayLoopingEffect(EffectData effectData, Vector3 position, Quaternion rotation)
    {
        if (effectData.effectPrefab == null) return;
        
        // 이미 재생 중인 같은 이펙트가 있다면 중지
        if (loopingEffects.ContainsKey(effectData.effectName))
        {
            StopLoopingEffect(effectData.effectName);
        }
        
        var effect = GetFromPool(effectData.effectPrefab);
        if (effect != null)
        {
            effect.transform.position = position;
            effect.transform.rotation = rotation;
            effect.SetActive(true);
            
            // 지속 이펙트 목록에 추가
            loopingEffects[effectData.effectName] = effect;
            
            // 지속 이펙트는 자동으로 풀로 반환하지 않음
            // StopLoopingEffect()를 호출할 때까지 계속 재생
        }
    }
    
    private GameObject GetFromPool(GameObject prefab)
    {
        if (!effectPools.ContainsKey(prefab))
        {
            CreatePool(prefab);
        }
        
        var pool = effectPools[prefab];
        if (pool.Count > 0)
        {
            var effect = pool.Dequeue();
            activeEffects.Add(effect);
            return effect;
        }
        
        var newEffect = Instantiate(prefab, transform);
        activeEffects.Add(newEffect);
        return newEffect;
    }
    
    private IEnumerator ReturnToPoolAfterEffect(GameObject effect, EffectData effectData)
    {
        float duration = effectData.duration;
        
        if (duration <= 0f)
        {
            var particleSystem = effect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                duration = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
            }
            else
            {
                var animator = effect.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                    if (clipInfo.Length > 0)
                    {
                        duration = clipInfo[0].clip.length;
                    }
                    else
                    {
                        duration = 1f;
                    }
                }
                else
                {
                    duration = 1f;
                }
            }
        }
        
        yield return new WaitForSeconds(duration);
        
        if (activeEffects.Contains(effect))
        {
            activeEffects.Remove(effect);
            effect.SetActive(false);
            effectPools[effectData.effectPrefab].Enqueue(effect);
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
