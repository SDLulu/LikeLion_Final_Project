using System.Collections;
using Fusion;
using UnityEngine;

public class PMK_TileRPC_Manager : NetworkBehaviour
{
    public static PMK_TileRPC_Manager Instance { get; private set; }
    private PMK_TileRogic tileRogic => PMK_TileRogic.Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    #region 타일 관련 RPC 메서드
    // 타일 생성
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Create_Tile(Vector3Int cellPos)
    {
        tileRogic.mainTilemap.SetTile(cellPos, tileRogic.ruleTile);
        Physics2D.SyncTransforms();

        tileRogic.Create_TileItem(cellPos);
    }


    // 타일 파괴
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_DestroyTile(Vector3 Pos)
    {
        Debug.DrawRay(Pos, Vector2.up * 0.2f, Color.red, 1f);

        Vector3Int cellPosition = tileRogic.mainTilemap.WorldToCell(Pos);

        if (tileRogic.mainTilemap.HasTile(cellPosition))
        {
            tileRogic.mainTilemap.SetTile(cellPosition, null);
            tileRogic.mainTilemap.RefreshTile(cellPosition);
        }
    }


    // 타일 아이템 생성
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Create_TileItem(int itemIndex, Vector3 worldPos, bool tileTrap)
    {
        Vector3Int cellPos = tileRogic.mainTilemap.WorldToCell(worldPos);

        if (tileTrap)
        {
            tileRogic.mainTilemap.SetTile(cellPos, null);
            tileRogic.mainTilemap.RefreshTile(cellPos);
            Instantiate(tileRogic.trap[1], worldPos, Quaternion.identity, tileRogic.parentTrans);
        }
        else
        {
            Instantiate(tileRogic.tileItems[itemIndex].prefab, worldPos, Quaternion.identity, tileRogic.parentTrans);
        }
    }


    // 타일 아이템 파괴
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_DestroyItem(Vector3 pos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.05f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Tileitem"))
            {
                Destroy(hit.gameObject);
            }
        }
    }
    #endregion


    #region TileZoneSpawner 관련 RPC
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RndTileSpawn(int rnd, Vector3Int cellPos, int trapSpawnChance)
    {
        // 해당 셀에 타일이 없으면 함정을 배치합니다.
        if (tileRogic.mainTilemap.GetTile(cellPos) == null)
        {
            if (rnd > trapSpawnChance)
            {
                // 타일이 없으면 타일을 배치합니다.
                tileRogic.mainTilemap.SetTile(cellPos, tileRogic.ruleTile);

                PMK_TileRogic.Instance.Create_TileItem(cellPos);
            }
            else
            {

                bool isThreeAboveEmpty =
                tileRogic.mainTilemap.GetTile(cellPos + new Vector3Int(0, 1, 0)) == null &&
                tileRogic.mainTilemap.GetTile(cellPos + new Vector3Int(0, 2, 0)) == null &&
                tileRogic.mainTilemap.GetTile(cellPos + new Vector3Int(0, 3, 0)) == null;
                Vector3Int downCell = cellPos + Vector3Int.down;



                // 아래 셀에 타일이 없고, 위쪽 셀 3개가 비어있을 때 함정 생성
                if (isThreeAboveEmpty && tileRogic.mainTilemap.GetTile(downCell) != null)
                {
                    StartCoroutine(DelayedTrapSpawn(cellPos));
                }
                else
                {
                    RPC_Create_Tile(cellPos);
                }
            }
        }
    }

    private IEnumerator DelayedTrapSpawn(Vector3Int cellPos)
    {
        yield return new WaitForSeconds(0.05f); // 0.05초 딜레이 (충돌 방지용)

        if (HasStateAuthority)
        {
            Rpc_DestroyTile(cellPos + new Vector3Int(0, 1, 0));
            Rpc_DestroyTile(cellPos + new Vector3Int(0, 2, 0));
            Rpc_DestroyTile(cellPos + new Vector3Int(0, 3, 0));
            Rpc_DestroyItem(cellPos + new Vector3Int(0, 1, 0));
            Rpc_DestroyItem(cellPos + new Vector3Int(0, 2, 0));
            Rpc_DestroyItem(cellPos + new Vector3Int(0, 3, 0));
        }

        Vector3 worldPos = tileRogic.mainTilemap.GetCellCenterWorld(cellPos);
        Instantiate(tileRogic.trap[0], worldPos, Quaternion.identity, tileRogic.parentTrans);
    }


    // 위 아래 셀에 타일이 없을 때 돌 함정 생성 (빈 공간 방지)
    public void DelayedTileSpawnBool(Vector3Int cellPos)
    {
        if (!HasInputAuthority) return;

        bool isTileEmpty =
        tileRogic.mainTilemap.GetTile(cellPos + Vector3Int.up) != null &&
        tileRogic.mainTilemap.GetTile(cellPos + Vector3Int.down) != null &&
        tileRogic.mainTilemap.GetTile(cellPos) == null;

        bool isThreeAboveEmpty =
        tileRogic.mainTilemap.GetTile(cellPos + Vector3Int.up) != null &&
        tileRogic.mainTilemap.GetTile(cellPos + Vector3Int.down) != null &&
        tileRogic.mainTilemap.GetTile(cellPos + Vector3Int.left) != null &&
        tileRogic.mainTilemap.GetTile(cellPos + Vector3Int.right) != null &&
        tileRogic.mainTilemap.GetTile(cellPos) == null;

        RPC_DelayedTileSpawn(cellPos, isTileEmpty, isThreeAboveEmpty);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_DelayedTileSpawn(Vector3Int cellPos, bool isTileEmpty, bool isThreeAboveEmpty)
    {
        Vector3 worldPos = tileRogic.mainTilemap.GetCellCenterWorld(cellPos);
        Vector3Int tilePos = tileRogic.mainTilemap.WorldToCell(cellPos);

        if (isTileEmpty)
        {
            RPC_Create_Tile(tilePos);
        }
        else if (isThreeAboveEmpty)
        {
            Instantiate(tileRogic.trap[1], worldPos, Quaternion.identity, tileRogic.parentTrans);
        }
    }
    #endregion




    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SpawnArrow(Vector3 position, Vector2 velocity)
    {
        if (!Object.HasStateAuthority) return; // 클라이언트는 스폰 못 함

        var arrowInstance = Runner.Spawn(tileRogic.launchTrapPrefab, position, Quaternion.identity);
        var rb = arrowInstance.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
    }


    // 즉사 트랩 플레이어 중력 설정 (미완 - 아마 플레이어 스크립트를 건드려서 해야 될 듯 함)
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_SetPlayerGravity([RpcTarget] PlayerRef player, float gravityScale)
    {
        var playerObj = Runner.GetPlayerObject(player);
        if (playerObj == null) return;

        var rb = playerObj.GetComponentInChildren<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = gravityScale;
            if (gravityScale < 1f)
                rb.linearVelocity = Vector2.zero;
        }
    }
}
