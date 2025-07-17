using System.Collections;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class PMK_TileDestroyItem : MonoBehaviour
{
    [SerializeField] private GameObject destroyArea;
    [SerializeField] private LayerMask whatisPlatform;
    [SerializeField] private LayerMask whatisTileitem;
    [SerializeField] private CircleCollider2D deleteCollider2D;

    [Header("폭발 범위")]
    [SerializeField] private float deleteRadius = 3f;

    private void Start()
    {
        deleteCollider2D.radius = deleteRadius;
        StartCoroutine(Booooom());
    }

    private IEnumerator Booooom()
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
                    Collider2D overCollider2d = Physics2D.OverlapCircle(checkCellPos, 0.01f, whatisPlatform);
                    if (overCollider2d != null)
                    {
                        overCollider2d.transform.GetComponent<PMK_Bricks>().MakeDot(checkCellPos);
                    }


                    // 타일 아이템 제거
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
