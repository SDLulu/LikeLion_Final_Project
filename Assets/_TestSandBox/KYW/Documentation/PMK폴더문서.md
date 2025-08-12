## PMK 폴더 문서 (맵/타일/트랩/문 시스템 개요)

이 문서는 `Assets/_TestSandBox/PMK/` 폴더의 구성 요소가 어떤 기능을 하고, 어떤 식으로 제어/연동하는지 한눈에 이해할 수 있도록 정리한 가이드입니다. 멀티플레이 로직은 Photon Fusion 2 호스트-클라이언트(Host-Client) 기준으로 작성되었습니다.

### 디렉터리 구성
- `Scripts/`
  - `Item/`
    - `PMK_TileDestroyItem.cs`: 지연 후 범위 내 타일/오브젝트 파괴 아이템
  - `Object/`
    - `PMK_ArrowTrap.cs`: 감지 후 화살 발사 트랩(스폰은 RPC 경유)
    - `PMK_ArrowTrapObj.cs`: 네트워크 화살 오브젝트(IItemInteraction)
    - `PMK_DeathTrap.cs`: 즉사/중력 조작 트랩(플레이어 중력 스케일 변경)
    - `PMK_NextStageDoor.cs`: 다음 스테이지 문, 모든 플레이어 도달 체크
    - `PMK_TileItem.cs`: 타일 위 아이템 파괴 시 실제 아이템 생성
    - `PMK_TileTrap.cs`: 낙하 시 공격 판정이 켜지는 타일 트랩 오브젝트
  - `Player/`
    - `PlayerClearStageHandler.cs`: 문 트리거 내 W 입력 → 서버에 클리어 요청
  - `Tile/`
    - `PMK_TileDestroyer.cs`: 자리에 타일 생성/혹은 기존 타일·타일아이템 파괴
    - `PMK_TileRogic.cs`: 맵 로딩/배치/타일아이템 생성의 중심 로직(네트워크 동기화)
    - `PMK_TileRPC_Manager.cs`: 타일/아이템/트랩/화살 등 네트워크 RPC 허브
    - `PMK_TileZoneSpawner.cs`: 존 단위로 타일 또는 트랩을 확률 생성
- `Prefab/`
  - `Map/`: 맵 블록 프리팹 모음(실제 생성은 `Resources/Maps` 기반)
  - `Trap/`, `Item/`, `Money/`, `Object/`, `Platform*.prefab`: 관련 오브젝트
- `Scenes/`
  - `PMK_Map_Test.unity`, `PMK_Trap_Test.unity`: 맵/트랩 테스트용 씬
- `Sprites/`
  - `Tile/`, `Traps/`: 시각 자원

### 네트워킹 원칙(Fusion 2)
- 모든 생성/파괴/상태 변경은 서버(StateAuthority)에서만 결정하고, RPC로 전파합니다.
- `RpcSources.StateAuthority` → `RpcTargets.All` 패턴으로 월드 동기화, 
  입력 기반 요청은 `RpcSources.InputAuthority` → `RpcTargets.StateAuthority`로 서버에 위임합니다.
- 네트워크 스폰은 반드시 `Runner.Spawn(...)`을 사용합니다.
- 지속 동기화 값은 `[Networked]` 프로퍼티로 관리합니다.

## 핵심 시스템 요약

### 1) 맵/타일 시스템
- `PMK_TileRogic`
  - 역할: 스테이지 전체 그리드 설계, 맵 프리팹 로딩, 타일 배치, 타일 아이템 생성, 특수 맵 삽입, 클리어 카운트 동기화
  - 인스펙터
    - `parentTrans`: 생성되는 오브젝트의 부모 Transform
    - `mainTilemap`: 실제 타일이 찍히는 타일맵
    - `maxTileX`, `maxTileY`: 스테이지 그리드 크기
    - `nextTileX`, `nextTileY`: 블록간 간격
    - `itemSpawnChance`: 타일 아이템 생성 확률(%)
    - `special_Map_Chance`: 특수 맵 생성 확률(%)
    - `RuleTile`: 기본 룰 타일
    - `trap[0]`, `trap[1]`: 트랩 프리팹(0: 즉사/낙하 트랩, 1: 돌 함정)
    - `launchTrapPrefab`: 네트워크 화살 오브젝트 프리팹
    - `[Networked] ClearCount`: 문 통과한 플레이어 수
  - 주요 동작 흐름
    - Resources/Maps 하위 프리팹을 자동 로드하여 타입별(파일명 `Map_*_*`)로 분류
    - 서버에서 `RPC_ResetMap()` 호출 → 기존 타일/자식 오브젝트/화살 정리 → 
      시작 맵, 출구 라인, 빈 공간 채우기, 특수 맵 배치, 타일 리프레시
    - 타일 생성 시 확률적으로 타일 아이템 생성(`Create_TileItem`)

- `PMK_TileRPC_Manager`
  - 역할: 타일/아이템/트랩/화살 관련 모든 네트워크 RPC의 허브
  - 주요 RPC
    - `RPC_Create_Tile(Vector3Int cellPos)`: 룰 타일 생성 후 아이템 생성 시도
    - `Rpc_DestroyTile(Vector3 pos)`: 월드 좌표 → 셀 좌표 변환 후 타일 제거
    - `RPC_Create_TileItem(int itemId, Vector3 worldPos, bool tileTrap)`: 아이템 또는 트랩 배치
    - `Rpc_DestroyItem(Vector3 pos)`: 해당 위치의 `Tileitem` 제거
    - `RPC_RndTileSpawn(int rnd, Vector3Int cellPos, int trapSpawnChance)`: 확률에 따라 타일/트랩 결정
    - `DelayedTileSpawnBool(Vector3Int cellPos)`: 주변 조건 따라 트랩 대체 또는 타일 생성
    - `RPC_SpawnArrow(Vector3 position, Vector2 velocity)`: 네트워크 화살 스폰 및 초기 속도 부여
    - `RPC_SetPlayerGravity([RpcTarget] PlayerRef player, float gravityScale)`: 대상 플레이어 중력 조절
    - `ClearAllArrows()`: 스테이지 초기화 시 잔여 화살 정리

- `PMK_TileZoneSpawner`
  - 역할: 특정 존에서 타일/트랩을 확률적으로 생성하는 일회성 스포너
  - 사용: 오브젝트를 원하는 위치에 배치 → 시작 시 서버가 `RPC_RndTileSpawn` 호출 → 1프레임 뒤 `DelayedTileSpawnBool` 보정 후 자멸

- `PMK_TileDestroyer`
  - 역할: 자신의 위치 셀에 타일이 없으면 생성, 있으면 해당 타일 및 타일아이템 파괴 후 자멸

### 2) 트랩/오브젝트 시스템
- `PMK_ArrowTrap`
  - 역할: `launchPoint`에서 `arrowDir` 방향으로 레이캐스트. `Ground` 이외의 레이어 감지 시 1회 발사 → `RPC_SpawnArrow`
  - 인스펙터: `arrowDir`, `shotSpeed`, `launchPoint`, `targetLayer`
  - 주의: 서버 권한(`HasStateAuthority`)일 때만 동작. `PMK_TileRogic.launchTrapPrefab` 설정 필요

- `PMK_ArrowTrapObj` (NetworkBehaviour, IItemInteraction)
  - 역할: 네트워크 화살 본체. 이동 방향으로 회전, 특정 속도·충돌 조건에서 정지
  - 동기화: `[Networked] bool IsHeld`(픽업/해제)
  - 메서드: `ApplyKnockback(...)`, `OnPickedUp()`, `OnReleased()` 등

- `PMK_DeathTrap`
  - 역할: 플레이어가 트리거 안/바깥으로 이동 시 서버가 `RPC_SetPlayerGravity`로 중력 스케일 변경

- `PMK_TileTrap`
  - 역할: 플레이어가 상단에서 낙하 충돌할 때만 공격 콜라이더 활성화. 넉백 적용 가능
  - 인스펙터: `AttackCollisionHandler`(공격 콜라이더 참조)

- `PMK_TileItem`
  - 역할: `DestroyItem()` 호출 시 `DataManager.Inst.ItemData[30000]`의 프리팹을 스폰하고 자기 자신 파괴

### 3) 문/스테이지 클리어 시스템
- `PMK_NextStageDoor`
  - 역할: 문 트리거 내에서 서버가 플레이어 도달을 집계하고, 전원 도달 시 다음 스테이트 전환
  - 흐름: `MarkPlayerCleared(PlayerRef)` → `[Networked] ClearCount` 증가 → 전원 도달 시 `GameStageCompletedState` 활성 → 5초 후 초기화

- `PlayerClearStageHandler` (플레이어 부착)
  - 역할: `NextDoor` 태그 트리거 안에서 W 입력 감지 → `RPC_RequestDoorClear()`로 서버에 알림
  - 키: W(문 상호작용)

## 프리팹/리소스 연동
- 맵 프리팹 로딩: `Resources/Maps` 경로의 프리팹들을 자동 로드하여 타입별(`Map_C_*`, `Map_D_*`, `Map_LR_*`, `Map_W*`, `Map_WD_*`, `Map_S_*`)로 배치합니다.
- 트랩 프리팹: `PMK_TileRogic.trap[0]`(즉사/낙하), `trap[1]`(돌함정) 연결 필요
- 화살 프리팁: `PMK_TileRogic.launchTrapPrefab`에 네트워크 오브젝트 프리팹 할당 후, 해당 프리팹에 `NetworkObject` 포함 필수

## 필수 태그/레이어
- 태그
  - `NextDoor`: 문 트리거 오브젝트
  - `Tileitem`: 타일 위 아이템(파괴/정리 시 탐색에 사용)
  - `BoobDestoryObj`: 폭발 등에 의해 파괴될 오브젝트(문자열 오탈자 주의)
- 레이어
  - `Player`, `Ground`, `Item`(스크립트에서 직접 참조함)
  - 트랩 감지용 `targetLayer`는 상황에 맞게 구성

## 설치/세팅 절차(요약)
1) 씬에 `PMK_TileRogic`과 `PMK_TileRPC_Manager`를 배치
   - `PMK_TileRogic`의 `parentTrans`, `mainTilemap`, `RuleTile`, `trap[]`, `launchTrapPrefab` 할당
2) 문 오브젝트에 `PMK_NextStageDoor` 부착, 태그를 `NextDoor`로 지정, 트리거 Collider 설정
   - 플레이어 프리팹에 `PlayerClearStageHandler` 부착
3) 트랩 배치 시
   - 화살 트랩: `PMK_ArrowTrap`의 `launchPoint`, `targetLayer`, `shotSpeed` 설정
   - 타일 트랩: `PMK_TileTrap`의 `AttackCollisionHandler` 연결
4) 존 기반 생성은 `PMK_TileZoneSpawner` 프리팹을 원하는 위치에 배치(서버에서 생성/보정 후 자멸)
5) 테스트: `PMK_Map_Test.unity` 또는 `PMK_Trap_Test.unity` 실행

## 입력/제어 요약
- 문 상호작용: 문 트리거 내에서 W 입력 → 서버에 클리어 요청(`PlayerClearStageHandler`)
- 맵 리셋: 서버에서 `RPC_ResetMap()` 호출(개발 중에는 `PMK_TileRogic.Update()`의 숫자키 바인딩 참고: `Alpha1`)
- 폭발 아이템: `PMK_TileDestroyItem`이 `delayBeforeBoom` 경과 후 반경 내 타일/아이템 파괴

## 주의 및 팁
- 서버 권한이 아닐 때는 생성/파괴/물리 제어를 하지 않도록 스크립트들이 `HasStateAuthority`를 확인합니다. 오브젝트 배치 시 반드시 서버가 관리하도록 하세요.
- 타일/아이템 파괴는 태그/레이어에 의존하므로, 태그(`Tileitem`, `BoobDestoryObj`)와 레이어(`Ground`, `Player`, `Item`)를 정확히 맞추세요.
- `PMK_TileRogic`은 partial 클래스입니다. 테스트 모드/추가기능이 다른 partial에 있을 수 있습니다.

## 공개 메서드/프로퍼티(참고)
- `PMK_TileRogic`
  - `RPC_ResetMap()`, `ResetMap()`, `Create_TileItem(Vector3Int targetPos)`
  - `mainTilemap`, `parentTrans`, `ruleTile`, `launchTrapPrefab`, `[Networked] ClearCount`
- `PMK_TileRPC_Manager`
  - `RPC_Create_Tile(...)`, `Rpc_DestroyTile(...)`, `RPC_Create_TileItem(...)`, `Rpc_DestroyItem(...)`
  - `RPC_RndTileSpawn(...)`, `DelayedTileSpawnBool(...)`, `RPC_SpawnArrow(...)`, `RPC_SetPlayerGravity(...)`, `ClearAllArrows()`
- `PMK_NextStageDoor`
  - `MarkPlayerCleared(PlayerRef player)`, `HasPlayerCleared(PlayerRef player)`
- `PlayerClearStageHandler`
  - `[Rpc] RPC_RequestDoorClear()`

## 테스트 체크리스트
- 서버에서 시작했을 때만 맵이 생성/리셋되는가?
- 문 트리거 안에서 W 입력 시 모든 플레이어 도달 조건에 맞춰 다음 스테이트로 전환되는가?
- 화살 트랩이 감지 후 한 번만 발사되고, 화살은 네트워크 오브젝트로 동기화되는가?
- 폭발 아이템이 반경 내 타일과 타일아이템을 정상 파괴하는가?

필요한 추가 항목이나 상세도가 더 필요한 섹션이 있으면 알려주세요. 바로 보강하겠습니다.


