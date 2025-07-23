# 🎮 LikeLion Final Project - 프로젝트 구조

## 📁 **프로젝트 폴더 구조**

### 🏗️ **메인 스크립트 구조** (`Assets/Scripts/`)
- **Lobby/**: 로비 관련 UI 및 관리 스크립트
  - `LobbyUIManager.cs`: 로비 UI 전체 관리
  - `CreateNickNamePanel.cs`: 닉네임 생성 패널
  - `LoadingCavnasController.cs`: 로딩 화면 컨트롤러
  - `LobbyPanelBase.cs`: 로비 패널 베이스 클래스
  - `MiddleSectionPanel.cs`: 중앙 섹션 패널

- **MainGame/**: 메인 게임 관련 스크립트
  - `PlayerSpawnerController.cs`: 플레이어 스폰 관리

- **Other/**: 공통 및 유틸리티 스크립트
  - `NetworkRunnerController.cs`: 포톤 퓨전 네트워크 러너 관리
  - `GlobalManagers.cs`: 전역 매니저
  - `DDOL.cs`: DontDestroyOnLoad 오브젝트
  - `Utils.cs`: 유틸리티 함수들

- **PMK/**: 타일 및 맵 관련 스크립트
  - `PMK_Tile_Rogic.cs`: 타일 로직 (11KB, 메인 타일 시스템)
  - `PMK_Bricks.cs`: 벽돌 관련
  - `PNK_Tile_Wall.cs`: 벽 타일

- **UGS/**: Unity Gaming Services 관련 데이터
  - `FakeClient.Data.cs`: 가짜 클라이언트 데이터
  - `Skin.Data.cs`: 스킨 데이터
  - `Stage.Data.cs`: 스테이지 데이터

### 🧪 **테스트 샌드박스 구조** (`Assets/_TestSandBox/`)

#### **KYW (개발자) 폴더** (`Assets/_TestSandBox/KYW/Scripts/`)
- **Core/**: 핵심 시스템 스크립트
  - `SpelunkyPlayerController.cs`: 메인 플레이어 컨트롤러 (9.1KB)
  - `DevAutoStarter.cs`: 개발용 자동 네트워크 시작 (13KB)
  - `PlayerSpawner.cs`: 플레이어 스폰 전용 (2.5KB)
  - `SpelunkyNetworkInitializer.cs`: 네트워크 초기화 (3.5KB)
  - `SpelunkyLocalInputPoller.cs`: 로컬 입력 폴링 (3.1KB)
  - `SpelunkyPlayerData.cs`: 플레이어 데이터 (1.1KB)
  - `PlayerState.cs`: 플레이어 상태 및 액션 열거형 (5.3KB)

- **Control/**: 플레이어 제어 관련
  - `PlayerMovement.cs`: 플레이어 이동 (3.2KB)
  - `PlayerJump.cs`: 플레이어 점프 (5.2KB)
  - `PlayerClimbing.cs`: 사다리 등반 (4.8KB)
  - `PlayerAnimation.cs`: 애니메이션 제어 (3.4KB)
  - `PlayerItemPickup.cs`: 아이템 줍기 (11KB)
  - `PlayerItemUsage.cs`: 아이템 사용 (6.3KB)
  - `PlayerItemThrower.cs`: 아이템 던지기 (5.2KB)
  - `PlayerShiftSkill.cs`: 시프트 스킬 (4.1KB)
  - `Check/`: 체크 관련 서브 폴더

- **Items/**: 아이템 시스템
  - `IUsableItem.cs`: 사용 가능한 아이템 인터페이스 (402B)
  - `BasicPunchItem.cs`: 기본 펀치 아이템 (3.6KB)
  - `Shotgun.cs`: 샷건 (3.8KB)
  - `MachineGun.cs`: 머신건 (4.0KB)
  - `Pickaxe.cs`: 곡괭이 (5.3KB)
  - `Bullets.cs`: 총알 (1.1KB)

- **Physics/**: 물리 및 충돌 처리
  - `IHitReaction.cs`: 히트 반응 인터페이스 (1KB)
  - `PlayerHitReaction.cs`: 플레이어 히트 반응 (3.7KB)
  - `ExampleHitReaction.cs`: 예시 히트 반응 (4.3KB)

- **Util/**: 유틸리티 및 디버그
  - `GroundCheckVisualizer.cs`: 지면 체크 시각화 (2.3KB)
  - `LadderCheckVisualizer.cs`: 사다리 체크 시각화 (2.3KB)

- **Input/**: 입력 처리 (현재 비어있음)
- **Data/**: 데이터 관련 (현재 비어있음)
- **Debug/**: 디버그 관련 (현재 비어있음)

#### **LMU (다른 개발자) 폴더** (`Assets/_TestSandBox/LMU/Scripts/`)
- **Core/**: 핵심 시스템
- **Player/**: 플레이어 관련
- **Network/**: 네트워크 관련
- **UI/**: UI 관련
- **Input/**: 입력 처리
- **Editor/**: 에디터 스크립트
- **Scriptable/**: ScriptableObject 관련
- `LocalSceneManager.cs`: 로컬 씬 관리
- `DataManager.cs`: 데이터 관리
- `GlobalSetting.cs`: 전역 설정
- `FastRunController.cs`: 빠른 실행 컨트롤러

## 📝 **개발 규칙**

### 📁 **파일 위치 규칙**
- **새 스크립트 생성 시**: 해당 기능에 맞는 폴더에 배치
  - 플레이어 제어 → `Control/`
  - 아이템 관련 → `Items/`
  - 물리/충돌 → `Physics/`
  - 유틸리티 → `Util/`
  - 핵심 시스템 → `Core/`
- **테스트용 스크립트**: `_TestSandBox/KYW/Scripts/` 하위에 배치
- **메인 게임 스크립트**: `Assets/Scripts/` 하위에 배치

### 🎯 **네이밍 규칙**
- **클래스명**: PascalCase (예: `SpelunkyPlayerController`)
- **변수명**: camelCase (예: `playerMovement`)
- **상수**: UPPER_SNAKE_CASE (예: `MAX_PLAYER_COUNT`)
- **프로퍼티**: PascalCase (예: `PlayerHealth`)

### 🔧 **기술적 특징**
- **네트워크**: 포톤 퓨전 2 공식 문서 최신 정보 기반
- **입력 처리**: Fusion 2 공식 문법 사용 (immediate input handling)
- **리팩토링**: 일관성 있고 통일된 방식으로 진행
- **사다리 시스템**: W키로 사다리 등반/매달리기 상태 시작

## 🌐 **네트워킹 패턴**

### 📡 **Photon Fusion 2 규칙**
- **입력 처리**: NetworkButtons 사용 (immediate input handling)
- **상태 동기화**: [Networked] 프로퍼티 사용
- **일회성 이벤트**: RPC 사용 (아이템 줍기 등)
- **권한 관리**: 
  - StateAuthority: 호스트가 상태 변경 제어
  - InputAuthority: 로컬 플레이어가 입력 제어
  - 스폰: 호스트가 NetworkPrefabRef로 플레이어 스폰

### ✅ **Do (해야 할 것)**
- **상태 동기화**: 계속 변하는 값(체력, 위치)은 `[Networked]` 프로퍼티 사용
- **타이머**: 모든 시간 기반 로직(스킬 쿨타임, 버프 지속시간)은 `TickTimer` 사용
- **권한 체크**: `FixedUpdateNetwork()` 시작 부분에서 `if (Object.HasStateAuthority == false) return;` 권한 확인
- **컴포넌트 참조**: `Spawned()` 생명주기 함수 내에서 `GetComponent` 호출하여 캐싱

### ❌ **Don't (하지 말아야 할 것)**
- **RPC 남용**: `Update()` 또는 `FixedUpdateNetwork()` 안에서 절대 RPC 호출 금지
- **클라이언트 신뢰**: 클라이언트에서 보낸 정보를 서버가 검증 없이 신뢰 금지 (RPC로 요청 → 서버가 검증 후 상태 변경)
- **무거운 함수 사용**: `GameObject.Find()`, `GetComponent` 등을 `FixedUpdateNetwork()` 안에서 절대 사용 금지

## 🔧 **개발 워크플로우**

### 📝 **코드 품질**
- **컴포넌트 참조**: 모든 GetComponent 호출을 Spawned()에서 처리
- **에러 처리**: Spawned()에서 Debug.LogError로 누락된 컴포넌트 검증
- **null 체크**: Spawned() 검증 후 런타임 null 체크 제거
- **테스트**: _TestSandBox에서 실험적 기능 개발

### 🎯 **최근 개선사항**
- **상태 관리**: PlayerState와 PlayerAction 열거형으로 중앙화된 상태 관리
- **상태 전환 시스템**: PlayerStateTransitions 클래스로 안전한 상태 전환 로직 구현
- **성능 최적화**: 컴포넌트 참조를 초기화 시점에 한 번만 설정
- **안정성 향상**: 초기화 시점에 모든 의존성 검증
- **액션 시스템**: 플래그 열거형으로 동시 액션 처리 가능

## 📋 **중요 참고사항**
- **사다리 시스템**: W키로 매달리기/등반 상태 시작
- **카메라 처리**: 플레이어가 자동으로 카메라 설정 처리
- **상태 전환**: PlayerStateTransitions 클래스로 안전한 상태 전환 관리
- **액션 동시성**: 플래그 열거형으로 여러 액션을 동시에 수행 가능
- **파일 크기**: 각 스크립트의 크기 정보 포함하여 복잡도 파악 가능
- **컴포넌트 참조**: Spawned() 메서드에서 모든 컴포넌트 참조 초기화 

# 플레이어 오브젝트 계층구조 및 컴포넌트 배치 규칙

아래 구조를 반드시 지킬 것. (컴포넌트 참조 실수 방지)

| 오브젝트 계층         | 필수 컴포넌트 목록                                  |
|----------------------|---------------------------------------------------|
| Player (루트)        | PlayerInventory, PlayerMovement, PlayerJump,      |
|                      | PlayerClimbing, PlayerGroundCheck, ...            |
| ├── Visual           | SpriteRenderer, Animator, PlayerAnimation         |
| └── Hand             | PlayerItemPickup, PlayerItemUsage, PlayerItemThrower, CircleCollider2D |

- **PlayerInventory는 반드시 Player(루트)에 붙인다.**
- **Hand에는 PlayerItemPickup, PlayerItemUsage, PlayerItemThrower만 붙인다.**
- Hand의 모든 아이템 참조/조작은 GetComponentInParent<PlayerInventory>()로 한다.
- Visual에는 시각적 컴포넌트만 배치한다.

> 이 구조를 어기면 참조 오류, 아이템 사용/던지기 버그가 발생할 수 있음. 