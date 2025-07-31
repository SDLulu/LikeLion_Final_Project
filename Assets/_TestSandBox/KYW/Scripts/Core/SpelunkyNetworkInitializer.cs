using Fusion;
using UnityEngine;
using Unity.Cinemachine;

// 플레이어 네트워크 설정 초기화 유틸리티
// 플레이어 프리팹 Spwaned시 호출해줄것들
public static class SpelunkyNetworkInitializer
{
    // 네트워크 설정 초기화
    public static void InitializeNetworkSettings(SpelunkyPlayerController player)
    {
        player.Runner.SetIsSimulated(player.Object, true);
        if (player.Object.HasInputAuthority)
        {
            Debug.Log("🌐 로컬 플레이어 - Input Authority 설정 완료");
        }
        else
        {
            player.Object.RenderSource = RenderSource.Interpolated;
            player.Object.ForceRemoteRenderTimeframe = true;
            Debug.Log("🌐 원격 플레이어 - RenderSource를 Interpolated로 설정");
        }
        Debug.Log($"🌐 네트워크 설정 완료 - HasInputAuthority: {player.Object.HasInputAuthority}");
    }

    // 플레이어 타입별 초기화
    public static void InitializePlayerType(SpelunkyPlayerController player)
    {
        if (player.Object.HasInputAuthority)
        {
            InitializeLocalPlayer(player);
        }
        else
        {
            InitializeRemotePlayer(player);
        }
    }

    // 로컬 플레이어 초기화
    public static void InitializeLocalPlayer(SpelunkyPlayerController player)
    {
        SetupCameraForLocalPlayer(player);
        InitializeLocalPlayerUI(player);
        Debug.Log("🏠 로컬 플레이어 초기화 완료");
    }

    // 원격 플레이어 초기화
    public static void InitializeRemotePlayer(SpelunkyPlayerController player)
    {
        Debug.Log("🌍 원격 플레이어 초기화 완료");
    }

    // 로컬 플레이어용 카메라 설정
    public static void SetupCameraForLocalPlayer(SpelunkyPlayerController player)
    {
        // 기존 시네머신 카메라 찾기
        var existingCamera = Object.FindFirstObjectByType<CinemachineCamera>();
        if (existingCamera != null)
        {
            existingCamera.Follow = player.transform;
            existingCamera.LookAt = player.transform;
            Debug.Log("📷 기존 시네머신 카메라를 로컬 플레이어에게 연결");
        }
        else
        {
            // 기존 카메라가 없으면 간단한 시네머신 설정 생성
            CreateSimpleCinemachineSetup(player);
        }
    }

    // 간단한 시네머신 설정 생성 (Unity Cinemachine 3.x의 간단한 API 사용)
    private static void CreateSimpleCinemachineSetup(SpelunkyPlayerController player)
    {
        try
        {
            // 1. Main Camera 확인
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraGO = new GameObject("Main Camera");
                mainCamera = cameraGO.AddComponent<Camera>();
                cameraGO.tag = "MainCamera";
                Debug.Log("📷 Main Camera 생성됨");
            }

            // 2. CinemachineBrain 추가 (없으면)
            var brain = mainCamera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = mainCamera.gameObject.AddComponent<CinemachineBrain>();
                Debug.Log("📷 CinemachineBrain 추가됨");
            }

            // 3. Virtual Camera 생성 (간단하게)
            var virtualCamera = new GameObject("Player Virtual Camera").AddComponent<CinemachineCamera>();
            
            // 4. 기본 설정만 적용
            virtualCamera.Follow = player.transform;
            virtualCamera.LookAt = player.transform;
            virtualCamera.Priority = 10;
            
            // 5. 2D 게임용 렌즈 설정
            var lens = virtualCamera.Lens;
            lens.OrthographicSize = 5f; // 2D 게임용
            virtualCamera.Lens = lens;
            
            Debug.Log("📷 간단한 시네머신 설정 생성 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"📷 시네머신 설정 생성 실패: {e.Message}");
        }
    }

    // 로컬 플레이어 UI 초기화
    public static void InitializeLocalPlayerUI(SpelunkyPlayerController player)
    {
        Debug.Log("🎨 로컬 플레이어 UI 초기화 완료");
    }
} 