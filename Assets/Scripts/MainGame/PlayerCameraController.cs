using Cinemachine;
using UnityEngine;

// 📷 플레이어 카메라 제어를 담당하는 컨트롤러
// 📦 MonoBehaviour: 순수 Unity 컴포넌트 (네트워크 동기화 불필요)
//
// 💡 왜 MonoBehaviour인가?
// - 카메라는 각 플레이어 개인의 시점이므로 동기화 불필요
// - 로컬 플레이어에게만 활성화되어 개인적으로 동작
// - 피격 효과, 흔들림 등은 당사자만 보면 됨
public class PlayerCameraController : MonoBehaviour
{
    // 📳 카메라 흔들림 효과를 생성하는 Cinemachine 컴포넌트
    [SerializeField] private CinemachineImpulseSource impulseSource;
    
    // 🗺️ 카메라가 움직일 수 있는 범위를 제한하는 컴포넌트
    // 맵 밖으로 카메라가 나가지 않도록 경계 설정
    [SerializeField] private CinemachineConfiner2D cinemachineConfiner2D;

    // 🎬 게임 시작 시 한 번 호출
    private void Start()
    {
        // 🗺️ GameManager에서 설정된 카메라 경계(맵 범위)를 가져와서 적용
        // 이렇게 하면 카메라가 맵 밖으로 나가지 않음
        cinemachineConfiner2D.m_BoundingShape2D = GlobalManagers.Instance.GameManager.CameraBounds;
    }

    // 📳 카메라 흔들림 효과를 실행하는 함수
    // PlayerHealthController에서 피격 시 호출됨
    public void ShakeCamera(Vector3 shakeAmount)
    {
        // 🎯 지정된 강도로 카메라 흔들림 생성
        // shakeAmount: (X축 흔들림, Y축 흔들림, 지속시간) 형태
        impulseSource.GenerateImpulse(shakeAmount);
    }
}
