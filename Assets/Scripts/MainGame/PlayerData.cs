using Fusion;
using UnityEngine;

// 🎮 플레이어 입력 데이터 구조체
// struct 사용 이유: 값 타입이라 성능이 좋고, 네트워크 전송에 최적화됨
// INetworkInput 인터페이스: Fusion에게 "이것은 네트워크 입력 데이터야"라고 알려줌
public struct PlayerData : INetworkInput
{
    // 🏃 좌우 이동 입력 (-1.0 ~ 1.0)
    // A/D 키 또는 좌/우 화살표 키 입력
    // -1.0 = 왼쪽, 0.0 = 정지, 1.0 = 오른쪽
    public float HorizontalInput;
    
    // 🎯 총구 회전 데이터
    // 마우스 위치에 따라 총구가 바라보는 방향을 Quaternion으로 저장
    // 모든 클라이언트에게 전송되어 다른 플레이어의 총구 방향을 보여줌
    public Quaternion GunPivotRotation;
    
    // 🔘 버튼 입력 데이터 (비트 플래그 방식)
    // 여러 버튼을 하나의 변수로 효율적으로 관리
    // 예: 점프, 발사, 재장전 등의 버튼 상태를 동시에 저장
    public NetworkButtons NetworkButtons;
}

// 📚 Fusion 네트워크 입력 시스템 개념 정리:
//
// 🎯 INetworkInput 인터페이스란?
// - Fusion에게 "이 구조체는 네트워크 입력 데이터"라고 알려주는 마커
// - 이 인터페이스를 구현한 struct는 자동으로 네트워크로 전송됨
// - 모든 클라이언트의 입력이 Host로 전송되어 처리됨
//
// 💡 struct vs class 선택 이유:
// - struct: 값 타입, 스택 메모리, 복사 비용 낮음, 네트워크 전송 최적화
// - class: 참조 타입, 힙 메모리, GC 압박, 네트워크 전송 비효율적
// - 입력 데이터는 매 틱마다 생성되므로 성능이 중요함!
//
// 🔄 네트워크 입력 흐름:
// 1. 각 클라이언트에서 입력 수집 (키보드, 마우스)
// 2. PlayerData 구조체에 입력 데이터 저장
// 3. 자동으로 Host에게 전송 (Fusion이 처리)
// 4. Host에서 모든 플레이어의 입력을 받아 게임 로직 처리
// 5. 결과를 모든 클라이언트에게 동기화
//
// 🎮 각 필드의 역할:
// - HorizontalInput: 연속적인 입력 (아날로그 값)
// - GunPivotRotation: 3D 회전 데이터 (마우스 조준)
// - NetworkButtons: 디지털 입력 (버튼 눌림/안눌림)
//
// 🔗 다른 스크립트와의 연관성:
// - LocalInputPoller: 이 구조체에 입력 데이터를 채움
// - PlayerController: 이 구조체의 데이터를 읽어서 캐릭터 제어
// - PlayerWeaponController: GunPivotRotation으로 총구 회전, NetworkButtons로 발사 처리
//
// ⚡ 성능 최적화 포인트:
// - 필요한 최소한의 데이터만 포함 (네트워크 대역폭 절약)
// - 비트 플래그로 여러 버튼을 하나의 변수에 저장
// - float 대신 압축된 데이터 타입 사용 가능 (고급 기법)
//
// 🎯 사용 예시:
// var input = new PlayerData
// {
//     HorizontalInput = Input.GetAxis("Horizontal"),
//     GunPivotRotation = CalculateGunRotation(),
//     NetworkButtons = GetPressedButtons()
// };
// 이 데이터가 자동으로 네트워크를 통해 전송됨!
