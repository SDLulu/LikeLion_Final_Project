using Fusion;
using UnityEngine;

// 🎮 초간단 테스트 시작 클래스
// 복잡한 설정 없이 바로 Host 모드로 게임 시작
public class SimpleTestStarter : MonoBehaviour
{
    [Header("⚡ Quick Test")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private KeyCode manualStartKey = KeyCode.F1; // F1 키로 수동 시작
    
    private bool hasStarted = false;

    private void Start()
    {
        if (autoStart && !hasStarted)
        {
            StartHostGame();
        }
    }
    
    private void Update()
    {
        // F1 키로 수동 시작
        if (Input.GetKeyDown(manualStartKey) && !hasStarted)
        {
            StartHostGame();
        }
    }
    
    private void StartHostGame()
    {
        hasStarted = true;
        
        // 기존 NetworkRunnerController 찾기
        var networkController = FindObjectOfType<NetworkRunnerController>();
        
        if (networkController != null)
        {
            Debug.Log("🎮 [SimpleTestStarter] Host 모드로 테스트 시작!");
            networkController.SetPlayerNickname("TestPlayer");
            networkController.StartGame(GameMode.Host, "TestRoom");
        }
        else
        {
            Debug.LogError("❌ NetworkRunnerController를 찾을 수 없습니다! 씬에 추가해주세요.");
        }
    }
} 