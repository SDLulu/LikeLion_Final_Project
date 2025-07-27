# 🎮 스펠렁키 프로젝트 개발 규칙

## 📋 기본 규칙
- **포톤 퓨전(Photon Fusion) 관련 답변은 항상 최신 공식 문서를 토대로 답변해 주세요.**
- **모든 답변은 한국어로 작성해 주세요.**
- **AI 모델명을 괄호 안에 먼저 표시하고, 변명 없이 바로 답변해 주세요.**

## 🎯 코드 작성 규칙
- **프로퍼티 작성 시**: 화살표 함수(`=>`)보다는 `{ get; set; }` 또는 `{ get { return ...; } }` 형식을 사용
- **멀티플레이 로직**: Fusion2 공식 문법 사용, 네트워킹 패턴 가이드 준수
- **상태 동기화**: `[Networked]` 프로퍼티 사용
- **시간 로직**: `TickTimer` 사용
- **권한 확인**: `FixedUpdateNetwork()` 시작부에서 확인
- **컴포넌트 캐싱**: `Spawned()`에서 수행
- **RPC 남용 금지**
- **클라이언트 데이터 무조건 신뢰 금지**
- **FixedUpdateNetwork() 내 무거운 함수 사용 금지**

## 🏗️ 플레이어 프리팹 구조

### 📁 권장 오브젝트 구조:
```
Player (최상위 오브젝트)
├── Core/Control 스크립트들
│   ├── SpelunkyPlayerController
│   ├── PlayerMovement
│   ├── PlayerJump
│   ├── PlayerClimbing
│   ├── PlayerStunInvincibleDie
│   └── PlayerShiftSkill
│
├── Check (자식 오브젝트)
│   ├── PlayerGroundCheck
│   └── 기타 체크 관련 스크립트들
│
├── Visual (자식 오브젝트)
│   ├── SpriteRenderer
│   ├── Animator
│   └── PlayerAnimation
│
├── Hand (자식 오브젝트)
│   ├── PlayerObjectPickup
│   ├── PlayerItemUsage
│   ├── PlayerObjectThrower
│   └── CircleCollider2D (IsTrigger = true)
│
└── Back (Data) (자식 오브젝트)
    ├── PlayerHealth
    ├── PlayerInventory
    ├── PlayerStateManager
    └── 기타 데이터 관련 스크립트들
```

### 🔧 스크립트 배치 규칙:
1. **Core/Control**: 플레이어 최상위 오브젝트에 배치
2. **Check**: 자식 오브젝트 "Check"에 배치
3. **Visual**: 자식 오브젝트 "Visual"에 배치
4. **Data/StateManager**: 자식 오브젝트 "Back(Data)"에 배치

### 🎮 컴포넌트 찾기 방식:
- **부모에서 자식 찾기**: `GetComponentInChildren<T>()`
- **자식에서 부모 찾기**: `GetComponentInParent<T>()`
- **같은 오브젝트**: `GetComponent<T>()`

## 🎯 상태 관리 규칙
- **상태 변경**: `PlayerStateManager`를 통해서만 수행
- **상태 확인**: `stateManager.IsDead`, `stateManager.IsStunned` 등 헬퍼 프로퍼티 사용
- **네트워크 동기화**: `[Networked]` 프로퍼티로 자동 동기화

## 🎮 입력 처리 규칙
- **즉시 입력**: Fusion2 공식 방식 사용
- **네트워크 입력**: `SpelunkyPlayerInputData` 구조체 사용
- **입력 권한**: `Object.HasInputAuthority` 확인

## 📝 주석 작성 규칙
- **간결한 주석**: XML 문서 주석 대신 간단한 설명 사용
- **이모지 활용**: 🎮 🏗️ 🔧 등으로 섹션 구분
- **한국어 주석**: 모든 주석은 한국어로 작성