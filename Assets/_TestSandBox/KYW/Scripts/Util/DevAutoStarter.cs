using System.Collections;
using Fusion;
using UnityEngine;

// 🛠️ 개발용 자동 시작 클래스
// Unity Play 버튼만 누르면 바로 테스트 가능하게 해주는 편의 도구
public class DevAutoStarter : MonoBehaviour
{
    [Header("🛠️ Development Settings")]
    [SerializeField] private bool enableAutoStart = true; // 자동 시작 활성화/비활성화
    [SerializeField] private GameMode gameMode = GameMode.Host; // Host, Client, Server 선택
    [SerializeField] private string roomName = "DevTestRoom"; // 테스트용 방 이름
    [SerializeField] private string playerNickname = "DevPlayer"; // 테스트용 닉네임
    
    [Header("🎮 Network Components")]
    [SerializeField] private NetworkRunnerController networkRunnerControllerPrefab; // NetworkRunner 프리팹
    [SerializeField] private GameObject globalManagersPrefab; // GlobalManagers 프리팹 (필요한 경우)
    
    [Header("👤 Player Settings")]
    [SerializeField] private NetworkObject playerPrefab; // 소환할 플레이어 프리팹
    [SerializeField] private Vector3 spawnPosition = Vector3.zero; // 플레이어 소환 위치
    [SerializeField] private bool autoSpawnPlayer = true; // 플레이어 자동 소환 여부
    
    [Header("⏰ Timing")]
    [SerializeField] private float startDelay = 1f; // 시작 전 대기 시간 (초)
    [SerializeField] private float playerSpawnDelay = 2f; // 플레이어 소환 대기 시간 (초)
    
    private NetworkRunnerController networkController;
    private bool hasStarted = false;

    private void Start()
    {
        // 자동 시작이 활성화되어 있으면 게임 시작
        if (enableAutoStart && !hasStarted)
        {
            StartCoroutine(AutoStartGame());
        }
    }
    
    // 🚀 자동 게임 시작 코루틴
    private IEnumerator AutoStartGame()
    {
        hasStarted = true;
        
        Debug.Log($"🛠️ [DevAutoStarter] {startDelay}초 후 자동 시작...");
        yield return new WaitForSeconds(startDelay);
        
        // GlobalManagers가 없으면 생성
        SetupGlobalManagers();
        
        // NetworkRunnerController 설정
        SetupNetworkController();
        
        // 게임 시작 (현재 씬에서 바로 테스트하도록 씬 이동 건너뛰기)
        Debug.Log($"🛠️ [DevAutoStarter] {gameMode} 모드로 방 '{roomName}' 시작! (현재 씬에서 테스트)");
        networkController.SetPlayerNickname(playerNickname);
        networkController.StartGame(gameMode, roomName, true); // 씬 이동 건너뛰기
        
        // 플레이어 자동 소환이 활성화되어 있으면 플레이어 소환
        if (autoSpawnPlayer && playerPrefab != null)
        {
            StartCoroutine(AutoSpawnPlayer());
        }
    }
    
    // 👤 자동 플레이어 소환 코루틴
    private IEnumerator AutoSpawnPlayer()
    {
        Debug.Log($"🛠️ [DevAutoStarter] {playerSpawnDelay}초 후 플레이어 소환...");
        yield return new WaitForSeconds(playerSpawnDelay);
        
        // NetworkRunner가 활성화되고 플레이어가 준비될 때까지 대기
        NetworkRunner runner = null;
        int waitCount = 0;
        while (runner == null || (!runner.IsServer && !runner.IsConnectedToServer) || runner.LocalPlayer == null)
        {
            runner = FindObjectOfType<NetworkRunner>();
            waitCount++;
            
            if (runner == null)
            {
                Debug.Log($"🛠️ [DevAutoStarter] NetworkRunner 찾는 중... ({waitCount})");
            }
            else
            {
                bool isReady = (runner.IsServer || runner.IsConnectedToServer) && runner.LocalPlayer != null;
                Debug.Log($"🛠️ [DevAutoStarter] 네트워크 준비 대기 중... IsServer: {runner.IsServer}, IsConnected: {runner.IsConnectedToServer}, LocalPlayer: {runner.LocalPlayer}, Ready: {isReady} ({waitCount})");
            }
            
            yield return new WaitForSeconds(0.1f);
            
            // 무한 대기 방지 (30초 후 포기)
            if (waitCount > 300)
            {
                Debug.LogError("🛠️ [DevAutoStarter] NetworkRunner 연결 대기 시간 초과!");
                yield break;
            }
        }
        
        Debug.Log($"🛠️ [DevAutoStarter] NetworkRunner 연결 완료! 플레이어 소환 시작...");
        
        // 플레이어 소환
        SpawnPlayer(runner);
    }
    
    // 👤 플레이어 소환 메서드
    private void SpawnPlayer(NetworkRunner runner)
    {
        Debug.Log($"🛠️ [DevAutoStarter] SpawnPlayer 메서드 시작");
        Debug.Log($"🛠️ [DevAutoStarter] runner: {runner}, playerPrefab: {playerPrefab}");
        
        if (runner == null)
        {
            Debug.LogError("🛠️ [DevAutoStarter] 플레이어 소환 실패: NetworkRunner가 null");
            return;
        }
        
        if (playerPrefab == null)
        {
            Debug.LogError("🛠️ [DevAutoStarter] 플레이어 소환 실패: PlayerPrefab이 null - Inspector에서 Player Prefab을 설정해주세요!");
            return;
        }
        
        Debug.Log($"🛠️ [DevAutoStarter] NetworkRunner 상태 - IsServer: {runner.IsServer}, IsClient: {runner.IsClient}, LocalPlayer: {runner.LocalPlayer}");
        
        // Host 모드거나 Server에서만 플레이어 소환 가능
        // 참고: Host 모드에서는 IsServer가 false일 수도 있지만 HasStateAuthority로 권한 확인 가능
        bool canSpawn = runner.IsServer || (gameMode == GameMode.Host);
        
        if (!canSpawn)
        {
            Debug.LogWarning($"🛠️ [DevAutoStarter] 플레이어 소환 실패: 서버 권한 없음 (GameMode: {gameMode}, IsServer: {runner.IsServer})");
            return;
        }
        
        Debug.Log($"🛠️ [DevAutoStarter] 플레이어 소환 권한 확인됨 (GameMode: {gameMode})");
        
        try
        {
            // 스폰 위치 설정 (기본값이 Vector3.zero면 원점에서 소환)
            Vector3 finalSpawnPosition = spawnPosition;
            
            Debug.Log($"🛠️ [DevAutoStarter] 플레이어 소환 중... 위치: {finalSpawnPosition}, LocalPlayer: {runner.LocalPlayer}");
            
            // 플레이어 소환
            var player = runner.Spawn(
                playerPrefab, 
                finalSpawnPosition, 
                Quaternion.identity, 
                runner.LocalPlayer
            );
            
            if (player != null)
            {
                // 플레이어 객체와 PlayerRef 연결
                runner.SetPlayerObject(runner.LocalPlayer, player);
                
                Debug.Log($"🛠️ [DevAutoStarter] 플레이어 소환 완료! ID: {player.Id}, 이름: {player.name}, 위치: {player.transform.position}");
                Debug.Log($"🛠️ [DevAutoStarter] 하이어라키에서 '{player.name}' 오브젝트를 확인해보세요!");
            }
            else
            {
                Debug.LogError("🛠️ [DevAutoStarter] runner.Spawn()이 null을 반환했습니다!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🛠️ [DevAutoStarter] 플레이어 소환 오류: {e.Message}");
            Debug.LogError($"🛠️ [DevAutoStarter] 스택 트레이스: {e.StackTrace}");
        }
    }
    
    // 🌐 GlobalManagers 설정
    private void SetupGlobalManagers()
    {
        // GlobalManagers가 이미 있는지 확인
        if (GlobalManagers.Instance == null && globalManagersPrefab != null)
        {
            Debug.Log("🛠️ [DevAutoStarter] GlobalManagers 생성 중...");
            Instantiate(globalManagersPrefab);
        }
        else if (GlobalManagers.Instance != null)
        {
            Debug.Log("🛠️ [DevAutoStarter] 기존 GlobalManagers 사용");
        }
    }
    
    // 🎮 NetworkRunnerController 설정
    private void SetupNetworkController()
    {
        // 기존 NetworkRunnerController 찾기
        networkController = FindObjectOfType<NetworkRunnerController>();
        
        // 없으면 새로 생성
        if (networkController == null && networkRunnerControllerPrefab != null)
        {
            Debug.Log("🛠️ [DevAutoStarter] NetworkRunnerController 생성 중...");
            var instance = Instantiate(networkRunnerControllerPrefab);
            networkController = instance.GetComponent<NetworkRunnerController>();
        }
        
        // GlobalManagers에 등록
        if (GlobalManagers.Instance != null && networkController != null)
        {
            // 리플렉션을 통해 NetworkRunnerController 설정 (private set 우회)
            var field = typeof(GlobalManagers).GetField("<NetworkRunnerController>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(GlobalManagers.Instance, networkController);
        }
    }
    
    // 🎛️ 에디터 버튼: Inspector에서 수동으로 시작
    [ContextMenu("🚀 Start Game Now")]
    private void StartGameNow()
    {
        if (!hasStarted && Application.isPlaying)
        {
            StartCoroutine(AutoStartGame());
        }
    }
    
    // 🛑 에디터 버튼: 게임 정지
    [ContextMenu("🛑 Stop Game")]
    private void StopGame()
    {
        if (networkController != null)
        {
            networkController.ShutDownRunner();
        }
    }
    
    // 📝 에디터에서 도움말 표시
    private void OnValidate()
    {
        if (enableAutoStart)
        {
            Debug.Log("🛠️ [DevAutoStarter] 자동 시작 활성화됨. Play 버튼으로 바로 테스트 가능!");
        }
    }
} 