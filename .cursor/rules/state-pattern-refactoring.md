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
- [~] `PlayerItemPickup.cs` 상태 패턴 적용 (즉발 액션, 리팩토링 불필요)
- [ ] `PlayerItemUsage.cs` 상태 패턴 적용
- [~] `PlayerItemThrower.cs` 상태 패턴 적용 (즉발 액션, 리팩토링 불필요)
- [ ] `PlayerShiftSkill.cs` 상태 패턴 적용
- [보류] PlayerAnimation.cs 상태 패턴 기반 리팩토링 (우선순위: 인벤토리/체력 시스템 추가가 더 시급)

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

---

## 🎒 인벤토리/오브젝트 핸들링 상태 패턴 적용 의도 및 구조

### 📦 리팩토링 의도

- **상태 패턴과 인벤토리/오브젝트 핸들링의 결합**
  - 플레이어가 "손에 들고 있는 것"과 "저장 슬롯"을 명확히 분리하여,  
    상태별로 아이템/캐릭터/빈손을 일관성 있게 관리
  - 상태(예: Stunned, Ducking 등)에 따라 아이템 줍기/사용/던지기 등 액션의 허용 여부를 중앙에서 제어

---

### 🏗️ 구조 및 설계

- **PlayerInventory**
  - `currentHeldObject`: 손에 들고 있는 오브젝트(아이템/캐릭터, 1개만)
  - `inventorySlots`: 저장 슬롯(아이템만, 기본 1칸, 패시브 등으로 확장 가능)
  - `maxInventorySlots`: 현재 슬롯 개수(동적 확장)
  - 오브젝트 타입은 레이어(PlayerNpc, Enemy, Item)로 구분

- **핵심 프로퍼티/메서드**
  - `CurrentHeldObject`, `CurrentHeldItem`, `CurrentHeldCharacter`
  - `StoreItemToSlot`, `HoldItemFromSlot`, `HoldCharacter`, `DropHeldObject`
  - `AddInventorySlot`, `RemoveInventorySlot`

---

### 🔄 3개 스크립트 리팩토링 방향

- **PlayerItemPickup**
  - 기존 CurrentItem/HasItem 등 직접 관리 → PlayerInventory의 CurrentHeldObject/StoreItemToSlot/HoldCharacter 등으로 일원화
  - 상태(예: Ducking)와 레이어에 따라 아이템/캐릭터를 들거나 저장

- **PlayerItemThrower**
  - 던지기 대상도 PlayerInventory의 CurrentHeldObject로 통일
  - 상태에 따라 던지기 가능 여부 제어

- **PlayerItemUsage**
  - 아이템/캐릭터/빈손(기본 펀치) 사용 로직을 PlayerInventory의 CurrentHeldObject 기준으로 분기
  - 상태별로 사용 가능 여부를 중앙에서 제어

---

### 🎯 기대 효과

- **상태 패턴과 인벤토리/오브젝트 핸들링의 결합으로**  
  코드 일관성, 확장성, 버그 방지, 유지보수성 대폭 향상
  - 상태별 액션 제한이 명확해지고, 새로운 상태/아이템/캐릭터 추가가 쉬워짐 

---

## 🎒 플레이어 인벤토리 및 오브젝트 줍기/던지기/사용 명세

### 1. 인벤토리 시스템
- 플레이어는 '손에 든 오브젝트'와 '인벤토리 슬롯(아이템만 저장 가능)'을 별도로 관리한다.
- 손에 들 수 있는 대상: 아이템, 스턴된 적/NPC, 죽은 적/NPC, 죽은 플레이어, (특정 상황에서) 플레이어 등
- 인벤토리 슬롯에는 아이템만 저장할 수 있다. (캐릭터/적/NPC/플레이어 등은 슬롯에 저장 불가)

### 2. 오브젝트 줍기(Pickup)
- Space+웅크리기 입력 시, 감지 범위 내에서 '들 수 있는 오브젝트'를 하나 선택해 무조건 손에 든다.
- 손에 든 것이 있을 때는 줍기 불가.
- 들 수 있는 오브젝트 판정은 레이어+상태(스턴, 죽음 등)로 결정한다.
- 줍는 순간에는 아이템/캐릭터 구분 없이 HoldObject(obj)로 처리
- 손에 든 오브젝트는 항상 currentHeldObject로 관리
- 줍는 대상이 아이템이면, 인벤토리 슬롯에 저장하지 않고 일단 손에 든다

### 3. 오브젝트 던지기(Throw)
- 손에 든 오브젝트가 있을 때, 우클릭 등 지정 입력 시 마우스 방향으로 던진다.
- 아이템/캐릭터/적/NPC/플레이어 등 들 수 있는 모든 오브젝트를 던질 수 있다.
- 던진 후에는 손에 든 오브젝트가 비워진다.
- 던질 때 물리/충돌/네트워크 권한 등 복구 필요
- 던질 수 없는 오브젝트(특수 상황)는 예외 처리

### 4. 오브젝트 사용(Use)
- 손에 든 오브젝트가 아이템일 때만 사용(Use) 가능
- 손에 든 것이 캐릭터/적/NPC/플레이어 등일 때는 사용 불가
- 사용 입력(좌클릭 등) 시, IUsableItem 인터페이스를 통해 동작
- GetCurrentUsableItem()에서 손에 든 것이 아이템일 때만 반환
- 캐릭터류는 사용 입력 무시

### 5. 슬롯(휠) 전환 및 아이템 저장/불러오기
- 마우스 휠 등으로 슬롯을 전환할 수 있다.
- 손에 든 것이 아이템일 때만 현재 선택 슬롯에 저장 가능
- 슬롯에 아이템이 있을 때만 꺼내서 손에 들 수 있다
- 손에 든 것이 캐릭터/적/NPC/플레이어 등일 때는 슬롯 전환/저장/불러오기 모두 불가
- StoreItemToSlot, HoldItemFromSlot 등은 아이템만 처리
- 캐릭터류는 슬롯 관련 함수에서 무시

### 6. 상태 변화(스턴 해제 등)와 오브젝트 분리
- 들고 있는 대상이 스턴/죽음 상태에서 회복되면, 자동으로 손에서 분리(부모 해제, currentHeldObject 해제, 물리/권한 복구 등)
- 스턴/죽음 상태를 관리하는 컴포넌트에서 상태 변화 시 직접 분리 로직 호출

### 7. 네트워크 권한 관리
- 오브젝트를 들 때 InputAuthority를 플레이어에게 할당
- 내려놓거나 던질 때 InputAuthority를 해제 