using System;
using Fusion;
using TMPro;
using UnityEngine;

// 🌐 NetworkBehaviour 상속: Unity의 MonoBehaviour와 비슷하지만 네트워크 기능이 추가됨
// 이 클래스는 네트워크로 동기화되는 객체가 됨 (모든 클라이언트에서 같은 상태 유지)
public class GameManager : NetworkBehaviour
{
    public event Action OnGameIsOver;
    
    // 🔒 static 변수: 모든 GameManager 인스턴스가 공유하는 변수
    // 네트워크와 관계없이 로컬에서만 사용 (각 클라이언트마다 개별 관리)
    public static bool MatchIsOver { get; private set; }
    
    [field: SerializeField] public Collider2D CameraBounds { get; private set; }
    [SerializeField] private Camera cam;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float matchTimerAmount = 60;

    // 🌐 [Networked]: 이 변수는 네트워크로 동기화됨!
    // 모든 클라이언트에서 같은 값을 가지게 됨 (Host가 변경하면 모든 Client에 자동 전파)
    // private set: 외부에서 직접 수정 불가, 네트워크를 통해서만 변경됨
    [Networked] private TickTimer matchTimer { get; set; }

    private void Awake()
    {
        if (GlobalManagers.Instance != null)
        {
            GlobalManagers.Instance.GameManager = this;
        }
    }

    // 🌐 NetworkBehaviour 라이프사이클 메서드
    // Unity의 Start()와 비슷하지만 네트워크 객체가 생성될 때 호출됨
    // 모든 클라이언트에서 호출되지만 Host와 Client에서 실행 타이밍이 다를 수 있음
    public override void Spawned()
    {
        // 🌐 Runner.SetIsSimulated: 이 객체가 물리 시뮬레이션에 참여할지 설정
        // true = 물리 계산 참여, false = 물리 계산 제외 (성능 최적화)
        Runner.SetIsSimulated(Object, true);
        
        // 🔒 로컬 변수 초기화 (네트워크 동기화 안됨)
        MatchIsOver = false;

        // 🎮 로컬 카메라 비활성화 (각 플레이어마다 자신의 카메라만 활성화되어야 함)
        cam.gameObject.SetActive(false);

        // 🌐 서버 권한 체크: Host/Server에서만 실행되는 코드
        // Client에서는 실행되지 않음 (권한 분리를 통한 치트 방지)
        if (Runner.IsServer)
        {
            // 🌐 TickTimer: Fusion의 네트워크 타이머 (일반 Unity Timer와 다름)
            // 네트워크 틱 기반으로 동작하여 모든 클라이언트에서 동기화됨
            // CreateFromSeconds: 초 단위로 타이머 생성
            matchTimer = TickTimer.CreateFromSeconds(Runner, matchTimerAmount);
        }
    }

    // 🌐 NetworkBehaviour 라이프사이클 메서드
    // Unity의 FixedUpdate()와 비슷하지만 네트워크 틱 기반으로 동작
    // 모든 클라이언트에서 같은 주기로 호출되어 동기화 보장
    public override void FixedUpdateNetwork()
    {
        // 🌐 TickTimer 상태 체크
        // Expired: 타이머가 만료되었는지 확인
        // RemainingTime: 남은 시간 반환 (HasValue로 유효성 체크)
        if (matchTimer.Expired(Runner) == false && matchTimer.RemainingTime(Runner).HasValue)
        {
            // 🕐 남은 시간을 사용자에게 표시
            var timeSpan = TimeSpan.FromSeconds(matchTimer.RemainingTime(Runner).Value);
            var outPut = $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            timerText.text = outPut;
        }
        // 🌐 타이머 만료 체크
        else if (matchTimer.Expired(Runner))
        {
            // 🔒 로컬 변수 업데이트 (각 클라이언트에서 개별 실행)
            MatchIsOver = true;
            
            // 🌐 타이머 정리: TickTimer.None으로 설정하여 메모리 정리
            matchTimer = TickTimer.None;
            
            // 🔔 이벤트 발생: 게임 종료를 다른 스크립트들에게 알림
            OnGameIsOver?.Invoke();
            
            Debug.Log("Match timer had ended");
        }
    }
}

// 📚 Fusion 네트워크 개념 정리:
//
// 🏗️ NetworkBehaviour vs MonoBehaviour:
// - MonoBehaviour: Unity 기본 컴포넌트 (로컬에서만 동작)
// - NetworkBehaviour: Fusion 네트워크 컴포넌트 (네트워크 동기화 지원)
//
// 🌐 [Networked] 프로퍼티:
// - 모든 클라이언트에서 자동으로 동기화되는 변수
// - Host가 값을 변경하면 모든 Client에 자동 전파
// - 네트워크 대역폭을 사용하므로 꼭 필요한 데이터만 사용
//
// ⏰ TickTimer vs Unity Timer:
// - Unity Timer: Time.time, Time.deltaTime 기반 (클라이언트마다 다를 수 있음)
// - TickTimer: 네트워크 틱 기반 (모든 클라이언트에서 정확히 동기화)
// - 멀티플레이어 게임에서는 TickTimer 사용 필수!
//
// 🔐 서버 권한 (Runner.IsServer):
// - Host/Server에서만 실행되어야 하는 로직 구분
// - 게임 상태 변경, 타이머 관리 등은 서버에서만 처리
// - 치트 방지와 일관성 유지를 위한 필수 패턴
//
// 🎯 라이프사이클 비교:
// - Unity: Awake → Start → Update/FixedUpdate
// - Fusion: Awake → Spawned → FixedUpdateNetwork
// - Spawned: 네트워크 객체가 모든 클라이언트에 생성될 때 호출
// - FixedUpdateNetwork: 네트워크 틱마다 호출 (동기화 보장)