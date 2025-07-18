using System.Collections;
using System.Collections.Generic;
using GoogleSheet.Type;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.Progress;

public class PMK_RandomTile : MonoBehaviour
{
    private Tilemap tilemap;
    [SerializeField] private GameObject trap;
    [SerializeField] private LayerMask whatisPlatform;
    [SerializeField] private TileBase ruleTile;

    private void OnEnable()
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

                if (Random.Range(0, 100) > 50)
                {
                    // 타일 설치
                    tilemap.SetTile(cellPos, ruleTile);

                    Physics2D.SyncTransforms(); // 물리 최신화

                    PMK_TileRogic.Instance.Create_TileItem(cellPos); // 아이템 랜덤 생성

                    Destroy(gameObject); // 현재 아이템 제거
                }
                else
                {
                    yield return null;

                    bool isThreeAboveEmpty =
                    tilemap.GetTile(cellPos + new Vector3Int(0, 1, 0)) == null &&
                    tilemap.GetTile(cellPos + new Vector3Int(0, 2, 0)) == null &&
                    tilemap.GetTile(cellPos + new Vector3Int(0, 3, 0)) == null;
                    Vector3Int downCell = cellPos + Vector3Int.down;



                    // 위3칸 양옆에 타일이 없고 , 아래에 타일이 있을 경우 함정 설치
                    if (isThreeAboveEmpty && tilemap.GetTile(downCell) != null)
                    {
                        Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
                        Instantiate(trap, worldPos, Quaternion.identity, PMK_TileRogic.Instance.parentTrans);
                    }
                    Destroy(gameObject); // 현재 아이템 제거
                }
            }
        }
        else
        { 
            Debug.Log("이미 타일이 존재합니다: " + pos);
        }
    }
}
