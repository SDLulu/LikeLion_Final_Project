using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using Fusion;

public class PMK_TileZoneSpawner : MonoBehaviour
{
    private Tilemap tilemap;
    [SerializeField] private GameObject trap;
    [SerializeField] private LayerMask whatisPlatform;
    [SerializeField] private TileBase ruleTile;

    [SerializeField] private int trapSpawnChance = 50;
    private int rnd;

    private void Start()
    {
        tilemap = PMK_TileRogic.Instance.mainTilemap;
        TryPlaceTileIfEmpty();
    }


    private void TryPlaceTileIfEmpty()
    {
        Vector2 pos = transform.position;
        Collider2D hits = Physics2D.OverlapCircle(pos, 0.01f, whatisPlatform);

        Vector3Int cellPos = tilemap.WorldToCell(pos);

        //
        if (hits == null)
        {
            // 해당 셀에 타일이 없으면 함정을 배치합니다.
            if (tilemap.GetTile(cellPos) == null)
            {
                int rnd = PMK_TileRogic.Instance.rnd;

                if (rnd > trapSpawnChance)
                {
                    // 타일이 없으면 타일을 배치합니다.
                    tilemap.SetTile(cellPos, ruleTile);
                    Physics2D.SyncTransforms();

                    PMK_TileRogic.Instance.Create_TileItem(cellPos);

                    Destroy(gameObject);
                }
                else
                {

                    bool isThreeAboveEmpty =
                    tilemap.GetTile(cellPos + new Vector3Int(0, 1, 0)) == null &&
                    tilemap.GetTile(cellPos + new Vector3Int(0, 2, 0)) == null &&
                    tilemap.GetTile(cellPos + new Vector3Int(0, 3, 0)) == null;
                    Vector3Int downCell = cellPos + Vector3Int.down;



                    // 아래 셀에 타일이 없고, 위쪽 셀 3개가 비어있을 때 함정 생성
                    if (isThreeAboveEmpty && tilemap.GetTile(downCell) != null)
                    {
                        Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
                        PMK_TileRogic.Instance.RPC_Create_Trap(trap, worldPos);
                    }
                    Destroy(gameObject);
                }
            }
        }
    }
}

