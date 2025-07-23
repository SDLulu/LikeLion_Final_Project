using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Kino
{
    [RequireComponent(typeof(Camera))]
    public class DigitalGlitchURPController : MonoBehaviour
    {
        [Header("Glitch Settings")]
        [SerializeField, Range(0, 1)]
        private float intensity = 0f;
        
        [SerializeField]
        private float durationToTarget = 0.3f;
        
        [SerializeField]
        private float maxGlitch = 1f;

        // Renderer Feature는 에디터에서 직접 추가해야 합니다.
        // [SerializeField] private DigitalGlitchURPRenderer glitchRenderer;
        
        private float timer = 0f;
        private bool active = false;

        private void Awake()
        {
            // 더 이상 rendererFeatures에 접근하지 않습니다.
        }
        
        private void Start()
        {
            // 더 이상 rendererFeatures에 접근하지 않습니다.
        }
        

        public void ToggleGlitch(bool value)
        {
            active = value;
        }
        
        public void SetIntensity(float value)
        {
            intensity = Mathf.Clamp01(value);
            // 효과 강도는 Volume/Renderer Feature에서 직접 조정해야 합니다.
        }
        
        private void Update()
        {
            float direction = active ? 1 : -1;
            
            if ((timer > 0 && direction == -1) || (timer < durationToTarget && direction == 1))
            {
                timer = Mathf.Clamp(timer + Time.deltaTime * direction, 0, durationToTarget);
                float t = timer / durationToTarget;
                float newIntensity = Mathf.Lerp(0, maxGlitch, t);
                SetIntensity(newIntensity);
            }
        }
        
        private void OnDestroy()
        {
            // 더 이상 rendererFeatures에 접근하지 않습니다.
        }
        
        // 인스펙터에서 테스트용
        [ContextMenu("Test Glitch")]
        public void TestGlitch()
        {
            ToggleGlitch(true);
        }
        
        [ContextMenu("Stop Glitch")]
        public void StopGlitch()
        {
            ToggleGlitch(false);
        }
    }
} 