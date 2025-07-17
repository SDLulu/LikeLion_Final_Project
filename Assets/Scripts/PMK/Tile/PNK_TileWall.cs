using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PNK_TileWall : MonoBehaviour
{
    [SerializeField] private LayerMask whatisPlatform;
    [SerializeField] private TileBase ruleTile;

    [SerializeField] private GameObject wallItem; // 타일 안에 생성될 아이템 오브젝트 (돈 아이템 등)

    private Tilemap tilemap;
    private bool placedTile = false;



    private void OnEnable()
    {
        tilemap = GameObject.Find("RockMap")?.GetComponent<Tilemap>();
        StartCoroutine(DelayedTilePlacement());
    }

    private IEnumerator DelayedTilePlacement()
    {
        yield return null; // 한 프레임 대기
        TryPlaceTileIfEmpty();
    }

    // 현재 위치에 타일이 없으면 룰 타일을 배치합니다.
    private void TryPlaceTileIfEmpty()
    {
        Vector2 pos = transform.position;

        Collider2D hits = Physics2D.OverlapCircle(pos, 0.01f, whatisPlatform);
        if (hits == null && !placedTile)
        {
            Vector3Int cellPos = tilemap.WorldToCell(pos);

            if (tilemap.GetTile(cellPos) == null)
            {
                // 타일 설치
                tilemap.SetTile(cellPos, ruleTile);
                Physics2D.SyncTransforms(); // 물리 최신화
                placedTile = true;

                PMK_TileRogic.Instance.Create_TileItem(cellPos);

                Destroy(gameObject); // 현재 아이템 제거
            }
        }
    }


    private void OnTriggerStay2D(Collider2D collision)
    {
        // 실제 충돌 지점 계산
        Vector2 contactPoint = collision.ClosestPoint(transform.position);

        // 플랫폼 레이어에 해당하는 콜라이더 탐색
        Collider2D hit = Physics2D.OverlapCircle(contactPoint, 0.01f, whatisPlatform);
        if (hit != null)
        {
            PMK_Bricks bricks = hit.GetComponent<PMK_Bricks>();
            if (bricks != null)
            {
                bricks.MakeDot(contactPoint); // 올바른 월드 위치 전달

                if (collision.CompareTag("Tileitem"))
                {
                    Destroy(collision.gameObject);
                }
                Destroy(gameObject); // 벽 제거
            }
        }
    }
}
