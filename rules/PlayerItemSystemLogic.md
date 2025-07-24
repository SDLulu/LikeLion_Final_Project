# 플레이어 아이템 시스템 로직 규칙

## 📋 시스템 개요
플레이어가 아이템을 줍고, 사용하고, 던지고, 스왑하는 전체적인 로직을 정의합니다.

## 🎯 핵심 컴포넌트
- **PlayerItemPickup**: 아이템 감지 및 줍기
- **PlayerInventory**: 인벤토리 데이터 관리
- **PlayerItemUsage**: 아이템 사용 및 스왑
- **PlayerItemThrower**: 아이템 던지기

## 🔄 아이템 줍기 (Pickup) 로직

### 1. 트리거 감지
```
Player의 CircleCollider2D가 아이템과 닿음
↓
OnTriggerEnter2D() 호출
↓
nearbyItems HashSet에 아이템 추가
```

### 2. 줍기 입력 처리
```
Space키 + 웅크리기 상태 + 손에 든 것 없음
↓
FindNearestItem()로 가장 가까운 아이템 찾기
↓
PickupObjectRpc() 호출 (InputAuthority → All)
```

### 3. 실제 줍기 처리 (StateAuthority에서만)
```
1. inventory.HoldObject(obj) 호출
   - InputAuthority 할당
   - currentHeldObject 설정
   
2. Transform 부모 할당
   - obj.transform.SetParent(handTransform)
   - obj.transform.localPosition = Vector3.zero
   - obj.transform.localRotation = Quaternion.identity
   
3. 물리 비활성화
   - Rigidbody2D.simulated = false
   - Collider2D.isTrigger = true
   
4. 주변 목록에서 제거
   - nearbyItems.Remove(obj)
```

## 🎮 아이템 사용 (Usage) 로직

### 1. 입력 처리
```
좌클릭 Hold 입력
↓
HandleUsage() 호출
↓
IUsableItem.OnUsePress/Hold/Release() 호출
```

### 2. 회전 처리
```
마우스 방향 계산
↓
NetworkedRotationAngle 설정
↓
FixedUpdateNetwork()에서 회전 적용
↓
Render()에서 스프라이트 뒤집기
```

## 🔄 아이템 스왑 (Swap) 로직

### 1. 휠 스크롤 감지
```
마우스 휠 스크롤 입력
↓
HandleWheelScroll() 호출
↓
SwapToSlot(direction) 호출
```

### 2. 스왑 처리
```
1. 현재 아이템을 저장소로 이동
   - currentItem.transform.SetParent(storageTransform)
   - currentItem.transform.position = new Vector3(1000f, 1000f, 0f)
   - InputAuthority 해제 (RemoveInputAuthority)
   
2. 인벤토리 데이터 스왑
   - inventory.SwapHeldItemWithSlot(targetSlot)
   - currentHeldObject = slotObj
   - inventorySlots.Set(slotIndex, null)
   
3. 새 아이템을 손으로 이동
   - slotObj.transform.SetParent(handTransform)
   - slotObj.transform.localPosition = Vector3.zero
   - InputAuthority 할당 (AssignInputAuthority)
```

## 🚀 아이템 던지기 (Throw) 로직

### 1. 입력 처리
```
우클릭 + 손에 든 오브젝트 존재
↓
ThrowObjectRpc() 호출 (InputAuthority → All)
```

### 2. 던지기 처리
```
1. 인벤토리에서 제거
   - inventory.DropHeldObject()
   - currentHeldObject = null
   
2. Transform 부모 해제
   - obj.transform.SetParent(null)
   
3. 물리 복구 (StateAuthority에서만)
   - Rigidbody2D.simulated = true
   - Rigidbody2D.isKinematic = false
   - Rigidbody2D.linearVelocity = direction * throwForce
   - Collider2D.isTrigger = false
   
4. InputAuthority 해제
   - netObj.RemoveInputAuthority()
```

## 📦 인벤토리 데이터 관리

### 1. 데이터 구조
```csharp
[Networked] private NetworkObject currentHeldObject;  // 손에 든 오브젝트
[Networked, Capacity(8)] private NetworkArray<NetworkObject> inventorySlots;  // 슬롯 데이터
```

### 2. 데이터와 시각적 표현 분리
- **인벤토리**: NetworkObject 참조 저장 (네트워크 동기화)
- **Transform**: 실제 오브젝트 위치 관리 (시각적 표현)

## 🔐 InputAuthority 관리 규칙

### 1. InputAuthority 할당
- 아이템 줍기 시: `AssignInputAuthority(Object.InputAuthority)`
- 아이템 스왑 시 (새 아이템): `AssignInputAuthority(Object.InputAuthority)`

### 2. InputAuthority 해제
- 아이템 던지기 시: `RemoveInputAuthority()`
- 아이템 스왑 시 (기존 아이템): `RemoveInputAuthority()`

## 🎯 상태 관리 규칙

### 1. 네트워크 동기화
- 모든 상태 변경은 StateAuthority에서만 실행
- RPC를 통한 안전한 상태 전파
- [Networked] 속성으로 자동 동기화

### 2. 물리 시뮬레이션
- 손에 들고 있을 때: `simulated = false, isKinematic = true`
- 던졌을 때: `simulated = true, isKinematic = false`

### 3. 충돌 처리
- 손에 들고 있을 때: `isTrigger = true`
- 던졌을 때: `isTrigger = false`

## ⚠️ 주의사항

### 1. 권한 체크
- 모든 상태 변경 전 `Object.HasStateAuthority` 체크
- InputAuthority 관련 작업 전 권한 확인

### 2. null 체크
- 모든 컴포넌트 참조 전 null 체크
- NetworkObject 유효성 검증

### 3. 네트워크 동기화
- RPC 호출 시 적절한 소스/타겟 설정
- 상태 변경의 원자성 보장

## 🔧 확장 가능한 구조

### 1. 아이템 타입별 처리
- 레이어 기반 아이템 구분
- IUsableItem 인터페이스 활용
- 컴포넌트 기반 확장

### 2. 슬롯 시스템
- 동적 슬롯 개수 조정 가능
- 슬롯별 특수 기능 추가 가능
- 패시브 아이템 시스템 분리

### 3. 네트워크 최적화
- 불필요한 RPC 호출 방지
- 상태 변경 최소화
- 효율적인 동기화 전략 