using Fusion;
using TMPro;
using UnityEngine;

// ⏰ 플레이어 리스폰 UI 패널을 관리하는 컨트롤러
// 📡 NetworkBehaviour: 네트워크 동기화가 필요한 컴포넌트
public class RespawnPanel : NetworkBehaviour
{
    // 🎮 참조할 플레이어 컨트롤러 (리스폰 타이머 정보 가져오기)
    [SerializeField] private PlayerController playerController;
    
    // 📱 UI 요소들
    [SerializeField] private TextMeshProUGUI respawnAmountText; // 리스폰 남은 시간 텍스트
    [SerializeField] private GameObject childObj;              // 패널 전체 오브젝트

    // 🎬 네트워크 오브젝트가 생성될 때 한 번 호출
    public override void Spawned()
    {
        // ⚙️ 이 오브젝트의 시뮬레이션을 활성화
        // UI 업데이트를 위해 필요
        Runner.SetIsSimulated(Object, true);
    }

    // 📡 네트워크 물리 업데이트 (고정된 프레임레이트로 실행)
    public override void FixedUpdateNetwork()
    {
        // 🎮 로컬 플레이어인 경우에만 UI 업데이트
        // 다른 플레이어의 리스폰 패널은 보일 필요 없음!
        if (Utils.IsLocalPlayer(Object))
        {
            // ⏰ 리스폰 타이머가 실행 중인지 확인
            var timerIsRunning = playerController.RespawnTimer.IsRunning;
            
            // 👁️ 타이머 실행 여부에 따라 패널 표시/숨김
            childObj.SetActive(timerIsRunning);

            // ⏱️ 타이머가 실행 중이고 남은 시간이 있다면
            if (timerIsRunning && playerController.RespawnTimer.RemainingTime(Runner).HasValue)
            {
                // 📊 남은 시간을 가져와서 정수로 반올림
                var time = playerController.RespawnTimer.RemainingTime(Runner).Value;
                var roundInt = Mathf.RoundToInt(time);
                
                // 📝 UI 텍스트 업데이트 (예: "3", "2", "1")
                respawnAmountText.text = roundInt.ToString();
            }
        }
    }
}
