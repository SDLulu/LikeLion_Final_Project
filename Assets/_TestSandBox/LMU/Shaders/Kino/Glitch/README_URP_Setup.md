# URP용 Digital Glitch 효과 설정 가이드

## 개요
기존 Post Processing Stack을 사용하는 Kino Glitch 효과를 URP(Universal Render Pipeline)용으로 변환한 버전입니다.

## 파일 구조
```
Assets/_TestSandBox/LMU/Shaders/Kino/Glitch/
├── DigitalGlitchURP.cs              # URP용 메인 스크립트
├── DigitalGlitchURPController.cs    # 효과 제어용 컨트롤러
├── Shader/
│   └── DigitalGlitchURP.shader     # URP용 쉐이더
└── README_URP_Setup.md             # 이 파일
```

## 설정 방법

### 1. 카메라에 컨트롤러 추가
1. 메인 카메라를 선택합니다
2. `DigitalGlitchURPController` 컴포넌트를 추가합니다
3. 인스펙터에서 설정을 조정합니다:
   - **Intensity**: 글리치 효과의 강도 (0-1)
   - **Duration To Target**: 효과가 나타나는 시간
   - **Max Glitch**: 최대 글리치 강도

### 2. 자동 설정
컨트롤러는 자동으로 다음을 수행합니다:
- Renderer Feature를 카메라에 추가
- 쉐이더와 머티리얼 생성
- 노이즈 텍스처 생성

### 3. 코드에서 사용
```csharp
// 컨트롤러 참조
DigitalGlitchURPController glitchController = camera.GetComponent<DigitalGlitchURPController>();

// 글리치 효과 활성화
glitchController.ToggleGlitch(true);

// 글리치 효과 비활성화
glitchController.ToggleGlitch(false);

// 강도 직접 설정
glitchController.SetIntensity(0.5f);
```

## 주요 변경사항

### 기존 버전 vs URP 버전
| 항목 | 기존 버전 | URP 버전 |
|------|-----------|----------|
| 렌더링 방식 | OnRenderImage | ScriptableRenderPass |
| 포스트 프로세싱 | Post Processing Stack | Volume System |
| 쉐이더 언어 | CG | HLSL |
| 카메라 설정 | 컴포넌트 기반 | Renderer Feature |

### 호환성
- ✅ Unity 2022.3 LTS 이상
- ✅ URP 14.0 이상
- ✅ 모든 플랫폼 지원

## 문제 해결

### 1. 쉐이더가 보이지 않는 경우
- URP Asset에서 "Depth Texture" 활성화 확인
- 카메라의 "Depth Texture" 옵션 활성화

### 2. 효과가 적용되지 않는 경우
- 카메라에 `DigitalGlitchURPController` 컴포넌트가 추가되었는지 확인
- URP Asset의 Renderer Feature가 올바르게 설정되었는지 확인

### 3. 성능 문제
- 모바일에서는 `intensity` 값을 낮게 설정
- `maxGlitch` 값을 조정하여 성능 최적화

## 예제 사용법

### 간단한 트리거
```csharp
public class GlitchTrigger : MonoBehaviour
{
    private DigitalGlitchURPController glitchController;
    
    void Start()
    {
        glitchController = Camera.main.GetComponent<DigitalGlitchURPController>();
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            glitchController.ToggleGlitch(true);
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            glitchController.ToggleGlitch(false);
        }
    }
}
```

### 시간 기반 효과
```csharp
public class TimedGlitch : MonoBehaviour
{
    private DigitalGlitchURPController glitchController;
    public float glitchInterval = 5f;
    public float glitchDuration = 1f;
    
    void Start()
    {
        glitchController = Camera.main.GetComponent<DigitalGlitchURPController>();
        StartCoroutine(GlitchRoutine());
    }
    
    IEnumerator GlitchRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(glitchInterval);
            glitchController.ToggleGlitch(true);
            yield return new WaitForSeconds(glitchDuration);
            glitchController.ToggleGlitch(false);
        }
    }
}
```

## 라이선스
원본 Kino Glitch 효과와 동일한 MIT 라이선스를 따릅니다. 