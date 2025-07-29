using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PMK_TileDestroyer : MonoBehaviour
{
    [SerializeField] private LayerMask whatisPlatform;
    [SerializeField] private TileBase ruleTile;

    private Tilemap tilemap;



    private void OnEnable()
    {
        tilemap = PMK_TileRogic.Instance.mainTilemap;
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
        if (hits == null)
        {
            Vector3Int cellPos = tilemap.WorldToCell(pos);

            if (tilemap.GetTile(cellPos) == null)
            {
                // 타일 설치
                tilemap.SetTile(cellPos, ruleTile);
                Physics2D.SyncTransforms(); // 물리 최신화

                PMK_TileRogic.Instance.Create_TileItem(cellPos); // 아이템 랜덤 생성

                Destroy(gameObject); // 현재 아이템 제거
            }
        }
    }


    // 벽이 있을경우 벽 삭제
    private void OnTriggerStay2D(Collider2D collision)
    {
        // 실제 충돌 지점 계산
        Vector2 contactPoint = collision.ClosestPoint(transform.position);

        // 플랫폼 레이어에 해당하는 콜라이더 탐색
        Collider2D hit = Physics2D.OverlapCircle(contactPoint, 0.01f, whatisPlatform);
        if (hit != null)
        {
            PMK_TileRogic destroyTile = hit.GetComponent<PMK_TileRogic>();
            if (destroyTile != null)
            {
                destroyTile.DestoryTile(contactPoint); // 올바른 월드 위치 전달

                if (collision.CompareTag("Tileitem")) // 벽안에 아이템이 있을경우 아이템 삭제
                {
                    Destroy(collision.gameObject);
                }
                Destroy(gameObject); // 벽 제거
            }
        }
    }
}
