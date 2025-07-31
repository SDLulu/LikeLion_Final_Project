using LMCore;
using Unity.Cinemachine;
using UnityEngine;

public class CameraMover : BaseManager<CameraMover>
{
    [field: SerializeField] public CinemachineCamera PlayerCamera;
    [field: SerializeField] public CinemachineCamera UICamera;
    [field: SerializeField] public CinemachineBrain CinemachineBrain;

}
