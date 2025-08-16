using UnityEngine;

/// <summary>
/// 지속 사운드와 이펙트 사용 예시
/// 드릴, 엔진, 불꽃 등의 지속적인 효과에 활용
/// </summary>
public class LoopingEffectExample : MonoBehaviour
{
    [Header("🎵 지속 사운드")]
    [SerializeField] private string drillSoundName = "DrillLoop";
    [SerializeField] private string engineSoundName = "EngineLoop";
    
    [Header("✨ 지속 이펙트")]
    [SerializeField] private string drillEffectName = "DrillSpark";
    [SerializeField] private string engineEffectName = "EngineSmoke";
    
    [Header("🎮 입력 설정")]
    [SerializeField] private KeyCode drillKey = KeyCode.Space;
    [SerializeField] private KeyCode engineKey = KeyCode.E;
    
    private bool isDrillActive = false;
    private bool isEngineActive = false;
    
    void Update()
    {
        HandleDrillInput();
        HandleEngineInput();
    }
    
    private void HandleDrillInput()
    {
        // 드릴 키를 누르고 있는 동안
        if (Input.GetKeyDown(drillKey))
        {
            StartDrill();
        }
        else if (Input.GetKeyUp(drillKey))
        {
            StopDrill();
        }
    }
    
    private void HandleEngineInput()
    {
        // 엔진 키를 누르고 있는 동안
        if (Input.GetKeyDown(engineKey))
        {
            StartEngine();
        }
        else if (Input.GetKeyUp(engineKey))
        {
            StopEngine();
        }
    }
    
    private void StartDrill()
    {
        if (!isDrillActive)
        {
            isDrillActive = true;
            
            // 드릴 사운드 시작 (루프)
            AudioManager.Inst.PlayLoopingSound(drillSoundName, transform);
            
            // 드릴 이펙트 시작 (루프)
            EffectManager.Inst.PlayLoopingEffect(drillEffectName, transform);
            
            Debug.Log("🔧 드릴 시작!");
        }
    }
    
    private void StopDrill()
    {
        if (isDrillActive)
        {
            isDrillActive = false;
            
            // 드릴 사운드 중지
            AudioManager.Inst.StopLoopingSound(drillSoundName);
            
            // 드릴 이펙트 중지
            EffectManager.Inst.StopLoopingEffect(drillEffectName);
            
            Debug.Log("🔧 드릴 중지!");
        }
    }
    
    private void StartEngine()
    {
        if (!isEngineActive)
        {
            isEngineActive = true;
            
            // 엔진 사운드 시작 (루프)
            AudioManager.Inst.PlayLoopingSound(engineSoundName, transform);
            
            // 엔진 이펙트 시작 (루프)
            EffectManager.Inst.PlayLoopingEffect(engineEffectName, transform);
            
            Debug.Log("🚗 엔진 시작!");
        }
    }
    
    private void StopEngine()
    {
        if (isEngineActive)
        {
            isEngineActive = false;
            
            // 엔진 사운드 중지
            AudioManager.Inst.StopLoopingSound(engineSoundName);
            
            // 엔진 이펙트 중지
            EffectManager.Inst.StopLoopingEffect(engineEffectName);
            
            Debug.Log("🚗 엔진 중지!");
        }
    }
    
    // 🆕 자동 정리 (필요시)
    private void OnDisable()
    {
        // 모든 지속 효과 중지
        AudioManager.Inst.StopAllLoopingSounds();
        EffectManager.Inst.StopAllLoopingEffects();
        
        isDrillActive = false;
        isEngineActive = false;
    }
    
    // 🆕 게임 오브젝트 파괴 시 정리
    private void OnDestroy()
    {
        // 모든 지속 효과 중지
        AudioManager.Inst.StopAllLoopingSounds();
        EffectManager.Inst.StopAllLoopingEffects();
    }
}
