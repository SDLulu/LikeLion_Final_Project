using Fusion;
using UnityEngine;

public class PMK_ArrowTrap : NetworkBehaviour
{
    [SerializeField] private Vector2 arrowDir = Vector2.right; // 화살이 발사되는 방향
    [SerializeField] private float shotSpeed = 40f; // 발사 속도
    [SerializeField] private GameObject launchTrapPrefab; // 발사할 프리팹
    [SerializeField] private Transform launchPoint; // 발사 위치
    [field: SerializeField] public LayerMask targetLayer { get; private set; } // 충돌 레이어 감지

    private bool isTrapActive = true;

    public override void FixedUpdateNetwork()
    {
        // 권한이 없는 오브젝트는 처리하지 않음
        if (!Object.HasStateAuthority || !isTrapActive) return;

        Vector2 origin = launchPoint.position;
        float maxDistance = 10f; // 최대 거리 설정

        // Raycast를 사용하여 충돌 감지
        var hit = Runner.GetPhysicsScene2D().Raycast(origin, arrowDir, maxDistance, targetLayer);

        if (hit.collider != null)
        {
            // 충돌 지점에 빨간색 선을 그려서 디버그
            Debug.DrawLine(origin, hit.point, Color.red, 0.1f);
            Debug.Log("Hit: " + hit.collider.name);

            // 충돌한 오브젝트가 "Ground" 레이어가 아닌 경우에만 트랩 활성화
            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Ground"))
            {
                isTrapActive = false; // 트랩 비활성화

                // 트랩이 활성화되면 launchTrapPrefab을 스폰
                Runner.Spawn(launchTrapPrefab, launchPoint.position, Quaternion.identity, inputAuthority: null,
                    onBeforeSpawned: (runner, obj) =>
                    {
                        var rb = obj.GetComponent<Rigidbody2D>();
                        rb.linearVelocity = arrowDir * shotSpeed; // 화살 속도 설정
                    });
            }
        }
        else
        {
            // 충돌이 없을 경우, 최대 거리까지의 선을 그려서 디버그
            Debug.DrawLine(origin, origin + arrowDir * maxDistance, Color.green, 0.1f);
        }
    }
}
