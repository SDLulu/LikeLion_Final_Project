using LMCore;
using Unity.Cinemachine;
using UnityEngine;

public class CameraMover : BaseManager<CameraMover>
{
    [field: SerializeField] public CinemachineCamera PlayerCamera;
    [field: SerializeField] public CinemachineCamera UICamera;
    [field: SerializeField] public CinemachineBrain CinemachineBrain;
    [field: SerializeField] public Camera MainCam;

    private bool _offsetCached;
    private Vector3 _playerBaseOffset;
    private Vector3 _uiBaseOffset;

    private void CacheBaseOffsetsIfNeeded()
    {
        if (_offsetCached) return;

        if (PlayerCamera != null)
        {
            var comp = PlayerCamera.GetComponent<CinemachinePositionComposer>();
            if (comp != null)
            {
                _playerBaseOffset = comp.TargetOffset;
            }
        }

        if (UICamera != null)
        {
            var comp = UICamera.GetComponent<CinemachinePositionComposer>();
            if (comp != null)
            {
                _uiBaseOffset = comp.TargetOffset;
            }
        }

        _offsetCached = true;
    }

    // Y 오프셋을 적용(플레이어/UI 카메라 모두) - 양수=위로, 음수=아래로
    public void ApplyYOffset(float yOffset)
    {
        CacheBaseOffsetsIfNeeded();

        if (PlayerCamera != null)
        {
            var comp = PlayerCamera.GetComponent<CinemachinePositionComposer>();
            if (comp != null)
            {
                var o = _playerBaseOffset;
                comp.TargetOffset = new Vector3(o.x, o.y + yOffset, o.z);
            }
        }

        if (UICamera != null)
        {
            var comp = UICamera.GetComponent<CinemachinePositionComposer>();
            if (comp != null)
            {
                var o = _uiBaseOffset;
                comp.TargetOffset = new Vector3(o.x, o.y + yOffset, o.z);
            }
        }
    }

    // 기본 오프셋으로 복원
    public void ResetYOffset()
    {
        CacheBaseOffsetsIfNeeded();

        if (PlayerCamera != null)
        {
            var comp = PlayerCamera.GetComponent<CinemachinePositionComposer>();
            if (comp != null)
            {
                comp.TargetOffset = _playerBaseOffset;
            }
        }

        if (UICamera != null)
        {
            var comp = UICamera.GetComponent<CinemachinePositionComposer>();
            if (comp != null)
            {
                comp.TargetOffset = _uiBaseOffset;
            }
        }
    }

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
