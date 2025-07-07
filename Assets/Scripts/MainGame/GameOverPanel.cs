using UnityEngine;
using UnityEngine.UI;

// 🏁 게임 종료 시 표시되는 UI 패널을 관리하는 클래스
// 📦 MonoBehaviour: 순수 Unity 컴포넌트 (UI는 네트워크 동기화 불필요)
//
// 💡 UI가 왜 MonoBehaviour인가?
// - UI는 각 플레이어 개인의 화면이므로 동기화 불필요
// - 게임 종료 이벤트만 동기화되고, UI 표시는 로컬에서 처리
// - 각자 자신의 언어/설정에 맞게 UI를 다르게 표시할 수 있음
public class GameOverPanel : MonoBehaviour
{
    // 🔙 로비로 돌아가는 버튼
    [SerializeField] private Button returnToLobbyBtn;
    
    // 🎨 게임 오버 UI 요소들을 담고 있는 부모 오브젝트
    // 평소에는 비활성화되어 있다가 게임 종료 시에만 활성화
    [SerializeField] private GameObject childObj;
        
    // 🎬 게임 시작 시 한 번 호출
    private void Start()
    {
        // 📡 GameManager의 게임 종료 이벤트에 구독
        // 게임이 끝나면 OnMatchIsOver 함수가 자동으로 호출됨
        GlobalManagers.Instance.GameManager.OnGameIsOver += OnMatchIsOver;
        
        // 🔙 로비 복귀 버튼에 클릭 이벤트 등록
        // 버튼을 누르면 네트워크 연결을 종료하고 로비로 돌아감
        returnToLobbyBtn.onClick.AddListener(() => GlobalManagers.Instance.NetworkRunnerController.ShutDownRunner());
    }

    // 🏁 게임이 종료되었을 때 호출되는 함수
    // GameManager의 OnGameIsOver 이벤트에서 호출됨
    private void OnMatchIsOver()
    {
        // 🎨 게임 오버 UI를 활성화하여 플레이어에게 표시
        childObj.SetActive(true);
    }

    // 🗑️ 오브젝트가 파괴될 때 호출 (메모리 누수 방지)
    private void OnDestroy()
    {
        // 📡 이벤트 구독 해제 (중요!)
        // 이벤트 구독을 해제하지 않으면 메모리 누수가 발생할 수 있음
        GlobalManagers.Instance.GameManager.OnGameIsOver -= OnMatchIsOver;
    }
}
