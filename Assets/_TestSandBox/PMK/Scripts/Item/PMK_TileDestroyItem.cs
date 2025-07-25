using System.Collections;
using Fusion;
using UnityEngine;

public class PMK_TileDestroyItem : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] protected GameObject destroyArea; // 파괴 영역 오브젝트
    [SerializeField] protected CircleCollider2D destroyCollider2D; // 파괴 범위 콜라이더
    [SerializeField] protected LayerMask destroyLayer; // 파괴할 타일 레이어

    [Header("파괴 설정")]
    [SerializeField] protected float deleteRadius = 3f; // 파괴 반경
    [SerializeField] protected float delayBeforeBoom = 3f; // 폭발 전 대기 시간

    protected virtual void Start()
    {
        destroyCollider2D.radius = deleteRadius;
        StartCoroutine(Booooom());
    }

    protected virtual IEnumerator Booooom()
    {
        yield return new WaitForSeconds(delayBeforeBoom);

        if (destroyArea != null)
            destroyArea.SetActive(true);

        DestroyArea();

        Destroy(gameObject);
    }

    protected virtual void DestroyArea()
    {
        int radiusInt = Mathf.RoundToInt(destroyCollider2D.radius);
        for (int i = -radiusInt; i <= radiusInt; i++)
        {
            for (int j = -radiusInt; j <= radiusInt; j++)
            {
                Vector3 checkCellPos = new Vector3(transform.position.x + i, transform.position.y + j, 0);
                float distance = Vector2.Distance(transform.position, checkCellPos) - 0.001f;

                if (distance <= radiusInt)
                {
                    // 타일 파괴
                    Collider2D overCollider2d = Physics2D.OverlapCircle(checkCellPos, 0.01f, destroyLayer);
                    if (overCollider2d != null)
                    {
                        var tileLogic = overCollider2d.GetComponent<PMK_TileRogic>();
                        if (tileLogic != null)
                            tileLogic.DestoryTile(checkCellPos);
                    }

                    // 타일 아이템 파괴
                    Collider2D[] hitObjects = Physics2D.OverlapCircleAll(checkCellPos, 0.01f, destroyLayer);
                    foreach (var col in hitObjects)
                    {
                        if (col.CompareTag("Tileitem"))
                        {
                            var item = col.GetComponent<PMK_TileItem>();
                            if (item != null)
                                item.DestroyItem();
                        }
                    }
                }
            }
        }
    }
}
