# BGMManager 사용법

## 📋 개요
BGMManager는 각 스테이지별로 BGM을 관리하고 반복재생/정지 기능을 제공하는 매니저입니다.

## 🎵 주요 기능
- **스테이지별 BGM 관리**: 각 스테이지마다 다른 BGM 설정 가능
- **자동 반복재생**: 설정에 따라 BGM 자동 루프
- **페이드 효과**: 부드러운 전환을 위한 페이드인/아웃
- **볼륨 제어**: 마스터 볼륨 및 개별 BGM 볼륨 설정
- **뮤트 기능**: 전체 BGM 음소거/해제

## 🚀 사용법

### 1. 기본 설정
```csharp
// BGMManager 프리팹을 씬에 배치
// Inspector에서 각 스테이지별 BGM 설정:
// - Stage Name: "Stage1", "Stage2" 등
// - BGM Clip: 해당 스테이지의 음악 파일
// - Volume: 개별 볼륨 (0f ~ 1f)
// - Fade In Time: 페이드인 시간
// - Fade Out Time: 페이드아웃 시간
```

### 2. BGM 재생/정지
```csharp
// 특정 스테이지 BGM 재생
BGMManager.Inst.PlayBGM("Stage1");

// BGM 정지 (페이드아웃과 함께)
BGMManager.Inst.StopBGM();

// BGM 정지 (즉시)
BGMManager.Inst.StopBGM(false);

// 일시정지/재개
BGMManager.Inst.PauseBGM();
BGMManager.Inst.ResumeBGM();
```

### 3. 볼륨 및 설정 제어
```csharp
// 마스터 볼륨 설정
BGMManager.Inst.SetMasterVolume(0.8f);

// 특정 스테이지 BGM 볼륨 설정
BGMManager.Inst.SetBGMVolume("Stage1", 0.7f);

// 뮤트 설정
BGMManager.Inst.SetMute(true);

// 루프 설정
BGMManager.Inst.SetLooping(false);
```

### 4. 상태 확인
```csharp
// 현재 재생 중인 BGM 이름
string currentBGM = BGMManager.Inst.CurrentBGMName;

// 재생 상태 확인
bool isPlaying = BGMManager.Inst.IsPlaying;

// 뮤트 상태 확인
bool isMuted = BGMManager.Inst.IsMuted;

// 루프 상태 확인
bool isLooping = BGMManager.Inst.IsLooping;

// 현재 BGM 진행 시간
float currentTime = BGMManager.Inst.GetCurrentBGMTime();

// 현재 BGM 총 길이
float totalLength = BGMManager.Inst.GetCurrentBGMLength();
```

## 🎮 실제 게임에서 사용 예시

### 스테이지 전환 시
```csharp
public class StageManager : MonoBehaviour
{
    [SerializeField] private string stageName;
    
    private void Start()
    {
        // 스테이지 시작 시 해당 BGM 재생
        BGMManager.Inst.PlayBGM(stageName);
    }
    
    private void OnDestroy()
    {
        // 스테이지 종료 시 BGM 정지
        BGMManager.Inst.StopBGM();
    }
}
```

### 메뉴에서 BGM 제어
```csharp
public class MenuController : MonoBehaviour
{
    public void OnBGMVolumeChanged(float volume)
    {
        BGMManager.Inst.SetMasterVolume(volume);
    }
    
    public void OnBGMMuteToggled(bool isMuted)
    {
        BGMManager.Inst.SetMute(isMuted);
    }
}
```

### 게임 일시정지 시
```csharp
public class GamePauseManager : MonoBehaviour
{
    public void PauseGame()
    {
        // 게임 일시정지 시 BGM도 일시정지
        BGMManager.Inst.PauseBGM();
    }
    
    public void ResumeGame()
    {
        // 게임 재개 시 BGM도 재개
        BGMManager.Inst.ResumeBGM();
    }
}
```

## ⚠️ 주의사항
1. **프리팹 설정**: BGMManager 프리팹을 씬에 배치해야 합니다
2. **스테이지 이름**: 각 BGM의 Stage Name은 고유해야 합니다
3. **오디오 파일**: BGM Clip에 실제 음악 파일을 할당해야 합니다
4. **싱글톤**: DontDestroyOnLoad로 설정되어 씬 전환 시에도 유지됩니다

## 🔧 커스터마이징
- `fadeInTime`, `fadeOutTime`을 0으로 설정하면 페이드 효과를 비활성화할 수 있습니다
- `isLooping`을 false로 설정하면 반복재생이 비활성화됩니다
- `masterVolume`으로 전체 BGM 볼륨을 조절할 수 있습니다
