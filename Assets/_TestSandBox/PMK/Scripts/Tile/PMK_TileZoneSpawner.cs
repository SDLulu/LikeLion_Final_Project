using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PMK_TileZoneSpawner : MonoBehaviour
{
    private PMK_TileRogic tileRogic => PMK_TileRogic.Instance;
    private PMK_TileRPC_Manager tileRPCManager => PMK_TileRPC_Manager.Instance;

    [SerializeField] private LayerMask whatisPlatform;
    [SerializeField] private TileBase ruleTile;

    [SerializeField] private int trapSpawnChance = 50;
    private int rnd;

    private void Start()
    {
        TryPlaceTileIfEmpty();

        StartCoroutine(DelayedTileSpawn());
    }

    private void TryPlaceTileIfEmpty()
    {
        Vector2 pos = transform.position;
        Collider2D hits = Physics2D.OverlapCircle(pos, 0.01f, whatisPlatform);

        Vector3Int cellPos = tileRogic.mainTilemap.WorldToCell(pos);

        // 해당 셀에 타일이 있는지 확인합니다.
        if (hits == null)
        {
            rnd = Random.Range(0, 100);
            if (tileRPCManager.HasStateAuthority)
            {
                tileRPCManager.RPC_RndTileSpawn(rnd, cellPos, trapSpawnChance);
            }
        }
    }


    private IEnumerator DelayedTileSpawn()
    {
        yield return new WaitForSeconds(0.05f);

        if (tileRPCManager.HasStateAuthority)
        {
            Vector2 pos = transform.position;
            Vector3Int cellPos = tileRogic.mainTilemap.WorldToCell(pos);
            tileRPCManager.RPC_DelayedTileSpawn(cellPos);
        }
    }
}

