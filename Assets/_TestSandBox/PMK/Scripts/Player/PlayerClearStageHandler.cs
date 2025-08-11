using Fusion;
using UnityEngine;

public class PlayerClearStageHandler : NetworkBehaviour
{
    private bool isInDoorTrigger = false;
    private PMK_NextStageDoor door;

    void Update()
    {
        // 문 안에 있을 때만 W 키 눌렀는지 체크
        if (isInDoorTrigger && Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log($"플레이어가 문에 나감");
            // 서버에 RPC로 W 눌렀다고 알림
            RPC_RequestDoorClear();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("NextDoor"))
        {

            door = other.GetComponent<PMK_NextStageDoor>();
            if (door == null)
            {
                return;
            }

            isInDoorTrigger = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("NextDoor"))
        {
            isInDoorTrigger = false;
            door = null;
        }
    }

    // 서버에 W키 눌렀다고 요청하는 RPC (InputAuthority → StateAuthority)
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestDoorClear()
    {
        if (door != null)
        {
            PlayerRef player = Object.InputAuthority;

            if (!door.HasPlayerCleared(player))
            {
                door.MarkPlayerCleared(player); // 서버 내에서 판단 및 실행
            }
        }
    }
}
