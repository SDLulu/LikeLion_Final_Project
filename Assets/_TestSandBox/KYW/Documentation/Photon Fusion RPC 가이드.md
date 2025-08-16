# Photon Fusion RPC 가이드

## 📖 개요

이 문서는 Photon Fusion에서 RPC(Remote Procedure Call)를 사용하는 방법과 다양한 타겟 설정에 대한 이해를 돕습니다. RPC는 네트워크 상에서 다른 클라이언트에게 함수 실행을 요청하는 핵심 메커니즘입니다.

## 🎯 RPC란?

### RPC의 정의
- **Remote Procedure Call**: 원격 프로시저 호출
- 네트워크 상의 다른 클라이언트에게 함수 실행을 요청
- Fusion 네트워크 시스템을 통한 안전한 함수 호출

### RPC의 장점
- **자동 동기화**: FixedUpdateNetwork와 자동으로 동기화
- **권한 분리**: InputAuthority와 StateAuthority 역할 분리
- **네트워크 최적화**: Fusion이 자동으로 네트워크 최적화
- **코드 간소화**: 복잡한 네트워크 동기화 로직 불필요

## 🔧 RPC 기본 구조

### RPC 함수 선언
```csharp
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_RequestDie()
{
    // RPC 로직
}
```

### RPC 어트리뷰트 구성
```csharp
[Rpc(
    RpcSources = RpcSources.InputAuthority,    // 누가 호출할 수 있는가?
    RpcTargets = RpcTargets.StateAuthority,    // 누구에게 전송되는가?
    InvokeResim = true,                        // 리심레이션 시 재실행 여부
    InvokeLocal = false                        // 로컬에서도 실행할지 여부
)]
```

## 🎮 RpcSources (호출자 권한)

### RpcSources.InputAuthority
```csharp
// 입력 권한을 가진 클라이언트만 호출 가능
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_PlayerInput()
{
    // 플레이어 입력 관련 RPC
    // 예: 점프, 공격, 이동 등
}

// 사용 예시
void Update()
{
    if (Input.GetKeyDown(KeyCode.Space))
    {
        RPC_PlayerInput(); // InputAuthority만 호출 가능
    }
}
```

### RpcSources.StateAuthority
```csharp
// StateAuthority만 호출 가능
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
public void RPC_GameStateChange()
{
    // 게임 상태 변경 RPC
    // 예: 게임 시작, 종료, 스테이지 전환 등
}

// 사용 예시
public void StartGame()
{
    if (HasStateAuthority)
    {
        RPC_GameStateChange(); // StateAuthority만 호출 가능
    }
}
```

### RpcSources.All
```csharp
// 모든 클라이언트가 호출 가능
[Rpc(RpcSources.All, RpcTargets.All)]
public void RPC_ChatMessage(string message)
{
    // 채팅 메시지 RPC
    // 예: 플레이어 간 대화, 시스템 메시지 등
}

// 사용 예시
public void SendChat(string message)
{
    RPC_ChatMessage(message); // 누구나 호출 가능
}
```

### RpcSources.Server
```csharp
// 서버만 호출 가능
[Rpc(RpcSources.Server, RpcTargets.All)]
public void RPC_ServerCommand()
{
    // 서버 전용 명령 RPC
    // 예: 관리자 명령, 서버 설정 변경 등
}
```

## 🎯 RpcTargets (수신자)

### RpcTargets.All
```csharp
// 모든 클라이언트에게 전송
[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
public void RPC_PlayerAction(string actionName)
{
    // 모든 클라이언트에서 실행
    // 예: 플레이어 애니메이션, 이펙트 등
}

// 사용 예시
public void PlayAnimation(string animName)
{
    RPC_PlayerAction(animName); // 모든 클라이언트에서 애니메이션 재생
}
```

### RpcTargets.Others
```csharp
// 다른 클라이언트에게만 전송 (자신 제외)
[Rpc(RpcSources.InputAuthority, RpcTargets.Others)]
public void RPC_PlayerMovement(Vector3 position)
{
    // 다른 클라이언트에서만 실행
    // 예: 다른 플레이어 위치 동기화
}

// 사용 예시
public void UpdatePosition(Vector3 newPos)
{
    RPC_PlayerMovement(newPos); // 다른 클라이언트에게만 위치 전송
}
```

### RpcTargets.StateAuthority
```csharp
// StateAuthority에게만 전송
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_RequestAction()
{
    // StateAuthority에서만 실행
    // 예: 권한이 필요한 작업 요청
}

// 사용 예시
public void RequestSpawnItem()
{
    RPC_RequestAction(); // StateAuthority에게 아이템 스폰 요청
}
```

### RpcTargets.InputAuthority
```csharp
// InputAuthority에게만 전송
[Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
public void RPC_ResponseToInput()
{
    // InputAuthority에서만 실행
    // 예: 입력에 대한 응답, 결과 전송
}

// 사용 예시
public void SendActionResult(bool success)
{
    RPC_ResponseToInput(); // InputAuthority에게 결과 전송
}
```

## 🎮 실제 게임 예시

### 1. 플레이어 공격 시스템
```csharp
public class PlayerCombat : NetworkBehaviour
{
    // 공격 요청 (InputAuthority → StateAuthority)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestAttack()
    {
        if (HasStateAuthority)
        {
            // 공격 로직 실행
            ExecuteAttack();
            
            // 모든 클라이언트에게 공격 결과 전송
            RPC_AttackResult();
        }
    }
    
    // 공격 결과 전송 (StateAuthority → All)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_AttackResult()
    {
        // 모든 클라이언트에서 공격 이펙트 재생
        PlayAttackEffect();
    }
    
    // 사용 예시
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 좌클릭
        {
            RPC_RequestAttack();
        }
    }
}
```

### 2. 아이템 획득 시스템
```csharp
public class ItemPickup : NetworkBehaviour
{
    // 아이템 획득 요청 (InputAuthority → StateAuthority)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestPickup(int itemId)
    {
        if (HasStateAuthority)
        {
            // 아이템 획득 로직 실행
            bool success = TryPickupItem(itemId);
            
            // 결과를 요청한 클라이언트에게 전송
            RPC_PickupResult(success, itemId);
        }
    }
    
    // 아이템 획득 결과 (StateAuthority → InputAuthority)
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_PickupResult(bool success, int itemId)
    {
        if (success)
        {
            // 성공 시 UI 업데이트
            UpdateInventoryUI(itemId);
            PlayPickupSound();
        }
        else
        {
            // 실패 시 메시지 표시
            ShowPickupFailedMessage();
        }
    }
    
    // 사용 예시
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Item"))
        {
            int itemId = other.GetComponent<Item>().itemId;
            RPC_RequestPickup(itemId);
        }
    }
}
```

### 3. 게임 상태 관리
```csharp
public class GameManager : NetworkBehaviour
{
    // 게임 시작 (StateAuthority → All)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartGame()
    {
        // 모든 클라이언트에서 게임 시작 처리
        StartGameUI();
        StartGameMusic();
        EnablePlayerControls();
    }
    
    // 게임 종료 (StateAuthority → All)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_EndGame(bool victory)
    {
        // 모든 클라이언트에서 게임 종료 처리
        ShowGameResult(victory);
        DisablePlayerControls();
        ShowMainMenu();
    }
    
    // 사용 예시
    public void StartGame()
    {
        if (HasStateAuthority)
        {
            RPC_StartGame();
        }
    }
    
    public void EndGame(bool victory)
    {
        if (HasStateAuthority)
        {
            RPC_EndGame(victory);
        }
    }
}
```

### 4. 플레이어 상호작용
```csharp
public class PlayerInteraction : NetworkBehaviour
{
    // 상호작용 요청 (InputAuthority → StateAuthority)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestInteraction(int targetId)
    {
        if (HasStateAuthority)
        {
            // 상호작용 로직 실행
            bool success = ProcessInteraction(targetId);
            
            // 결과를 요청한 클라이언트에게 전송
            RPC_InteractionResult(success, targetId);
        }
    }
    
    // 상호작용 결과 (StateAuthority → InputAuthority)
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_InteractionResult(bool success, int targetId)
    {
        if (success)
        {
            // 성공 시 이펙트 재생
            PlayInteractionEffect();
        }
        else
        {
            // 실패 시 메시지 표시
            ShowInteractionFailedMessage();
        }
    }
    
    // 상호작용 이펙트 (StateAuthority → Others)
    [Rpc(RpcSources.StateAuthority, RpcTargets.Others)]
    public void RPC_InteractionEffect(int targetId)
    {
        // 다른 클라이언트에서 이펙트 재생
        PlayInteractionEffect();
    }
    
    // 사용 예시
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            // 주변 상호작용 가능한 오브젝트 감지
            var interactable = GetNearestInteractable();
            if (interactable != null)
            {
                RPC_RequestInteraction(interactable.id);
            }
        }
    }
}
```

## 🔄 RPC 호출 패턴

### 1. 단방향 패턴
```csharp
// 단순히 정보만 전송
[Rpc(RpcSources.InputAuthority, RpcTargets.All)]
public void RPC_UpdatePosition(Vector3 position)
{
    transform.position = position;
}
```

### 2. 요청-응답 패턴
```csharp
// 요청
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_RequestAction() { }

// 응답
[Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
public void RPC_ActionResult(bool success) { }
```

### 3. 브로드캐스트 패턴
```csharp
// 모든 클라이언트에게 알림
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
public void RPC_BroadcastEvent(string eventName) { }
```

### 4. 체인 패턴
```csharp
// RPC 내에서 다른 RPC 호출
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_RequestAction()
{
    if (HasStateAuthority)
    {
        // 로직 실행 후 결과 전송
        RPC_ActionResult(true);
        
        // 다른 클라이언트에게 알림
        RPC_NotifyOthers();
    }
}
```

## ⚠️ 주의사항

### 1. RPC 호출 제한
```csharp
// ❌ 잘못된 사용: Update에서 매번 호출
void Update()
{
    if (Input.GetKey(KeyCode.W))
    {
        RPC_UpdatePosition(transform.position); // 매 프레임 호출!
    }
}

// ✅ 올바른 사용: 상태 변경 시에만 호출
private Vector3 lastPosition;

void Update()
{
    if (Input.GetKey(KeyCode.W))
    {
        Vector3 newPosition = transform.position + Vector3.up * Time.deltaTime;
        if (Vector3.Distance(lastPosition, newPosition) > 0.1f)
        {
            RPC_UpdatePosition(newPosition);
            lastPosition = newPosition;
        }
    }
}
```

### 2. 권한 확인
```csharp
// ❌ 잘못된 사용: 권한 확인 없음
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_RequestAction()
{
    // 권한 확인 없이 실행
    ExecuteAction();
}

// ✅ 올바른 사용: 권한 확인 포함
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_RequestAction()
{
    if (HasStateAuthority)
    {
        ExecuteAction();
    }
}
```

### 3. 네트워크 상태 확인
```csharp
// ❌ 잘못된 사용: 네트워크 상태 확인 없음
public void CallRPC()
{
    RPC_SomeFunction();
}

// ✅ 올바른 사용: 네트워크 상태 확인 포함
public void CallRPC()
{
    if (Object.IsValid && Runner.IsRunning)
    {
        RPC_SomeFunction();
    }
}
```

## 🔍 RPC 디버깅

### 1. RPC 호출 로그
```csharp
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_RequestAction()
{
    Debug.Log($"[RPC] RequestAction 호출됨 - 호출자: {Object.InputAuthority}");
    
    if (HasStateAuthority)
    {
        Debug.Log("[RPC] StateAuthority에서 실행");
        ExecuteAction();
    }
    else
    {
        Debug.LogWarning("[RPC] StateAuthority가 아님");
    }
}
```

### 2. RPC 성공/실패 확인
```csharp
public void CallRPC()
{
    try
    {
        RPC_RequestAction();
        Debug.Log("[RPC] 호출 성공");
    }
    catch (System.Exception e)
    {
        Debug.LogError($"[RPC] 호출 실패: {e.Message}");
    }
}
```

### 3. RPC 실행 순서 확인
```csharp
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_RequestAction()
{
    Debug.Log($"[RPC] RequestAction 실행 - 틱: {Runner.Tick}");
    
    if (HasStateAuthority)
    {
        ExecuteAction();
    }
}
```

## 📚 RPC 모범 사례

### 1. 명확한 네이밍
```csharp
// ✅ 좋은 예시
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_RequestPlayerJump() { }

[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
public void RPC_PlayerJumped(PlayerRef playerRef) { }

// ❌ 나쁜 예시
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_DoSomething() { }
```

### 2. 적절한 권한 설정
```csharp
// 플레이어 입력 → 서버 처리
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]

// 서버 결과 → 모든 클라이언트
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]

// 서버 결과 → 특정 클라이언트
[Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
```

### 3. 효율적인 데이터 전송
```csharp
// ✅ 좋은 예시: 필요한 데이터만 전송
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_UpdatePlayerState(bool isMoving, Vector3 position) { }

// ❌ 나쁜 예시: 불필요한 데이터 포함
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_UpdatePlayerState(bool isMoving, Vector3 position, 
    string playerName, int health, float stamina, bool hasWeapon) { }
```

## 🎯 요약

### RPC 사용 시 고려사항
1. **권한 설정**: RpcSources와 RpcTargets를 적절히 설정
2. **네트워크 부하**: 불필요한 RPC 호출 최소화
3. **동기화 순서**: RPC 실행 순서가 중요한 경우 주의
4. **에러 처리**: RPC 호출 실패에 대한 처리 포함
5. **디버깅**: RPC 실행 과정을 추적할 수 있는 로그 포함

### 권장 패턴
- **플레이어 입력**: InputAuthority → StateAuthority
- **게임 상태 변경**: StateAuthority → All
- **개별 응답**: StateAuthority → InputAuthority
- **브로드캐스트**: StateAuthority → All 또는 Others

---

**작성일**: 2024년
**버전**: 1.0
**작성자**: AI Assistant
**프로젝트**: LikeLion Final Project - KYW
