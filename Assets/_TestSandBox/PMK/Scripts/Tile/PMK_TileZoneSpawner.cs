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

    private void Start()
    {
        TryPlaceTileIfEmpty();
    }

    private void TryPlaceTileIfEmpty()
    {

        Vector2 pos = transform.position;
        Collider2D hits = Physics2D.OverlapCircle(pos, 0.01f, whatisPlatform);

        Vector3Int cellPos = tileRogic.mainTilemap.WorldToCell(pos);

        // 해당 셀에 타일이 있는지 확인합니다.
        if (hits == null)
        {
            int rnd1 = Random.Range(0, 100);
            int rnd2 = Random.Range(0, 100);
            if (tileRPCManager.HasStateAuthority)
            {
                tileRPCManager.RPC_RndTileSpawn(rnd1, rnd2, cellPos, trapSpawnChance);
            }
        }

        Destroy(gameObject);
    }
}

