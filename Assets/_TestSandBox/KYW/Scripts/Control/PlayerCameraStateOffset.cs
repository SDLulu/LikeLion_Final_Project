using Fusion;
using UnityEngine;

// 플레이어 상태(웅크리기/위보기) 지속 시간에 따라 카메라 Y 오프셋을 적용/복원
public class PlayerCameraStateOffset : NetworkBehaviour
{
    [Header("Offset Settings")]
    [SerializeField] private float offsetAmount = 2.0f; // 위보기 시 +, 웅크리기 시 - 적용할 크기
    [SerializeField] private float holdDuration = 1.0f;  // 상태 지속 시간(초)

    private PlayerMovement playerMovement;
    private float duckHeldTime;
    private float lookHeldTime;
    private bool offsetApplied;

    public override void Spawned()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            playerMovement = GetComponentInChildren<PlayerMovement>();
        }
    }

    public override void FixedUpdateNetwork()
    {
        // 입력 권한 클라이언트에서만 카메라 제어
        if (!Object.HasInputAuthority) return;

        bool isDuck = playerMovement != null && playerMovement.IsDucking;
        bool isLook = playerMovement != null && playerMovement.IsLookingUp;

        // 타이머 축적/초기화
        duckHeldTime = isDuck ? duckHeldTime + Runner.DeltaTime : 0f;
        lookHeldTime = isLook ? lookHeldTime + Runner.DeltaTime : 0f;

        // 한쪽만 적용(동시 불가 보장됨). 우선순위: 위보기 > 웅크리기
        if (lookHeldTime >= holdDuration)
        {
            ApplyOffset(+Mathf.Abs(offsetAmount));
        }
        else if (duckHeldTime >= holdDuration)
        {
            ApplyOffset(-Mathf.Abs(offsetAmount));
        }
        else
        {
            ResetOffsetIfNeeded();
        }
    }

    private void ApplyOffset(float y)
    {
        CameraMover.Inst?.ApplyYOffset(y);
        offsetApplied = true;
    }

    private void ResetOffsetIfNeeded()
    {
        if (!offsetApplied) return;
        CameraMover.Inst?.ResetYOffset();
        offsetApplied = false;
    }
}


