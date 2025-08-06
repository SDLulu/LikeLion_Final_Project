using Fusion;
using UnityEngine;

public class PMK_ArrowTrap : MonoBehaviour
{
    [SerializeField] private Vector2 arrowDir = Vector2.left; // 화살이 발사되는 방향
    [SerializeField] private float shotSpeed = 40f; // 발사 속도
    [SerializeField] private Transform launchPoint; // 발사 위치
    [field: SerializeField] public LayerMask targetLayer { get; private set; } // 충돌 레이어 감지

    PMK_TileRPC_Manager tileRPCManager => PMK_TileRPC_Manager.Instance;
    private bool isTrapActive = true;

    public void Update()
    {
        if (!tileRPCManager.HasStateAuthority || !isTrapActive) return;


        Vector2 origin = launchPoint.position;
        float maxDistance = 10f;


        // 화살 발사 방향을 시각적으로 표시
        var hit = Physics2D.Raycast(origin, arrowDir, maxDistance, targetLayer);


        if (hit.collider != null)
        {
            Debug.DrawLine(origin, hit.point, Color.red, 0.1f);

            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Ground"))
            {
                isTrapActive = false;

                tileRPCManager.RPC_SpawnArrow(launchPoint.position, arrowDir.normalized * shotSpeed);
            }
        }
        else
        {
            Debug.DrawLine(origin, origin + arrowDir * maxDistance, Color.green, 0.1f);
        }
    }
}
