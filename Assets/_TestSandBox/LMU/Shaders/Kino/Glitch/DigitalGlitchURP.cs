using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_2023_2_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
#endif

namespace Kino
{
    [System.Serializable]
    public class DigitalGlitchURP : VolumeComponent, IPostProcessComponent
    {
        [Range(0, 1)]
        public FloatParameter intensity = new FloatParameter(0f);

        public bool IsActive() => intensity.value > 0f;
        public bool IsTileCompatible() => false;
    }

    public class DigitalGlitchURPRenderer : ScriptableRendererFeature
    {
        [System.Serializable]
        public class DigitalGlitchURPSettings
        {
            [Range(0, 1)]
            public float intensity = 0f;
        }

        public DigitalGlitchURPSettings settings = new DigitalGlitchURPSettings();
        private DigitalGlitchURPPass m_ScriptablePass;

        public override void Create()
        {
            m_ScriptablePass = new DigitalGlitchURPPass(settings);
            m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }

        protected override void Dispose(bool disposing)
        {
            m_ScriptablePass?.Dispose();
        }
    }

    public class DigitalGlitchURPPass : ScriptableRenderPass
    {
        private Material m_Material;
        private Texture2D m_NoiseTexture;
        private RenderTexture m_TrashFrame1;
        private RenderTexture m_TrashFrame2;
        private DigitalGlitchURPRenderer.DigitalGlitchURPSettings m_Settings;
        private static readonly int IntensityProperty = Shader.PropertyToID("_Intensity");
        private static readonly int NoiseTexProperty = Shader.PropertyToID("_NoiseTex");
        private static readonly int TrashTexProperty = Shader.PropertyToID("_TrashTex");

        public DigitalGlitchURPPass(DigitalGlitchURPRenderer.DigitalGlitchURPSettings settings)
        {
            m_Settings = settings;
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Settings.intensity <= 0f) return;

            var cmd = CommandBufferPool.Get("Digital Glitch URP");
            
            if (m_Material == null)
            {
                var shader = Shader.Find("Hidden/Kino/Glitch/Digital");
                if (shader != null)
                {
                    m_Material = new Material(shader);
                    m_Material.hideFlags = HideFlags.DontSave;
                }
                else
                {
                    Debug.LogError("Digital Glitch shader not found!");
                    return;
                }
            }

            if (m_NoiseTexture == null)
            {
                m_NoiseTexture = new Texture2D(32, 16, TextureFormat.ARGB32, false);
                m_NoiseTexture.hideFlags = HideFlags.DontSave;
                m_NoiseTexture.wrapMode = TextureWrapMode.Clamp;
                m_NoiseTexture.filterMode = FilterMode.Point;
            }

            if (m_TrashFrame1 == null || m_TrashFrame1.width != Screen.width || m_TrashFrame1.height != Screen.height)
            {
                if (m_TrashFrame1 != null) m_TrashFrame1.Release();
                if (m_TrashFrame2 != null) m_TrashFrame2.Release();
                
                m_TrashFrame1 = new RenderTexture(Screen.width, Screen.height, 0);
                m_TrashFrame2 = new RenderTexture(Screen.width, Screen.height, 0);
                m_TrashFrame1.hideFlags = HideFlags.DontSave;
                m_TrashFrame2.hideFlags = HideFlags.DontSave;
            }

            // Update noise texture
            if (Random.value > Mathf.Lerp(0.9f, 0.5f, m_Settings.intensity))
            {
                UpdateNoiseTexture();
            }

            // Update trash frames
            var fcount = Time.frameCount;
            var colorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            if (fcount % 13 == 0) 
            {
                cmd.Blit(colorTarget, m_TrashFrame1);
            }
            if (fcount % 73 == 0) 
            {
                cmd.Blit(colorTarget, m_TrashFrame2);
            }

            m_Material.SetFloat(IntensityProperty, m_Settings.intensity);
            m_Material.SetTexture(NoiseTexProperty, m_NoiseTexture);
            var trashFrame = Random.value > 0.5f ? m_TrashFrame1 : m_TrashFrame2;
            m_Material.SetTexture(TrashTexProperty, trashFrame);

            // 최종 결과를 다시 카메라 타겟에 Blit
            cmd.Blit(colorTarget, colorTarget, m_Material);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

#if UNITY_2023_2_OR_NEWER
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // TODO: RenderGraph 방식으로 변환 필요
            // 현재는 에러 방지용 빈 함수
        }
#endif

        private void UpdateNoiseTexture()
        {
            var color = RandomColor();

            for (var y = 0; y < m_NoiseTexture.height; y++)
            {
                for (var x = 0; x < m_NoiseTexture.width; x++)
                {
                    if (Random.value > 0.89f) color = RandomColor();
                    m_NoiseTexture.SetPixel(x, y, color);
                }
            }

            m_NoiseTexture.Apply();
        }

        private static Color RandomColor()
        {
            return new Color(Random.value, Random.value, Random.value, Random.value);
        }

        public void Dispose()
        {
            if (m_Material != null)
            {
                CoreUtils.Destroy(m_Material);
                m_Material = null;
            }
            if (m_NoiseTexture != null)
            {
                CoreUtils.Destroy(m_NoiseTexture);
                m_NoiseTexture = null;
            }
            if (m_TrashFrame1 != null)
            {
                m_TrashFrame1.Release();
                m_TrashFrame1 = null;
            }
            if (m_TrashFrame2 != null)
            {
                m_TrashFrame2.Release();
                m_TrashFrame2 = null;
            }
        }
    }
} 