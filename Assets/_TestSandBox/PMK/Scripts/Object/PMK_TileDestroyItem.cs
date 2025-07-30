using System.Collections;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class MK_TileDestroyItem : MonoBehaviour
{
    [SerializeField] private GameObject destroyArea; // 파괴 영역 오브젝트
    [SerializeField] private LayerMask whatisPlatform; // 파괴할 타일 레이어
    [SerializeField] private LayerMask whatisTileitem; // 파괴할 타일 아이템 레이어
    [SerializeField] private CircleCollider2D deleteCollider2D; // 파괴 영역의 원형 콜라이더

    [Header("폭탄의 파괴 반경")]
    [SerializeField] private float deleteRadius = 3f; // 파괴 반경

    private void Start()
    {
        deleteCollider2D.radius = deleteRadius;
        StartCoroutine(Booooom());
    }

    private IEnumerator Booooom() // 3초뒤 폭발
    {
        yield return new WaitForSeconds(3f);
        destroyArea.SetActive(true);

        DestroyArea();

        Destroy(gameObject);
    }

    private void DestroyArea()
    {
        int radiusInt = Mathf.RoundToInt(deleteCollider2D.radius);
        for (int i = -radiusInt; i <= radiusInt; i++)
        {
            for (int j = -radiusInt; j <= radiusInt; j++)
            {
                Vector3 checkCellPos = new Vector3(transform.position.x + i, transform.position.y + j, 0);
                float distance = Vector2.Distance(transform.position, checkCellPos) - 0.001f;

                if(distance <= radiusInt)
                {
                    // 타일 파괴
                    Collider2D overCollider2d = Physics2D.OverlapCircle(checkCellPos, 0.01f, whatisPlatform);
                    if (overCollider2d != null)
                    {
                        overCollider2d.transform.GetComponent<PMK_TileRPC_Manager>().Rpc_DestroyTile(checkCellPos);
                    }


                    // 타일 아이템 파괴
                    Collider2D[] hitObjects = Physics2D.OverlapCircleAll(checkCellPos, 0.01f, whatisTileitem);
                    foreach (var col in hitObjects)
                    {
                        if (col.CompareTag("Tileitem"))
                        {
                            col.GetComponent<PMK_TileItem>().DestroyItem();
                        }
                    }
                }
            }
        }
    }
}
