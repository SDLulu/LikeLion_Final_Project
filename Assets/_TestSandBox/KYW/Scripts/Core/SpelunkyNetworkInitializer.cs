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
        // ConfigureNetworkPhysics(player);
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
        // 기존 시네머신 카메라 찾기 (CameraMover 또는 씬에 있는 것)
        var existingCamera = CameraMover.Inst?.PlayerCamera;
        if (existingCamera == null)
        {
            // 씬에서 기존 시네머신 카메라 찾기
            existingCamera = Object.FindAnyObjectByType<CinemachineCamera>();
        }
        
        if (existingCamera != null)
        {
            existingCamera.Follow = player.transform;
            existingCamera.LookAt = player.transform;
            Debug.Log("📷 기존 시네머신 카메라를 로컬 플레이어에게 연결");
        }
        else
        {
            // 메인 카메라 확인 및 설정
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraGO = new GameObject("Main Camera");
                mainCamera = cameraGO.AddComponent<Camera>();
                cameraGO.tag = "MainCamera";
                Debug.Log("📷 메인 카메라 생성");
            }
            
            // 메인 카메라를 2D용으로 설정
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 5f;
            
            // 시네머신 브레인 확인 및 설정
            var brain = mainCamera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = mainCamera.gameObject.AddComponent<CinemachineBrain>();
                Debug.Log("📷 시네머신 브레인 추가");
            }
            
            // 새 2D 시네머신 카메라 생성
            var virtualCameraGO = new GameObject("Player 2D Camera");
            var virtualCamera = virtualCameraGO.AddComponent<CinemachineCamera>();
            
            // 기본 2D 카메라 설정
            virtualCamera.Follow = player.transform;
            virtualCamera.LookAt = player.transform;
            virtualCamera.Priority = 10;  // 높은 우선순위
            
            // 2D 카메라 렌즈 설정 (사용자 카메라와 동일하게)
            var lens = virtualCamera.Lens;
            lens.OrthographicSize = 7f;  // 사용자 카메라와 동일한 크기
            virtualCamera.Lens = lens;
            
            // Position Composer 컴포넌트 추가 (사용자 카메라와 동일하게)
            var composer = virtualCameraGO.AddComponent<CinemachinePositionComposer>();
            if (composer != null)
            {
                // 기본 설정만 적용 (API 호환성 문제로 인해)
                Debug.Log("📷 Position Composer 컴포넌트 추가");
            }
            
            // 카메라 위치 설정 (2D 게임에 적합)
            virtualCameraGO.transform.position = new Vector3(0, 0, -10);
            
            // 새로 생성한 카메라를 CameraMover에 할당
            if (CameraMover.Inst != null)
            {
                CameraMover.Inst.PlayerCamera = virtualCamera;
                Debug.Log("📷 새로 생성한 카메라를 CameraMover에 할당");
            }
            
            Debug.Log("📷 새 2D 시네머신 카메라 생성 및 로컬 플레이어에게 연결");
        }
    }

    // 로컬 플레이어 UI 초기화
    public static void InitializeLocalPlayerUI(SpelunkyPlayerController player)
    {
        Debug.Log("🎨 로컬 플레이어 UI 초기화 완료");
    }
} 