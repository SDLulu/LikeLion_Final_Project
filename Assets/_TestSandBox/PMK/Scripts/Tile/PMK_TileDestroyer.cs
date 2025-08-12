using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PMK_TileDestroyer : MonoBehaviour
{
    [SerializeField] private LayerMask whatisPlatform;

    private PMK_TileRogic tileRogic => PMK_TileRogic.Instance;
    private PMK_TileRPC_Manager tileRPCManager => PMK_TileRPC_Manager.Instance;


    private void Start()
    {
        StartCoroutine(DelayedTilePlace());
    }

    private IEnumerator DelayedTilePlace()
    {
        yield return new WaitForSeconds(0.05f); // Physics2D 반영을 기다림
        TryPlaceTileIfEmpty();
    }

    // 공간이 비어있으면 타일을 배치합니다.
    private void TryPlaceTileIfEmpty()
    {
        Vector2 pos = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.01f, whatisPlatform);

        Vector3Int cellPos = tileRogic.mainTilemap.WorldToCell(pos);

        // 타일이 없고, 해당 셀에 타일이 없으면 타일을 배치합니다.
        if (hits.Length == 0 && tileRogic.mainTilemap.GetTile(cellPos) == null)
        {
            if (tileRPCManager.HasStateAuthority)
            {
                tileRPCManager.RPC_Create_Tile(cellPos);
            }

            Destroy(gameObject);
        }
        // 타일이 있다면 파괴하고, 아이템 타일도 있다면 이것도 파괴합니다.
        else
        {
            if (tileRPCManager.HasStateAuthority)
            {
                tileRPCManager.Rpc_DestroyTile(cellPos);
                tileRPCManager.Rpc_DestroyItem(pos);
            }

            Destroy(gameObject);
        }
    }
}
