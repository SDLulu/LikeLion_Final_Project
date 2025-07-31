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
        var existingCamera = CameraMover.Inst.PlayerCamera;
        if (existingCamera != null)
        {
            existingCamera.Follow = player.transform;
            existingCamera.LookAt = player.transform;
            Debug.Log("📷 기존 시네머신 카메라를 로컬 플레이어에게 연결");
        }
        else
        {
            var cameraGO = new GameObject("Player Virtual Camera");
            var virtualCamera = cameraGO.AddComponent<CinemachineCamera>();
            virtualCamera.Follow = player.transform;
            virtualCamera.LookAt = player.transform;
            try
            {
                var follow = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachineFollow;
                if (follow != null)
                {
                    follow.FollowOffset = new Vector3(0, 2, -10);
                }
                
                var lens = virtualCamera.Lens;
                lens.FieldOfView = 60f;
                lens.OrthographicSize = 5f;
                virtualCamera.Lens = lens;
                
                Debug.Log("📷 새 시네머신 카메라 생성 및 로컬 플레이어에게 연결");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"📷 카메라 설정 실패: {e.Message}");
            }
        }
    }

    // 로컬 플레이어 UI 초기화
    public static void InitializeLocalPlayerUI(SpelunkyPlayerController player)
    {
        Debug.Log("🎨 로컬 플레이어 UI 초기화 완료");
    }
} 