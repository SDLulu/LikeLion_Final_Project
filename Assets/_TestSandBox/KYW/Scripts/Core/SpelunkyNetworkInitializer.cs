using Fusion;
using UnityEngine;

// 플레이어 네트워크 설정 초기화 유틸리티
public static class SpelunkyNetworkInitializer
{
    // 네트워크 설정 초기화
    public static void InitializeNetworkSettings(SpelunkyPlayerController player)
    {
        player.Runner.SetIsSimulated(player.Object, true);
        ConfigureNetworkPhysics(player);
        if (player.Object.HasInputAuthority)
        {
            Debug.Log("🌐 로컬 플레이어 - Input Authority 설정 완료");
        }
        else
        {
            player.Object.RenderSource = RenderSource.Interpolated;
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
        var existingCamera = Object.FindObjectOfType<Cinemachine.CinemachineVirtualCamera>();
        if (existingCamera != null)
        {
            existingCamera.Follow = player.transform;
            existingCamera.LookAt = player.transform;
            Debug.Log("📷 기존 시네머신 카메라를 로컬 플레이어에게 연결");
        }
        else
        {
            var cameraGO = new GameObject("Player Virtual Camera");
            var virtualCamera = cameraGO.AddComponent<Cinemachine.CinemachineVirtualCamera>();
            virtualCamera.Follow = player.transform;
            virtualCamera.LookAt = player.transform;
            try
            {
                var transposer = virtualCamera.GetCinemachineComponent<Cinemachine.CinemachineTransposer>();
                if (transposer != null)
                {
                    transposer.m_FollowOffset = new Vector3(0, 2, -10);
                }
                virtualCamera.m_Lens.FieldOfView = 60f;
                virtualCamera.m_Lens.OrthographicSize = 5f;
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

    // 네트워크 물리 설정 (떨림 방지)
    private static void ConfigureNetworkPhysics(SpelunkyPlayerController player)
    {
        var rigidbody = player.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (player.Object.HasInputAuthority)
                Debug.Log("🎯 로컬 플레이어 Rigidbody2D를 Interpolate로 설정 (떨림 방지)");
            else
                Debug.Log("🎯 원격 플레이어 Rigidbody2D를 Interpolate로 설정");
        }
        Debug.Log("⚠️ 떨림 방지를 위한 프리팹 설정 확인:");
        Debug.Log("   1. NetworkObject의 'Is Master Client Only' 체크 해제");
        Debug.Log("   2. NetworkRigidbody2D의 'Interpolation Target'을 'Render'로 설정");
        Debug.Log("   3. Transform의 'Interpolation Data Source'를 'Predicted'로 설정");
    }
} 