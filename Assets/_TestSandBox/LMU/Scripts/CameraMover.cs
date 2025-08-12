using LMCore;
using Unity.Cinemachine;
using UnityEngine;

public class CameraMover : BaseManager<CameraMover>
{
    [field: SerializeField] public CinemachineCamera PlayerCamera;
    [field: SerializeField] public CinemachineCamera UICamera;
    [field: SerializeField] public CinemachineBrain CinemachineBrain;
    [field: SerializeField] public Camera MainCam;

    // 모든 등록된 시네머신 가상 카메라의 Follow/LookAt을 대상에 맞춥니다
    public void SetFollowAndLookAtForAll(Transform target)
    {
        if (target == null) return;

        if (PlayerCamera != null)
        {
            PlayerCamera.Follow = target;
            PlayerCamera.LookAt = target;
        }

        if (UICamera != null)
        {
            UICamera.Follow = target;
            UICamera.LookAt = target;
        }
    }

    public void SetSkyBoxEnv()
    {
        MainCam.clearFlags = CameraClearFlags.Skybox;
    }

    public void SetSolidColorEnv()
    {
        MainCam.clearFlags = CameraClearFlags.SolidColor;
    }
}
