using UnityEngine;

public class PMK_ArrowTrap : MonoBehaviour
{
    public static PMK_ArrowTrap Instance { get; private set; }

    [Header("화살 발사 설정")]
    [SerializeField] private Vector2 dir = Vector2.right; // 발사 방향
    [SerializeField] private float ShotSpeed = 40f; // 발사 속도

    [SerializeField] private GameObject launchTrapPrefab; // 발사 트랩 프리팹
    [SerializeField] private Transform launchPoint; // 발사 지점

    [field: SerializeField] public LayerMask targetLayer { get; private set; } // 타겟 레이어


    private bool isTrapActive = true; // 트랩 활성화 여부

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        Vector2 origin = transform.position;
        float maxDistance = 10f;

        // 정한 레이어만 검사
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, maxDistance, targetLayer);

        if (hit.collider != null)
        {
            Debug.DrawLine(origin, hit.point, Color.red, 0.1f);
            Debug.Log("Hit: " + hit.collider.name);


            // 레이어가 "Ground"가 아닌 경우 발사후 트렙 비활성화
            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Ground") && isTrapActive)
            {
                isTrapActive = false;
                GameObject trap = Instantiate(launchTrapPrefab, launchPoint.position, Quaternion.identity);
                trap.GetComponent<Rigidbody2D>().linearVelocity = dir * ShotSpeed; // 발사 속도 설정
            }
        }
        else
        {
            // �浹 ������ �ִ� �Ÿ����� ������
            Debug.DrawLine(origin, origin + dir * maxDistance, Color.green, 0.1f);
        }
    }


}
