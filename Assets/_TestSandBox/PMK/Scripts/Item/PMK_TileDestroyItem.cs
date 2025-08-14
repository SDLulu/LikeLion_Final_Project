using System.Collections;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Collections.Unicode;

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
        float radius = destroyCollider2D.radius;
        Vector2 origin = transform.position;

        // 한 셀 간격마다 반복 (0.5f로 샘플링 간격 줄이기 가능)
        float step = 0.5f;

        for (float x = -radius; x <= radius; x += step)
        {
            for (float y = -radius; y <= radius; y += step)
            {
                Vector2 checkPos = origin + new Vector2(x, y);
                float distance = Vector2.Distance(origin, checkPos);

                if (distance <= radius)
                {
                    // 타일 파괴
                    Collider2D tileCol = Physics2D.OverlapPoint(checkPos, destroyLayer);
                    if (tileCol != null)
                    {
                        var tileLogic = tileCol.GetComponent<PMK_TileRPC_Manager>();
                        if (tileLogic != null)
                        {
                            Vector3Int intPos = Vector3Int.FloorToInt(new Vector3(checkPos.x, checkPos.y, 0f));
                            tileLogic.Rpc_DestroyTile(intPos);
                        }
                    }

                    // 타일 아이템 파괴
                    Collider2D[] hitObjects = Physics2D.OverlapPointAll(checkPos, destroyLayer);
                    foreach (var col in hitObjects)
                    {
                        if (col.CompareTag("Tileitem"))
                        {
                            var item = col.GetComponent<PMK_TileItem>();
                            if (item != null)
                            {
                                item.DestroyItem();
                            }
                        }
                        else if (col.CompareTag("BoobDestoryObj"))
                        {
                            Destroy(col.gameObject); // 오브젝트 파괴
                        }
                    }
                }
            }
        }
    }

}
