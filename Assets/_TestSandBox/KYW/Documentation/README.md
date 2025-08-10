## KYW 모듈 개요

이 문서는 `Assets/_TestSandBox/KYW` 폴더 내 스펠렁키형 플레이어 시스템, 아이템/전투, 사다리/이동, 사망/유령, UI/디버그 전반을 누구나 빠르게 이해하고 유지보수할 수 있도록 정리한 가이드입니다. 모든 네트워크 로직은 Photon Fusion 2(Host-Client) 공식 패턴에 맞춰 작성되어 있습니다.


### 주요 특징
- 플레이어 코어: `SpelunkyPlayerController`가 이동/점프/사다리/아이템/상호작용 등 컴포넌트를 오케스트레이션
- 입력 파이프라인: `SpelunkyLocalInputPoller`가 로컬 입력을 Fusion 즉시 입력으로 수집(SpelunkyPlayerInputData)
- 이동/점프/사다리: `PlayerMovement`, `PlayerJump`, `PlayerClimbing`(+ `Ladder`/체크 컴포넌트)
- 아이템 시스템: `PlayerObjectPickup`(줍기) · `PlayerItemUsage`(사용/회전) · `PlayerObjectThrower`(던지기) · `PlayerInventory`(데이터만)
- 전투/투사체: 무기(샷건/기관단총/드릴 등) + 충돌 핸들러(근접/총알/드릴), 투사체(총알/플라즈마)
- 사망/유령: `PlayerHealth` + `PlayerDeathHandler` + `PlayerCorpse`(페이드아웃) + `PlayerGhostController`
- UI/디버그: `UI_PlayerSlot`(돈/체력/패시브 아이콘), `PlayerSlotUIManager`(등록/갱신), `PlayerDebugManager`/`PlayerStateDebugger`


## 폴더 맵

- `Scripts/`
  - `Core/`: `SpelunkyPlayerController`, `SpelunkyLocalInputPoller`, `SpelunkyNetworkInitializer`, `SpelunkyPlayerData`
  - `Control/`: `PlayerMovement`, `PlayerJump`, `PlayerClimbing`, `PlayerAnimation`, `PlayerInteraction`, `PlayerStunInvincibleDie` 등
  - `Control/Check/`: `PlayerGroundCheck`, `PlayerLadderCheck`, `PlayerWallCheck`
  - `Items/ActiveItems/`: `Shotgun`, `MachineGun`, `Drill`, `Pickaxe`, `PlasmaGun`, `Potion` 등
  - `Items/PassiveItems/`: `Wings`, `SpeedShoes`, `JumpShoes`, `Rocket`, `Magnet`, `Headset`, `Sunglasses`
  - `Items/Projectile/`: `Bullets`, `PlasmaBullet`
  - `Items/Collsion/`: `AttackCollisionHandler`, `BulletAttackCollisionHandler`, `DrillAttackCollisionHandler`, `SpeedCollisonHandler`
  - `Data/`: `PlayerInventory`, `PlayerHealth`, `PlayerSlotUIManager`, `UI_PlayerSlot`
  - `Death/`: `PlayerCorpse`, `Ghost/*`(유령 컨트롤러/입력/입김)
  - `Interface/`: `IItemInteraction`, `IPlayerInteraction`, `IHoldable`, `IUsableItem`, `IDamageable`, `IStunnable`, `IKnockbackable`, `IInvincible`
  - `Util/`: `*CheckVisualizer`, `DevAutoStarter` 등
- `Prefabs/`: `Player`, `GhostPlayer`, `UI_PlayerSlot`, 각종 무기/아이템/투사체/Ladder 등
- `Resources/`: 아이콘/투사체 시트/타일셋/이펙트 스프라이트 등
- `Scenes/`: `DebugRoomKYW.unity`, `Lobby.unity`, `FeelTest.unity`


## 플레이어 런타임 플로우

```mermaid
flowchart TD
  A[SpelunkyLocalInputPoller] --> B[SpelunkyPlayerController.GetNetworkInputData]
  B --> C[FixedUpdateNetwork]
  C --> C1[PlayerMovement.ProcessInput]
  C --> C2[PlayerJump.ProcessInput]
  C --> C3[PlayerClimbing.ProcessInput]
  C --> C4[PlayerObjectPickup/Usage/Thrower.ProcessInput]
  C --> C5[PlayerInventory.ProcessInput]
  C1 --> D[네트워크 상태 업데이트(IsDucking/IsFacingLeft...)]
  C4 --> E[아이템 RPC(픽업/사용/던지기)]
  E --> F[CollisionHandlers(데미지/넉백)]
  F --> G[PlayerHealth.TakeDamage/Death]
  G --> H[PlayerCorpse 페이드아웃/유령 스폰]
```


## 코어 구조

- `SpelunkyPlayerController`(NetworkBehaviour)
  - 전역 오케스트레이터. 하위 오브젝트 `Visual`, `Hand`를 참조해 렌더/아이템 시스템을 결선
  - 각 서브 컴포넌트의 `ProcessInput`를 순서대로 호출하여 이동/점프/사다리/아이템/상호작용 수행
  - 상태 접근 헬퍼: `IsDead/IsStunned/IsInvincible/IsHeld/IsThrown/IsNormal`
  - 네트워크 입력 데이터 구성: `SpelunkyPlayerInputData`(축 + NetworkButtons) 반환
- `SpelunkyLocalInputPoller`(INetworkRunnerCallbacks)
  - 로컬 권한에서만 Runner에 즉시 입력을 공급. `input.Set(SpelunkyPlayerInputData)`
- `SpelunkyNetworkInitializer`
  - Spawned 시 네트워크/카메라/UI 초기화. 원격은 `RenderSource.Interpolated`/`ForceRemoteRenderTimeframe` 활용

입력 버튼(예시): `Jump`, `DownJump`, `PickupItem`, `UseItemHold`, `ThrowItem`, `Interact`, `Skill`, `Death`, `pick`, `buy`, `drop`


## 이동/점프/사다리

- `PlayerMovement`
  - 수평 이동/웅크리기/위보기. 네트워크 상태(`IsDucking/IsLookingUp/IsFacingLeft/NormalizedSpeed`) 관리
  - 패시브 `SpeedShoes` 보유 시 이동속도 배율 적용
- `PlayerJump`
  - 가변 점프(버튼 유지 시간 기반), 최대 낙하 속도 제한, `JumpShoes`/`Wings`(다단 점프)·`Rocket`(공중 추진) 지원
  - Fusion 공식 버튼 래칭(`ButtonsPrevious` + `GetPressed`) 사용. 로켓 연료 `TickTimer`로 소모/충전
- `PlayerClimbing` + `Ladder`
  - 사다리 근처에서 위키(W) 유지 시 매달림 시작(쿨타임 존재). 사다리 중앙으로 X축 흡입, 위/아래키로 이동
  - 점프로 사다리 이탈 및 즉시 점프 진입


## 아이템 시스템(줍기/사용/던지기/인벤토리)

- `PlayerObjectPickup`
  - 입력 래칭 기반으로 가장 가까운 대상 탐색 후 `PickupObjectRpc` 호출(InputAuthority→StateAuthority)
  - 플레이어/아이템 구분해 `OnPickedUp` 등 인터페이스 콜백 호출, 물리 비활성화(Kinematic/Trigger)
- `PlayerItemUsage`
  - 좌클릭 홀드 기반 `IUsableItem`의 Press/Hold/Release 호출. 마우스 방향으로 Hand 회전(네트워크 각도 동기화)
  - 아이템 없을 때 `BasicPunchItem` 사용
- `PlayerObjectThrower`
  - 우클릭 래칭으로 `ThrowObjectRpc`(IA→All). StateAuthority에서 해제/물리 복구/힘 적용
  - 부모 해제 지연은 `TickTimer`로 처리(던진 직후 자충돌 방지)
- `PlayerInventory`(데이터만)
  - 손에 든 오브젝트(`NetworkObject`), 돈, 패시브 보유 여부(네트워크 bool) 유지 및 OnChangedRender로 UI 이벤트 발생


## 전투/투사체/충돌

- 무기(ActiveItems): `Shotgun`(산탄 RPC/쿨다운), `MachineGun`(연사/재장전), `Drill`(연속공격/스프라이트 회전)
- 투사체: `Bullets`(충돌 횟수 제한 후 소멸), `PlasmaBullet`(수명/히트 처리/이펙트)
- 충돌 핸들러: `AttackCollisionHandler`(근접), `BulletAttackCollisionHandler`(총알), `DrillAttackCollisionHandler`(드릴)
  - 공통 정책: StateAuthority에서만 판정, 같은 아이템/자식 트리 무시, `IPlayerInteraction`/`IItemInteraction`으로 통합 적용


## 사망/유령

- `PlayerHealth` OnChangedRender로 UI 갱신/사망 시 `PlayerDeathHandler.Die()`
- `PlayerCorpse`는 Tick 기반 페이드아웃 후 `Despawn`
- `PlayerGhostController`는 별도의 입력 데이터(`GhostInputData`)로 이동/회전/입김/대쉬를 처리, `GhostLocalInputPoller`가 입력 공급


## UI/디버그

- `UI_PlayerSlot`: 돈/체력/패시브 아이콘을 `PlayerInventory.OnInventoryDataChanged`, `PlayerHealth.OnHealthChangedEvent`로 갱신
- `PlayerSlotUIManager`: 플레이어 UI 등록/해제/일괄 갱신
- `PlayerDebugManager`/`PlayerStateDebugger`: 런타임 상태/테스트 입력을 한 화면에서 확인


## 빠른 테스트 절차

1) 씬 열기: `DebugRoomKYW.unity`
- `Player` 프리팹 배치(필요 시 `SpelunkyLocalInputPoller` 포함)
- 무기/아이템/사다리 프리팹을 근처에 배치해 상호작용 확인

2) 조작
- 기본 이동: A/D, 점프: Space(웅크리면 밑점프), 사다리: W 유지로 매달림 시작, 좌클릭: 사용, 우클릭: 던지기
- 패시브 아이템은 손에 든 뒤 좌클릭 1회로 소비되어 인벤토리에 반영


## Fusion2 사용 포인트(공식 문서 준수)

- 권한/소스/타겟: StateAuthority에서만 상태 변경/판정, 입력은 InputAuthority에서 수집 → RPC는 최소 권한/타겟으로 선언
- 네트워크 필드: `[Networked]` + `OnChangedRender`로 UI 연동. 투사체/무기는 `RenderSource.Interpolated` 권장
- 타이머/쿨다운: `TickTimer`로 재장전/쿨다운/부모해제 지연 관리
- 물리: `Rigidbody2D.linearVelocity` 기반 제어, 플랫폼 밑점프는 충돌 무시 토글로 처리

참고: Fusion 2 공식 문서
- Fusion Manual(Home): https://doc.photonengine.com/fusion/current/
- Network Runner: https://doc.photonengine.com/fusion/current/manual/fundamentals/network-runner
- Scene Loading: https://doc.photonengine.com/fusion/current/manual/scene-loading
- RPCs: https://doc.photonengine.com/fusion/current/manual/remote-procedures
- Custom Object Provider: https://doc.photonengine.com/fusion/current/manual/object-spawning#custom-object-provider
- Timers(TickTimer): https://doc.photonengine.com/fusion/current/manual/simulation/timers


## 리소스 체크리스트

- 프리팹: `Player`, `GhostPlayer`, `UI_PlayerSlot`, 각종 무기/아이템/투사체/사다리
- 스프라이트: `Resources/PlayerUI/*`, `발사체/*`, `파티클 이펙트/*`, `디버깅룸/*`
- 씬: `DebugRoomKYW.unity`, `Lobby.unity`, `FeelTest.unity`


## 참고(파일)

- 코어: `SpelunkyPlayerController.cs`, `SpelunkyLocalInputPoller.cs`, `SpelunkyNetworkInitializer.cs`
- 이동/점프/사다리: `PlayerMovement.cs`, `PlayerJump.cs`, `PlayerClimbing.cs`, `Ladder.cs`, `*Check.cs`
- 아이템: `PlayerObjectPickup.cs`, `PlayerItemUsage.cs`, `PlayerObjectThrower.cs`, `PlayerInventory.cs`
- 전투/투사체/충돌: `Shotgun.cs`, `MachineGun.cs`, `Drill.cs`, `Bullets.cs`, `PlasmaBullet.cs`, `*CollisionHandler.cs`
- 사망/유령: `PlayerHealth.cs`, `PlayerCorpse.cs`, `Ghost/*`
- UI/디버그: `UI_PlayerSlot.cs`, `PlayerSlotUIManager.cs`, `PlayerDebugManager.cs`, `PlayerStateDebugger.cs`

