# 🎮 상태 패턴 리팩토링 작업 요약

## 📋 **작업 개요**
기존의 복잡한 조건문 기반 플레이어 컨트롤을 상태 패턴으로 리팩토링하여 확장성과 유지보수성을 향상시킴.

## 🎯 **목표**
- 각 컴포넌트에서 복잡한 조건문을 상태 기반 조건문으로 단순화
- 상태별 행동 제한을 중앙화하여 관리
- 새로운 상태 추가 시 확장 용이성 확보

## 📁 **수정된 파일들**

### **1. PlayerState.cs** ✅ 완료
- **추가**: `PlayerStateHelper` 클래스 - 상태별 행동 가능 여부 정의
- **정리**: 과도한 추상화 제거 (인터페이스, 개별 상태 클래스들)
- **단순화**: `PlayerAction`에서 순간적 액션 제거 (ThrowingItem, PickingUpItem)
- **구조**:
  ```csharp
  public static bool CanMove(PlayerState state) { /* switch문으로 상태별 반환 */ }
  public static bool CanJump(PlayerState state) { /* switch문으로 상태별 반환 */ }
  public static bool CanClimb(PlayerState state) { /* switch문으로 상태별 반환 */ }
  // ... 기타 행동들
  ```

### **2. SpelunkyPlayerController.cs** ✅ 완료
- **추가**: 상태 관리 필드들
  ```csharp
  [Networked] public PlayerState CurrentState { get; private set; }
  [Networked] public PlayerAction CurrentActions { get; private set; }
  ```
- **추가**: 상태 감지 메서드들
  - `UpdateCurrentState()`: 기존 컴포넌트들의 상태를 읽어서 현재 상태 결정
  - `UpdateCurrentActions()`: 현재 액션들 업데이트
  - `DetermineCurrentState()`: 우선순위에 따른 상태 결정 로직
- **정리**: 불필요한 시각적 컴포넌트 참조들 제거
- **정리**: 자동 생성 로직 단순화 (수동 설정으로 변경)

### **3. PlayerJump.cs** ✅ 완료
- **추가**: `SpelunkyPlayerController` 참조
- **수정**: 상태 기반 점프 제한 적용
  ```csharp
  // 점프 시작 시에만 상태 체크 (점프 지속을 위해)
  if (pressed.IsSet(SpelunkyInputButtons.Jump) && groundCheck.IsGrounded)
  {
      if (!PlayerStateHelper.CanJump(playerController.CurrentState)) return;
      // 점프 로직
  }
  ```
- **해결**: 점프 중에도 점프키를 누르면 지속되는 문제 해결

### **4. PlayerMovement.cs** ✅ 완료
- **추가**: `SpelunkyPlayerController` 참조
- **수정**: 상태 기반 이동/웅크리기 제한 적용
  ```csharp
  // 이동 제한
  if (!PlayerStateHelper.CanMove(playerController.CurrentState)) return;
  
  // 웅크리기 제한
  if (!PlayerStateHelper.CanDuck(playerController.CurrentState)) 
  {
      IsDucking = false;
      return;
  }
  ```

### **5. PlayerClimbing.cs** ✅ 완료
- **추가**: `SpelunkyPlayerController` 참조
- **수정**: 상태 기반 사다리 오르기 제한 적용
  ```csharp
  // 사다리 오르기 제한 (순환 의존성 방지)
  if (playerController.CurrentState != PlayerState.Climbing && !PlayerStateHelper.CanClimb(playerController.CurrentState))
  {
      if (IsClimbing) StopClimbing();
      return;
  }
  ```
- **정리**: 불필요한 `movement` 참조 제거
- **통합**: `PlayerJump`에서 사다리 중 중력 처리 통합

## 🔧 **핵심 변경사항**

### **기존 방식 → 상태 패턴 방식**
```csharp
// 기존: 복잡한 조건문
if (movement.IsDucking) return;
if (climbing.IsClimbing) return;

// 새로운: 간단한 상태 체크
if (!PlayerStateHelper.CanJump(playerController.CurrentState)) return;
if (!PlayerStateHelper.CanMove(playerController.CurrentState)) return;
```

### **상태 우선순위**
1. **Climbing** (최우선)
2. **Jumping** 
3. **Grounded** (Ducking > Walking > Idle)
4. **Falling** (최후순위)

## 🎮 **상태별 행동 정의**

| 상태 | 이동 | 점프 | 사다리 | 아이템사용 | 아이템던지기 | 아이템줍기 | 웅크리기 |
|------|------|------|--------|------------|-------------|------------|----------|
| Idle | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |
| Walking | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |
| Ducking | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| Jumping | ✅ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Falling | ✅ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Climbing | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Stunned | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Dead | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

## 🚀 **다음 단계**
- [x] `PlayerMovement.cs` 순환 의존성 버그 수정 ✅
- [x] `PlayerClimbing.cs` 상태 패턴 적용 ✅
- [ ] `PlayerItemPickup.cs` 상태 패턴 적용  
- [ ] `PlayerItemUsage.cs` 상태 패턴 적용
- [ ] `PlayerItemThrower.cs` 상태 패턴 적용
- [ ] `PlayerShiftSkill.cs` 상태 패턴 적용

## 💡 **주요 학습사항**
1. **상태 패턴은 기존 파라미터를 대체하지 않고 보완**하는 역할
2. **점프 지속을 위해서는 점프 시작 시에만 상태 체크**해야 함
3. **기존 컴포넌트들의 상태를 읽어서 중앙화된 상태 관리**가 핵심
4. **과도한 추상화보다는 실용적인 단순화**가 더 효과적
5. **순환 의존성 방지**: 상태 결정과 컴포넌트 상태 설정 사이의 무한 루프 주의
6. **상태 안정성**: 현재 상태를 고려한 조건부 상태 전환으로 깜빡임 방지

## 🔍 **테스트 완료 사항**
- ✅ 점프 정상 동작 (한 번 점프 후 지속 가능)
- ✅ 상태별 이동 제한 동작
- ✅ 상태별 웅크리기 제한 동작
- ✅ 상태별 사다리 오르기 제한 동작
- ✅ 네트워크 동기화 정상 동작
- ✅ 웅크리기 애니메이션 깜빡임 버그 수정
- ✅ 사다리 중 중력 처리 통합 완료 