using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Cinemachine;

// 🛠️ 개발용 자동 네트워크 시작 클래스
// NetworkRunner를 관리하여 현재 씬에서 바로 테스트 가능한 환경 제공
// 
// 📋 책임 분리:
// - DevAutoStarter: 네트워크 시작 및 플레이어 소환만 담당
// - SpelunkyPlayerController: 소환 후 모든 설정 (카메라, 네트워크 등) 담당
//
// 🎯 동작 방식:
// 1. 클라이언트 모드로 기존 방 참여 시도
// 2. 실패하면 호스트 모드로 새 방 생성
// 3. 플레이어 소환 (호스트만 권한 보유)
// 4. 각 플레이어는 SpelunkyPlayerController에서 자동 초기화
public class DevAutoStarter : MonoBehaviour
{
    [Header("🛠️ 자동 테스트 설정")]
    [SerializeField] private bool enableAutoStart = true; // 자동 시작 활성화
    [SerializeField] private string roomName = "DevTestRoom"; // 테스트용 방 이름
    [SerializeField] private string playerNickname = "DevPlayer"; // 테스트용 닉네임
    
    [Header("🌐 네트워크 설정")]
    [SerializeField] private NetworkRunner networkRunnerPrefab; // NetworkRunner 프리팹 (필수 컴포넌트들 포함)
    
    [Header("👤 플레이어 설정")]
    [SerializeField] private NetworkPrefabRef playerNetworkPrefab = NetworkPrefabRef.Empty; // 소환할 플레이어 프리팹 (표준 패턴)
    [SerializeField] private Vector3 spawnPosition = Vector3.zero; // 플레이어 소환 위치
    [SerializeField] private bool autoSpawnPlayer = true; // 플레이어 자동 소환 여부
    
    [Header("📷 카메라 설정")]
    [SerializeField] private bool attachCameraToPlayer = true; // 플레이어에게 카메라 붙이기 (SpelunkyPlayerController에서 처리)
    
    [Header("⏰ 타이밍")]
    [SerializeField] private float startDelay = 1f; // 시작 전 대기 시간
    [SerializeField] private float playerSpawnDelay = 1f; // 플레이어 소환 대기 시간
    
    private NetworkRunner networkRunner;
    private bool hasStarted = false;
    private bool isPlayerSpawned = false;
    private GameMode currentGameMode = GameMode.Client;

    private void Start()
    {
        if (enableAutoStart && !hasStarted)
        {
            StartCoroutine(AutoStartNetwork());
        }
    }
    
    private void Update()
    {
        // 네트워크 연결 상태 확인 및 플레이어 자동 소환
        if (networkRunner != null && networkRunner.IsRunning && autoSpawnPlayer)
        {
            // 호스트 모드에서 모든 플레이어 소환
            if (currentGameMode == GameMode.Host)
            {
                SpawnAllPlayers();
            }
            // 클라이언트 모드에서 서버에 연결되었으면 서버에서 소환 (이미 서버에서 처리됨)
            else if (currentGameMode == GameMode.Client && networkRunner.IsConnectedToServer)
            {
                // 클라이언트는 서버에서 자동으로 소환되므로 여기서는 확인만
                CheckIfPlayerSpawned();
            }
        }
    }
    
    // 플레이어가 소환되었는지 확인 (클라이언트용)
    private void CheckIfPlayerSpawned()
    {
        if (networkRunner != null && networkRunner.LocalPlayer != null)
        {
            if (networkRunner.TryGetPlayerObject(networkRunner.LocalPlayer, out var playerObject))
            {
                if (playerObject != null && !isPlayerSpawned)
                {
                    isPlayerSpawned = true;
                    
                    // 카메라 설정은 이제 SpelunkyPlayerController에서 자동으로 처리됨
                    
                    Debug.Log($"🛠️ [DevAutoStarter] 클라이언트 플레이어 확인 완료! 위치: {playerObject.transform.position}, HasInputAuthority: {playerObject.HasInputAuthority}");
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        // 안전한 정리
        if (networkRunner != null && networkRunner.IsRunning)
        {
            networkRunner.Shutdown();
        }
    }
    
    // 🚀 자동 네트워크 시작 코루틴
    private IEnumerator AutoStartNetwork()
    {
        hasStarted = true;
        
        Debug.Log($"🛠️ [DevAutoStarter] {startDelay}초 후 자동 네트워크 시작...");
        yield return new WaitForSeconds(startDelay);
        
        // NetworkRunner 생성 및 설정
        SetupNetworkRunner();
        
        // 먼저 클라이언트 모드로 기존 방 참여 시도
        Debug.Log($"🛠️ [DevAutoStarter] 기존 방 '{roomName}' 참여 시도...");
        bool joinSuccess = false;
        yield return StartCoroutine(TryJoinRoom((success) => joinSuccess = success));
        
        if (!joinSuccess)
        {
            // 방 참여 실패시 호스트 모드로 새 방 생성
            Debug.Log($"🛠️ [DevAutoStarter] 방 참여 실패, 호스트 모드로 새 방 생성...");
            yield return StartCoroutine(StartHostMode());
        }
        else
        {
            Debug.Log($"🛠️ [DevAutoStarter] 클라이언트 모드로 연결 완료! 서버에서 플레이어 소환 대기 중...");
        }
    }
    
    // 🎮 NetworkRunner 설정 (프리팹 사용으로 모든 필수 컴포넌트 포함)
    private void SetupNetworkRunner()
    {
        // 🎯 Inspector에서 설정되지 않은 경우 Resources에서 자동 로드
        if (networkRunnerPrefab == null)
        {
            networkRunnerPrefab = Resources.Load<NetworkRunner>("Prefabs/NetworkRunner");
            if (networkRunnerPrefab == null)
            {
                Debug.LogError("🛠️ [DevAutoStarter] NetworkRunner 프리팹을 찾을 수 없습니다! (Resources/Prefabs/NetworkRunner)");
                return;
            }
            Debug.Log("🛠️ [DevAutoStarter] Resources에서 NetworkRunner 프리팹 자동 로드 완료");
        }
        
        // 🎯 프리팹 인스턴스화 (NetworkSceneManager, NetworkObjectProvider 등 필수 컴포넌트 포함)
        var runnerGO = Instantiate(networkRunnerPrefab.gameObject);
        networkRunner = runnerGO.GetComponent<NetworkRunner>();
        runnerGO.transform.SetParent(transform);
        
        Debug.Log("🛠️ [DevAutoStarter] NetworkRunner 프리팹 인스턴스화 완료 (모든 필수 컴포넌트 포함)");
        Debug.Log($"🔧 [DevAutoStarter] 포함된 컴포넌트: NetworkRunner, NetworkSceneManager, NetworkObjectProvider");
    }
    
    // 🔍 기존 방 참여 시도
    private IEnumerator TryJoinRoom(System.Action<bool> onComplete)
    {
        currentGameMode = GameMode.Client;
        
        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = roomName,
            Scene = null, // 현재 씬 사용
            PlayerCount = 4 // 최대 플레이어 수
        };
        
        var startTask = networkRunner.StartGame(startGameArgs);
        
        // 연결 시도 대기 (최대 5초)
        float waitTime = 0f;
        while (!startTask.IsCompleted && waitTime < 5f)
        {
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }
        
        if (startTask.IsCompleted && startTask.Result.Ok)
        {
            Debug.Log($"🛠️ [DevAutoStarter] 클라이언트 모드로 방 '{roomName}' 참여 성공!");
            onComplete?.Invoke(true);
        }
        else
        {
            Debug.Log($"🛠️ [DevAutoStarter] 클라이언트 모드 방 참여 실패");
            onComplete?.Invoke(false);
        }
    }
    
    // 🏠 호스트 모드로 방 생성
    private IEnumerator StartHostMode()
    {
        currentGameMode = GameMode.Host;
        
        // 이전 NetworkRunner 정리 및 새로운 NetworkRunner 생성
        if (networkRunner != null)
        {
            if (networkRunner.IsRunning)
            {
                networkRunner.Shutdown();
                yield return new WaitForSeconds(0.5f); // 정리 대기
            }
            
            // 기존 NetworkRunner 제거
            if (networkRunner.gameObject != null)
            {
                DestroyImmediate(networkRunner.gameObject);
            }
        }
        
        // 새로운 NetworkRunner 생성
        SetupNetworkRunner();
        
        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            PlayerCount = 4
        };
        
        var startTask = networkRunner.StartGame(startGameArgs);
        
        // 연결 완료 대기
        yield return new WaitUntil(() => startTask.IsCompleted);
        
        if (startTask.Result.Ok)
        {
            Debug.Log($"🛠️ [DevAutoStarter] 호스트 모드로 방 '{roomName}' 생성 성공!");
            
            // NetworkRunner 시작 후 수동으로 씬 로드
            networkRunner.LoadScene(SceneRef.FromIndex(4)); // DebugRoom.unity 로드
            
            yield return new WaitForSeconds(playerSpawnDelay);
            // SpawnAllPlayers는 Update에서 지속적으로 호출되므로 여기서는 제거
        }
        else
        {
            Debug.LogError($"🛠️ [DevAutoStarter] 호스트 모드 시작 실패");
        }
    }
    
    // 👤 플레이어 소환 요청 (클라이언트용) - 제거됨
    // 클라이언트는 직접 소환할 수 없으므로 서버에서 자동으로 처리
    
    // 👤 호스트 모드에서 모든 플레이어 소환
    private void SpawnAllPlayers()
    {
        if (!autoSpawnPlayer || playerNetworkPrefab == NetworkPrefabRef.Empty || networkRunner == null)
            return;
            
        // 호스트 모드에서만 플레이어 소환
        if (currentGameMode != GameMode.Host)
            return;
            
        // 모든 플레이어 확인하고 소환되지 않은 플레이어 소환
        foreach (var player in networkRunner.ActivePlayers)
        {
            if (!networkRunner.TryGetPlayerObject(player, out var playerObject) || playerObject == null)
            {
                SpawnPlayerForRef(player);
            }
        }
    }
    
    // 특정 PlayerRef에 대한 플레이어 소환 (표준 Fusion 패턴 적용)
    private void SpawnPlayerForRef(PlayerRef playerRef)
    {
        if (playerNetworkPrefab == NetworkPrefabRef.Empty || networkRunner == null)
            return;
            
        Debug.Log($"🛠️ [DevAutoStarter] 플레이어 소환 시작... PlayerRef: {playerRef}, IsLocalPlayer: {playerRef == networkRunner.LocalPlayer}");
        
        try
        {
            // 🎯 핵심: NetworkPrefabRef 사용으로 Fusion 최적화 적용
            var player = networkRunner.Spawn(
                playerNetworkPrefab, 
                spawnPosition, 
                Quaternion.identity, 
                playerRef
            );
            
            if (player != null)
            {
                // 🔥 중요: SetPlayerObject로 권한 연결 (표준 패턴)
                networkRunner.SetPlayerObject(playerRef, player);
                
                // Input Authority 상태 확인
                bool hasInputAuthority = player.HasInputAuthority;
                bool isLocalPlayer = playerRef == networkRunner.LocalPlayer;
                
                Debug.Log($"🛠️ [DevAutoStarter] 플레이어 소환 완료! PlayerRef: {playerRef}, 위치: {player.transform.position}");
                Debug.Log($"🛠️ [DevAutoStarter] Input Authority: {hasInputAuthority}, IsLocalPlayer: {isLocalPlayer}");
                Debug.Log($"🎯 [DevAutoStarter] NetworkPrefabRef 사용으로 Fusion 최적화 적용됨");
                
                // 로컬 플레이어인 경우 소환 상태 업데이트
                if (isLocalPlayer)
                {
                    isPlayerSpawned = true;
                }
            }
            else
            {
                Debug.LogError($"🛠️ [DevAutoStarter] 플레이어 소환 실패: Spawn이 null 반환 (PlayerRef: {playerRef})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🛠️ [DevAutoStarter] 플레이어 소환 오류: {e.Message}");
        }
    }
    
    // 📷 플레이어에게 시네머신 카메라 설정 (제거됨 - SpelunkyPlayerController에서 처리)
    // 카메라 설정은 이제 SpelunkyPlayerController.SetupCameraForLocalPlayer()에서 처리됩니다.
    
    // 🎛️ 에디터 컨텍스트 메뉴: 수동 시작
    [ContextMenu("🚀 Start Network Now")]
    private void StartNetworkNow()
    {
        if (!hasStarted && Application.isPlaying)
        {
            StartCoroutine(AutoStartNetwork());
        }
    }
    
    // 🛑 에디터 컨텍스트 메뉴: 네트워크 정지
    [ContextMenu("🛑 Stop Network")]
    private void StopNetwork()
    {
        if (networkRunner != null)
        {
            if (networkRunner.IsRunning)
            {
                networkRunner.Shutdown();
            }
            
            // NetworkRunner GameObject 제거
            if (networkRunner.gameObject != null)
            {
                DestroyImmediate(networkRunner.gameObject);
            }
            
            networkRunner = null;
        }
        
        // 상태 초기화
        hasStarted = false;
        isPlayerSpawned = false;
        
        Debug.Log("🛠️ [DevAutoStarter] 네트워크 정지 완료");
    }
    
    // 🔄 에디터 컨텍스트 메뉴: 플레이어 재소환
    [ContextMenu("👤 Respawn Player")]
    private void RespawnPlayer()
    {
        if (Application.isPlaying && networkRunner != null)
        {
            // 기존 플레이어 제거
            if (networkRunner.TryGetPlayerObject(networkRunner.LocalPlayer, out var playerObject))
            {
                networkRunner.Despawn(playerObject);
            }
            
            isPlayerSpawned = false;
            
            // 호스트 모드에서만 재소환
            if (currentGameMode == GameMode.Host)
            {
                SpawnAllPlayers();
            }
            else
            {
                Debug.Log("🛠️ [DevAutoStarter] 클라이언트 모드에서는 서버에서 자동으로 재소환됩니다.");
            }
        }
    }
    
    // 📷 에디터 컨텍스트 메뉴: 카메라 재설정 (제거됨)
    // 카메라 설정은 이제 SpelunkyPlayerController에서 자동으로 처리됩니다.
} 