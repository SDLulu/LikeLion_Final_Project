# 🎮 LikeLion Final Project - 핵심 규칙

## ⚠️ **절대 준수 필수 규칙**

### 🤖 **AI가 지켜야 할 규칙**
- **AI 모델 표시**: 답변 시작 시 "(claude-3.5-sonnet)" 형식으로 표시
- **언어**: 항상 한글로 답변 및 주석 작성
- **멀티플레이**: 포톤 퓨전2 공식 문서 최신 정보 기반으로 답변
- **파일 배치**: 새로운 코드 생성 시 "파일 위치 규칙"에 명시된 정확한 폴더에 배치
- **네이밍**: 모든 코드와 프로퍼티, 변수명은 "네이밍 규칙"을 엄격히 준수

### 👨‍💻 **개발자가 지켜야 할 규칙**
1. **프로퍼티 작성**: 화살표 함수(=>) 대신 `{ get; set; }` 형식 사용
2. **주석 작성**: XML 문서 주석(/// <summary>) 사용 금지
3. **컴포넌트 참조**: 모든 GetComponent 호출을 Spawned()에서 처리
4. **권한 체크**: FixedUpdateNetwork() 시작 부분에서 권한 확인
5. **상태 동기화**: 계속 변하는 값은 `[Networked]` 프로퍼티 사용
6. **타이머**: 시간 기반 로직은 `TickTimer` 사용
7. **RPC 사용**: Update/FixedUpdateNetwork에서 RPC 호출 금지
8. **클라이언트 신뢰**: 검증 없는 클라이언트 정보 신뢰 금지
9. **무거운 함수**: FixedUpdateNetwork에서 GameObject.Find() 등 사용 금지

- 움직임/애니메이션이 있는 모든 NetworkObject(캐릭터, 무기, 아이템 등)는 Spawned()에서 HasInputAuthority가 없는 경우 반드시 다음 코드를 실행해야 한다:

  if (!HasInputAuthority)
  {
      Runner.SetIsSimulated(Object, true);
      base.Object.RenderSource = RenderSource.Interpolated;
      base.Object.ForceRemoteRenderTimeframe = true;
  }

이 규칙은 네트워크 보간/시뮬레이션의 일관성을 위해 필수이다.

- 회전 애니메이션(localRotation)을 사용하는 아이템(예: 곡괭이 등)은 FixedUpdateNetwork 등에서 반드시 부모가 있을 때만 localRotation을 변경해야 하며, 부모가 없을 때(던진 상태, 월드에 떨어진 상태)에는 localRotation을 절대 건드리지 않아야 한다. 이 규칙을 지키면 아이템을 던질 때 월드 회전값이 자연스럽게 유지된다.

// 🔄 문서 업데이트 워크플로우 (개발자가 반드시 지켜야 함)
// 1. 업데이트 시점: 새로운 핵심 기능 추가 또는 기존 중요 로직 크게 변경 시
// 2. AI 요약 요청: 기능 개발 완료 후, AI에게 "방금 완성된 OOO 기능의 핵심 로직과 규칙을 이 문서에 추가할 수 있도록 마크다운 형식으로 요약해줘" 요청
// 3. 개발자 검토 및 반영: AI가 생성한 요약 내용을 개발자가 직접 검토하고 수정한 뒤, 문서의 적절한 위치에 반영

## 🎯 **게임 개발 컨텍스트**
- **엔진**: Unity
- **멀티플레이**: Photon Fusion 2 최신버전
- **모드**: 호스트-클라이언트 모드
- **참고 게임**: 스펠렁키 2
- **개발자**: 학생 (게임개발 학습 중)

## 🎮 **핵심 게임 메커닉**
- **이동**: A/D 또는 방향키 (좌우)
- **점프**: Space키 (웅크린 상태가 아닐 때)
- **웅크리기**: 아래 방향키 + 땅에 있을 때
- **사다리 오르기**: W키 (사다리 근처에서)
- **아이템 줍기**: Space + 웅크린 상태
- **아이템 사용**: 마우스 좌클릭
- **아이템 던지기**: 마우스 우클릭
- **스킬**: Shift키

## 🎭 **상태 시스템**
- **기본 상태** (상호 배타적): Idle, Walking, Ducking, Jumping, Falling, Climbing, Stunned, Dead
- **액션 상태** (동시에 여러 개 가능): UsingItem, ThrowingItem, PickingUpItem, UsingSkill, Invincible
- **아이템 사용**: 대부분의 상태에서 가능 (Stunned/Dead 제외)
- **아이템 줍기**: Ducking 상태에서만 가능