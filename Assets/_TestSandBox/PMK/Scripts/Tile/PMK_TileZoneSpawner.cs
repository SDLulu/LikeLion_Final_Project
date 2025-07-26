using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PMK_TileZoneSpawner : MonoBehaviour
{
    private Tilemap tilemap;
    [SerializeField] private GameObject trap;
    [SerializeField] private LayerMask whatisPlatform;
    [SerializeField] private TileBase ruleTile;

    [SerializeField] private int trapSpawnChance = 50;

    private void Start()
    {
        tilemap = PMK_TileRogic.Instance.mainTilemap;
        StartCoroutine(TryPlaceTileIfEmpty());
    }

    private IEnumerator TryPlaceTileIfEmpty()
    {
        yield return null;

        Vector2 pos = transform.position;

        Collider2D hits = Physics2D.OverlapCircle(pos, 0.01f, whatisPlatform);
        if (hits == null)
        {
            Vector3Int cellPos = tilemap.WorldToCell(pos);

            if (tilemap.GetTile(cellPos) == null)
            {

                if (Random.Range(0, 100) > trapSpawnChance)
                {
                    // trapSpawnChance 확률로 함정 생성
                    tilemap.SetTile(cellPos, ruleTile);

                    Physics2D.SyncTransforms();

                    PMK_TileRogic.Instance.Create_TileItem(cellPos);

                    Destroy(gameObject);
                }
                else
                {
                    yield return null;

                    bool isThreeAboveEmpty =
                    tilemap.GetTile(cellPos + new Vector3Int(0, 1, 0)) == null &&
                    tilemap.GetTile(cellPos + new Vector3Int(0, 2, 0)) == null &&
                    tilemap.GetTile(cellPos + new Vector3Int(0, 3, 0)) == null;
                    Vector3Int downCell = cellPos + Vector3Int.down;



                    // 아래 셀에 타일이 없고, 위쪽 셀 3개가 비어있을 때 함정 생성
                    if (isThreeAboveEmpty && tilemap.GetTile(downCell) != null)
                    {
                        Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);

                        Instantiate(trap, worldPos, Quaternion.identity, PMK_TileRogic.Instance.parentTrans); // 자식으로 추가
                    }
                    Destroy(gameObject);
                }
            }
        }
        else
        {
            Debug.Log("�̹� Ÿ���� �����մϴ�: " + pos);
        }
    }
}

