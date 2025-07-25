using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PMK_TileDestroyer : MonoBehaviour
{
    [SerializeField] private LayerMask whatisPlatform;
    [SerializeField] private TileBase ruleTile;

    private Tilemap tilemap;



    private void Start()
    {
        tilemap = PMK_TileRogic.Instance.mainTilemap;
        StartCoroutine(DelayedTilePlacement());
    }

    private IEnumerator DelayedTilePlacement()
    {
        yield return null;
        TryPlaceTileIfEmpty();
    }

    // 공간이 비어있으면 타일을 배치합니다.
    private void TryPlaceTileIfEmpty()
    {
        Vector2 pos = transform.position;
        Collider2D hits = Physics2D.OverlapCircle(pos, 0.01f, whatisPlatform);
        if (hits == null)
        {
            Vector3Int cellPos = tilemap.WorldToCell(pos);

            if (tilemap.GetTile(cellPos) == null)
            {

                tilemap.SetTile(cellPos, ruleTile);
                Physics2D.SyncTransforms();

                PMK_TileRogic.Instance.Create_TileItem(cellPos);

                Destroy(gameObject);
            }
        }
    }


    // 타일이 비어있지않다면 타일을 파괴합니다.
    private void OnTriggerStay2D(Collider2D collision)
    {
        Vector2 contactPoint = collision.ClosestPoint(transform.position);


        Collider2D hit = Physics2D.OverlapCircle(contactPoint, 0.01f, whatisPlatform);
        if (hit != null)
        {
            PMK_TileRogic destroyTile = hit.GetComponent<PMK_TileRogic>();
            if (destroyTile != null)
            {
                destroyTile.DestoryTile(contactPoint);

                if (collision.CompareTag("Tileitem"))
                {
                    Destroy(collision.gameObject);
                }
                Destroy(gameObject);
            }
        }
    }
}
