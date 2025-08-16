# AudioManager & EffectManager 사용 가이드

## 🎵 AudioManager (오디오 매니저)

### 기본 기능
- **3D 사운드**: 위치 기반 사운드 재생
- **오디오 풀링**: 성능 최적화를 위한 AudioSource 재사용
- **거리 기반 볼륨**: 거리에 따른 자동 볼륨 조절

### 사용법

#### 1. 일회성 사운드 재생
```csharp
// 위치 기반
AudioManager.Inst.PlaySound("Explosion", transform.position);

// Transform 기반
AudioManager.Inst.PlaySound("Jump", playerTransform);

// 2D 사운드 (UI 효과음 등)
AudioManager.Inst.PlaySound2D("ButtonClick");
```

#### 2. 🆕 지속 사운드 재생 (루프)
```csharp
// 드릴, 엔진 등 지속적인 사운드
AudioManager.Inst.PlayLoopingSound("DrillLoop", transform.position);

// 중지
AudioManager.Inst.StopLoopingSound("DrillLoop");

// 모든 지속 사운드 중지
AudioManager.Inst.StopAllLoopingSounds();
```

### 사운드 설정
Inspector에서 `soundList`에 사운드를 추가:
- `soundName`: 사운드 식별자
- `audioClip`: 실제 오디오 파일
- `volume`: 볼륨 (0~1)
- `is3D`: 3D 사운드 여부

## ✨ EffectManager (이펙트 매니저)

### 기본 기능
- **이펙트 풀링**: GameObject 재사용으로 성능 최적화
- **자동 정리**: 이펙트 완료 후 자동으로 풀로 반환
- **위치/회전 제어**: 정확한 위치와 회전으로 이펙트 배치

### 사용법

#### 1. 일회성 이펙트 재생
```csharp
// 위치 기반
EffectManager.Inst.PlayEffect("Explosion", transform.position);

// Transform 기반
EffectManager.Inst.PlayEffect("Spark", playerTransform);

// 위치 + 회전
EffectManager.Inst.PlayEffect("Fire", transform.position, Quaternion.identity);
```

#### 2. 🆕 지속 이펙트 재생 (루프)
```csharp
// 불꽃, 연기 등 지속적인 이펙트
EffectManager.Inst.PlayLoopingEffect("FireLoop", transform.position);

// 중지
EffectManager.Inst.StopLoopingEffect("FireLoop");

// 모든 지속 이펙트 중지
EffectManager.Inst.StopAllLoopingEffects();
```

### 이펙트 설정
Inspector에서 `effectList`에 이펙트를 추가:
- `effectName`: 이펙트 식별자
- `effectPrefab`: 이펙트 프리팹
- `duration`: 재생 시간 (0이면 자동 감지)

## 🎮 실제 사용 예시

### 드릴 시스템
```csharp
public class DrillController : MonoBehaviour
{
    void Update()
    {
        // 스페이스바를 누르고 있는 동안
        if (Input.GetKey(KeyCode.Space))
        {
            // 드릴 사운드 루프 재생
            AudioManager.Inst.PlayLoopingSound("DrillLoop", transform);
            
            // 드릴 스파크 이펙트 루프 재생
            EffectManager.Inst.PlayLoopingEffect("DrillSpark", transform);
        }
        else
        {
            // 키를 놓으면 중지
            AudioManager.Inst.StopLoopingSound("DrillLoop");
            EffectManager.Inst.StopLoopingEffect("DrillSpark");
        }
    }
}
```

### 엔진 시스템
```csharp
public class EngineController : MonoBehaviour
{
    private bool isEngineRunning = false;
    
    public void StartEngine()
    {
        if (!isEngineRunning)
        {
            isEngineRunning = true;
            
            // 엔진 사운드 루프 시작
            AudioManager.Inst.PlayLoopingSound("EngineLoop", transform);
            
            // 엔진 연기 이펙트 루프 시작
            EffectManager.Inst.PlayLoopingEffect("EngineSmoke", transform);
        }
    }
    
    public void StopEngine()
    {
        if (isEngineRunning)
        {
            isEngineRunning = false;
            
            // 엔진 사운드/이펙트 중지
            AudioManager.Inst.StopLoopingSound("EngineLoop");
            EffectManager.Inst.StopLoopingEffect("EngineSmoke");
        }
    }
}
```

## 🔧 고급 기능

### 1. 지속 효과 관리
```csharp
// 특정 효과만 중지
AudioManager.Inst.StopLoopingSound("DrillLoop");
EffectManager.Inst.StopLoopingEffect("DrillSpark");

// 모든 지속 효과 중지
AudioManager.Inst.StopAllLoopingSounds();
EffectManager.Inst.StopAllLoopingEffects();
```

### 2. 자동 정리
```csharp
private void OnDisable()
{
    // 컴포넌트 비활성화 시 모든 지속 효과 중지
    AudioManager.Inst.StopAllLoopingSounds();
    EffectManager.Inst.StopAllLoopingEffects();
}
```

### 3. 조건부 재생
```csharp
void Update()
{
    if (Input.GetKey(KeyCode.Space) && hasFuel)
    {
        // 연료가 있을 때만 드릴 작동
        AudioManager.Inst.PlayLoopingSound("DrillLoop", transform);
        EffectManager.Inst.PlayLoopingEffect("DrillSpark", transform);
    }
    else
    {
        // 연료가 없거나 키를 놓으면 중지
        AudioManager.Inst.StopLoopingSound("DrillLoop");
        EffectManager.Inst.StopLoopingEffect("DrillSpark");
    }
}
```

## 📝 주의사항

### 1. 메모리 관리
- 지속 효과는 수동으로 중지해야 함
- `OnDisable()` 또는 `OnDestroy()`에서 정리 필수
- 너무 많은 지속 효과 동시 재생 시 성능 저하 가능

### 2. 사운드 설정
- 지속 사운드는 `loop = true`로 설정됨
- 볼륨과 거리 설정으로 적절한 사운드 믹스 조절
- 3D 사운드와 2D 사운드 구분하여 사용

### 3. 이펙트 설정
- 지속 이펙트는 자동으로 풀로 반환되지 않음
- ParticleSystem의 `loop` 속성 활용 권장
- 적절한 `duration` 설정으로 메모리 효율성 확보

## 🚀 성능 최적화 팁

1. **풀 크기 조정**: `poolSize`를 적절히 설정
2. **동시 재생 제한**: 너무 많은 지속 효과 동시 재생 방지
3. **거리 기반 활성화**: 멀리 있는 오브젝트의 이펙트는 비활성화
4. **LOD 시스템**: 거리에 따른 이펙트 품질 조절

---

**작성일**: 2024년
**버전**: 2.0 (지속 효과 기능 추가)
**작성자**: AI Assistant
**프로젝트**: LikeLion Final Project - KYW
