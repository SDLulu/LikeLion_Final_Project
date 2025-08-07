using LMCore;
using Unity.Cinemachine;
using UnityEngine;

public class CameraMover : BaseManager<CameraMover>
{
    [field: SerializeField] public CinemachineCamera PlayerCamera;
    [field: SerializeField] public CinemachineCamera UICamera;
    [field: SerializeField] public CinemachineBrain CinemachineBrain;
    [field: SerializeField] public Camera MainCam;

    public void SetSkyBoxEnv()
    {
        MainCam.clearFlags = CameraClearFlags.Skybox;
    }

    public void SetSolidColorEnv()
    {
        MainCam.clearFlags = CameraClearFlags.SolidColor;
    }
}
