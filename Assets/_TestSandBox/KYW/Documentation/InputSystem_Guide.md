# 🎮 스펠렁키 입력 시스템 가이드

## 📋 목차
1. [개요](#개요)
2. [입력 흐름](#입력-흐름)
3. [주요 컴포넌트](#주요-컴포넌트)
4. [입력 종류](#입력-종류)
5. [네트워크 동기화](#네트워크-동기화)
6. [확장 방법](#확장-방법)

---

## 🎯 개요

스펠렁키 게임은 **Photon Fusion 2**를 사용한 멀티플레이어 게임입니다.  
모든 플레이어의 입력이 네트워크를 통해 동기화되어 일관된 게임 경험을 제공합니다.

### 🎮 기본 컨트롤
- **이동**: A/D 또는 방향키
- **점프**: 스페이스바 (앉지 않았을 때)
- **앉기**: 아래 방향키 + 땅에 있을 때
- **아이템 들기**: 스페이스바 (앉은 상태)
- **아이템 사용**: 마우스 좌클릭
- **아이템 던지기**: 마우스 우클릭
- **아이템 스왑**: 마우스 휠
- **스킬**: 쉬프트키

---

## 🔄 입력 흐름

```
키보드/마우스 → Unity Input → 플레이어 컨트롤러 → 네트워크 → 모든 플레이어
```

### 📝 단계별 설명

#### 1️⃣ **입력 수집** (매 프레임)
```csharp
// SpelunkyPlayerController.BeforeUpdate()
horizontalInput = Input.GetAxisRaw("Horizontal");  // A/D 키
verticalInput = Input.GetAxisRaw("Vertical");      // 위/아래 키
jumpPressed = Input.GetKey(KeyCode.Space);         // 스페이스바
useItemHeld = Input.GetMouseButton(0);             // 마우스 좌클릭
```

#### 2️⃣ **데이터 변환** (네트워크용)
```csharp
// SpelunkyPlayerController.GetNetworkInputData()
data.HorizontalInput = horizontalInput;
data.NetworkButtons.Set(SpelunkyInputButtons.Jump, jumpPressed);
```

#### 3️⃣ **네트워크 전송** (Fusion)
```csharp
// SpelunkyLocalInputPoller.OnInput()
var data = spelunkyPlayer.GetNetworkInputData();
input.Set(data);  // 모든 클라이언트로 전송
```

#### 4️⃣ **입력 처리** (게임 로직)
```csharp
// SpelunkyPlayerController.FixedUpdateNetwork()
movement?.ProcessInput(input);    // 이동 처리
jump?.ProcessInput(input);        // 점프 처리
itemUsage?.ProcessInput(input);   // 아이템 사용 처리
```

---

## 🧩 주요 컴포넌트

### 📁 **SpelunkyPlayerController.cs**
- **역할**: 입력 수집 및 분배의 중심
- **위치**: 플레이어 오브젝트의 메인 컴포넌트
- **주요 메서드**:
  - `BeforeUpdate()`: Unity 입력 수집
  - `GetNetworkInputData()`: 네트워크용 데이터 변환
  - `FixedUpdateNetwork()`: 입력을 각 컴포넌트에 분배

### 📁 **SpelunkyPlayerData.cs**
- **역할**: 네트워크로 전송할 입력 데이터 구조
- **구성**:
  ```csharp
  public struct SpelunkyPlayerData : INetworkInput
  {
      public float HorizontalInput;        // 좌우 이동
      public float VerticalInput;          // 위아래 입력
      public Vector2 MouseWorldPosition;   // 마우스 위치
      public float MouseScrollWheel;       // 휠 스크롤
      public NetworkButtons NetworkButtons; // 버튼 상태들
  }
  ```

### 📁 **SpelunkyLocalInputPoller.cs**
- **역할**: Fusion 네트워크와 입력 시스템 연결
- **주요 기능**: 입력 데이터를 네트워크로 전송

### 📁 **각 기능별 컴포넌트들**
- `PlayerMovement.cs`: 이동 처리
- `PlayerJump.cs`: 점프 처리
- `PlayerItemUsage.cs`: 아이템 사용 처리
- `PlayerItemPickup.cs`: 아이템 들기 처리
- `PlayerItemThrower.cs`: 아이템 던지기 처리

---

## 🎮 입력 종류

### 🏃 **연속 입력** (Analog)
```csharp
public float HorizontalInput;  // -1.0 ~ 1.0 (좌우)
public float VerticalInput;    // -1.0 ~ 1.0 (위아래)
public float MouseScrollWheel; // 휠 스크롤 값
```

### 🔘 **버튼 입력** (Digital)
```csharp
public enum SpelunkyInputButtons
{
    Jump = 1,           // 스페이스바
    PickupItem = 2,     // 스페이스바 (앉은 상태)
    UseItemHold = 3,    // 마우스 좌클릭
    ThrowItem = 4,      // 마우스 우클릭
    Skill = 5,          // 쉬프트키
}
```

### 🖱️ **마우스 위치**
```csharp
public Vector2 MouseWorldPosition; // 월드 좌표 (아이템 던지기 방향용)
```

---

## 🌐 네트워크 동기화

### 🔑 **권한 체계**
- **InputAuthority**: 로컬 플레이어만 입력 수집 가능
- **StateAuthority**: 호스트가 최종 게임 상태 결정

### ⚡ **즉시 입력 vs 상태 동기화**
```csharp
// 즉시 입력 (NetworkButtons)
data.NetworkButtons.Set(SpelunkyInputButtons.Jump, jumpPressed);

// 상태 동기화 ([Networked])
[Networked] public PlayerState CurrentState { get; set; }
```

### 🔄 **입력 처리 순서**
1. **로컬 플레이어**: 입력 수집 → 네트워크 전송
2. **호스트**: 모든 플레이어 입력 수신 → 게임 상태 업데이트
3. **모든 클라이언트**: 업데이트된 상태 수신 → 화면 갱신

---

## 🚀 확장 방법

### ➕ **새로운 입력 추가하기**

#### 1️⃣ **SpelunkyInputButtons에 추가**
```csharp
public enum SpelunkyInputButtons
{
    // 기존 입력들...
    NewAction = 6,  // 새로운 입력
}
```

#### 2️⃣ **SpelunkyPlayerController에 입력 수집 추가**
```csharp
private bool newActionPressed;

public void BeforeUpdate()
{
    if (Object.HasInputAuthority)
    {
        // 기존 입력들...
        newActionPressed = Input.GetKey(KeyCode.E);  // E키 예시
    }
}

public SpelunkyPlayerData GetNetworkInputData()
{
    // 기존 코드...
    data.NetworkButtons.Set(SpelunkyInputButtons.NewAction, newActionPressed);
    return data;
}
```

#### 3️⃣ **새로운 컴포넌트 생성**
```csharp
public class PlayerNewAction : NetworkBehaviour
{
    public void ProcessInput(SpelunkyPlayerData input)
    {
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.NetworkButtons;
        
        if (pressed.IsSet(SpelunkyInputButtons.NewAction))
        {
            // 새로운 액션 실행
        }
    }
}
```

#### 4️⃣ **SpelunkyPlayerController에 연결**
```csharp
private PlayerNewAction newAction;

public override void Spawned()
{
    // 기존 코드...
    newAction = GetComponent<PlayerNewAction>();
}

public override void FixedUpdateNetwork()
{
    if (Runner.TryGetInputForPlayer<SpelunkyPlayerData>(Object.InputAuthority, out var input))
    {
        // 기존 컴포넌트들...
        newAction?.ProcessInput(input);
    }
}
```

---

## 💡 팁과 주의사항

### ✅ **현재 코드의 좋은 점**
- **입력 분리**: 각 컴포넌트가 독립적으로 입력 처리 (`movement?.ProcessInput(input)`)
- **권한 체크**: `Object.HasInputAuthority`로 로컬 플레이어만 입력 수집
- **상태 동기화**: `[Networked]` 프로퍼티로 플레이어 상태 동기화
- **조건부 입력**: 웅크리기 상태에 따라 스페이스바 기능 분기

### ⚠️ **현재 구조에서 주의할 점**
- **Camera.main 의존성**: `Camera.main.ScreenToWorldPoint()` 사용 시 메인 카메라가 없으면 에러
- **컴포넌트 null 체크**: `movement?.IsDucking` 같은 null 체크 필수
- **입력 우선순위**: 스페이스바가 점프와 아이템 들기에 중복 사용됨
- **디버그 코드**: `#if UNITY_EDITOR` 블록의 디버그 코드는 빌드에서 제외됨

### 🐛 **디버깅 팁**
```csharp
// 현재 코드에서 사용 가능한 디버깅 방법
Debug.Log($"[SpelunkyPlayerController] 현재 손에 든 오브젝트: {heldName}");

// 입력 상태 확인 (ProcessInput 내에서)
Debug.Log($"Horizontal: {input.HorizontalInput}");
Debug.Log($"Jump Pressed: {input.NetworkButtons.IsSet(SpelunkyInputButtons.Jump)}");

// 컴포넌트 참조 확인
if (movement == null) Debug.LogError("PlayerMovement 컴포넌트 없음!");
```

---

## 📚 관련 파일들

- `SpelunkyPlayerController.cs`: 메인 입력 컨트롤러
- `SpelunkyPlayerData.cs`: 입력 데이터 구조
- `SpelunkyLocalInputPoller.cs`: 네트워크 입력 전송
- `PlayerMovement.cs`: 이동 입력 처리
- `PlayerJump.cs`: 점프 입력 처리
- `PlayerItemUsage.cs`: 아이템 사용 입력 처리

---

*이 문서는 스펠렁키 게임의 입력 시스템을 이해하고 확장하는 데 도움을 주기 위해 작성되었습니다.* 