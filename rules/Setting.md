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
│   ├── PlayerInteractionBase
│   └── PlayerShiftSkill
│
├── GroundCheck (자식 오브젝트) - PlayerGroundCheck 컴포넌트
├── 기타 체크 오브젝트들 - 각각의 체크 컴포넌트들
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

## 🛡️ 무적 및 상호작용 시스템

### 🎯 구현된 기능들
- **스턴**: 일정 시간 동안 기절 상태
- **무적**: 일정 시간 동안 무적 상태 (깜박임 효과 포함)
- **사망**: 사망 상태로 설정
- **넉백**: 방향과 힘으로 넉백 적용
- **랜덤 넉백**: 랜덤 방향으로 넉백 테스트

### 🎭 무적 깜박임 효과
- **설정**: `invincibleBlinkDuration` (초 단위)
- **동작**: 원본 메테리얼 ↔ 무적 메테리얼 교체
- **예시**: 0.5초 설정 시 0.5초씩 깜박임

### 🧪 테스트 도구 (PlayerStateDebugger)
- **1번 키**: 스턴 테스트
- **2번 키**: 무적 테스트
- **3번 키**: 사망 테스트
- **4번 키**: 랜덤 넉백 테스트
- **0번 키**: 상태 리셋
- **R키**: 상태 업데이트 강제 실행

## 🏃 플레이어 움직임 시스템

### 🎯 Velocity 제어 위치
1. **PlayerMovement**: 수평 이동 (사다리 오르는 중 제외)
2. **PlayerClimbing**: 사다리 오르기 (중앙 정렬 포함)
3. **PlayerJump**: 점프 상승 속도
4. **PlayerInteractionBase**: 넉백 힘 적용

### 🌍 중력 제어 위치
1. **PlayerClimbing**: 사다리 중력 (0 = 무중력, 1 = 일반)
2. **PlayerJump**: 수동 중력 적용 (땅에 없을 때)
3. **PMK_DeathTrap**: 함정 중력 (0.1 = 낮은 중력)

## 🚫 넉백 시 속도/중력 제어 방지 방법

### 🎯 문제 상황
넉백을 당할 때 다른 시스템(이동, 점프, 사다리 등)이 velocity를 덮어씌워서 넉백 효과가 사라지는 문제

### 🎮 해결 방안

#### **방법 1: 넉백 우선순위 시스템**
```csharp
// PlayerInteractionBase에 넉백 상태 추가
[Networked] public bool IsKnockbacked { get; private set; }
[Networked] public TickTimer KnockbackTimer { get; private set; }

// 넉백 적용 시
public virtual void ApplyKnockback(Vector3 force, float duration = 0.5f)
{
    if (!HasStateAuthority) return;
    
    IsKnockbacked = true;
    KnockbackTimer = TickTimer.CreateFromSeconds(Runner, duration);
    rb.AddForce(force, ForceMode2D.Impulse);
}

// 다른 시스템에서 velocity 변경 전에 확인
if (!playerInteraction.IsKnockbacked)
{
    // 기존 velocity 변경 로직
}
```

#### **방법 2: 넉백 중력 보호**
```csharp
// PlayerJump.cs의 ApplyGravity() 수정
private void ApplyGravity()
{
    // 넉백 중에는 중력 적용 안함
    if (playerInteraction?.IsKnockbacked == true) return;
    
    if (!groundCheck.IsGrounded)
    {
        rb.linearVelocity += Vector2.down * gravity * Runner.DeltaTime;
    }
}
```

#### **방법 3: 넉백 상태 기반 이동 제한**
```csharp
// PlayerMovement.cs의 ProcessMovement() 수정
private void ProcessMovement(SpelunkyPlayerInputData input)
{
    // 넉백 중에는 플레이어 입력 무시
    if (playerInteraction?.IsKnockbacked == true) return;
    
    // 기존 이동 로직
}
```

#### **방법 4: 넉백 타이머 기반 해제**
```csharp
// PlayerInteractionBase.cs의 FixedUpdateNetwork() 추가
public override void FixedUpdateNetwork()
{
    // 넉백 타이머 만료 시 해제
    if (IsKnockbacked && KnockbackTimer.Expired(Runner))
    {
        IsKnockbacked = false;
    }
}
```

### 🎮 권장 구현 순서
1. **넉백 상태 추가**: `IsKnockbacked` 프로퍼티
2. **넉백 타이머 추가**: `KnockbackTimer` 프로퍼티
3. **이동 시스템 수정**: 넉백 중 입력 무시
4. **중력 시스템 수정**: 넉백 중 중력 적용 안함
5. **사다리 시스템 수정**: 넉백 중 사다리 사용 불가

### 🔍 디버깅 도구
```csharp
// PlayerStateDebugger에 추가
debugText += $"넉백 상태: {playerInteraction?.IsKnockbacked}\n";
debugText += $"넉백 타이머: {playerInteraction?.KnockbackTimer.RemainingTime(Runner):F2}초\n";
```

이렇게 구현하면 넉백을 당할 때 다른 시스템이 간섭하지 않아 자연스러운 넉백 효과를 볼 수 있습니다.