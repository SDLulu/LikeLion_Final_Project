using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using static Unity.Collections.Unicode;

public class PMK_NextStageDoor : MonoBehaviour
{
    private HashSet<PlayerRef> clearedPlayers = new();

    private PMK_TileRogic tileRogic => PMK_TileRogic.Instance;


    private void OnTriggerStay2D(Collider2D other)
    {
        if (tileRogic.HasStateAuthority == false) return; // 서버에서만 처리

        if (other.CompareTag("Player") == false) return;

        Debug.Log($"[서버] 플레이어가 문에 접근");
        // InputAuthority를 통해 PlayerRef 얻기
        var netObj = other.GetComponent<NetworkObject>();
        if (netObj == null) return;

        PlayerRef playerRef = netObj.InputAuthority;
    }

    // 플레이어가 문을 통과했을 때 호출되는 함수
    public void MarkPlayerCleared(PlayerRef player)
    {
        StartCoroutine(DelayedNextStage());
    }

    private IEnumerator DelayedNextStage()
    {
        yield return null;
        Debug.Log("모든 플레이어가 문을 통과했습니다. 다음 스테이지로 이동합니다.");
        GameStates.Inst.DelayForceActiveState<GameStageCompletedState>();

        yield return new WaitForSeconds(5f);
        clearedPlayers.Clear(); // 모든 플레이어 문 통과 상태 초기화
    }
}
