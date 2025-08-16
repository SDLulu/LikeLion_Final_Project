using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PMK_TileRPC_Manager : NetworkBehaviour
{
    public static PMK_TileRPC_Manager Instance { get; private set; }
    private PMK_TileRogic tileRogic => PMK_TileRogic.Instance;

    private List<NetworkObject> spawnedArrows = new List<NetworkObject>(); // 발사된 화살들을 저장하는 리스트


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
        tileRogic.mainTilemap.SetTile(cellPos, tileRogic._setRuleTile);
        Physics2D.SyncTransforms();

        tileRogic.Create_TileItem(cellPos);
    }


    // 타일 파괴
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_DestroyTile(Vector3Int Pos)
    {
        Debug.DrawRay(Pos, Vector2.up * 0.2f, Color.red, 1f);

        tileRogic.mainTilemap.SetTile(Pos, null);
        tileRogic.mainTilemap.RefreshTile(Pos);
    }

    // 타일 파괴 + 같은 셀의 타일아이템/파괴오브젝트 정리를 한 번에 수행
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_DestroyTileAndCleanup(Vector3 worldPos)
    {
        // 셀 스냅
        Vector3Int cellPos = tileRogic.mainTilemap.WorldToCell(worldPos);
        Vector3 cellCenter = tileRogic.mainTilemap.GetCellCenterWorld(cellPos);

        // 타일 제거
        if (tileRogic.mainTilemap.HasTile(cellPos))
        {
            tileRogic.mainTilemap.SetTile(cellPos, null);
            tileRogic.mainTilemap.RefreshTile(cellPos);
        }

        // 같은 셀 영역 내 타일아이템/파괴오브젝트 정리
        // 셀 크기 근사값: 타일맵 셀은 보통 1x1, 약간 여유를 둔 박스 사용
        Vector2 boxSize = new Vector2(0.9f, 0.9f);
        var hits = Physics2D.OverlapBoxAll(cellCenter, boxSize, 0f);
        if (hits == null || hits.Length == 0) return;

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h == null) continue;
            if (h.CompareTag("Tileitem"))
            {
                var item = h.GetComponent<PMK_TileItem>();
                if (item != null)
                {
                    item.DestroyItem();
                }
                else
                {
                    UnityEngine.Object.Destroy(h.gameObject);
                }
            }
            else if (h.CompareTag("BoobDestoryObj"))
            {
                UnityEngine.Object.Destroy(h.gameObject);
            }
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
            var item = DataManager.Inst.ItemData[itemIndex].PrefabPath;
            var prefab = Resources.Load<GameObject>(item);
            Instantiate(prefab, worldPos, Quaternion.identity, tileRogic.parentTrans);
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
    public void RPC_RndTileSpawn(int rnd1, int rnd2, Vector3Int cellPos, int trapSpawnChance)
    {
        // 해당 셀에 타일이 없으면 랜덤 타일을 배치합니다.
        if (tileRogic.mainTilemap.GetTile(cellPos) == null)
        {
            if (rnd1 > trapSpawnChance)
            {
                if (rnd2 > 50)
                {
                    StartCoroutine(DelayedTrapSpawn(cellPos));
                }
                else
                {
                    TileSpawnBool(cellPos);
                }
            }
        }

        StartCoroutine(TestTileda(cellPos));
    }

    private IEnumerator DelayedTrapSpawn(Vector3Int cellPos)
    {
        yield return null;

        bool isThreeAboveEmpty =
        tileRogic.mainTilemap.GetTile(cellPos + new Vector3Int(0, 1, 0)) == null &&
        tileRogic.mainTilemap.GetTile(cellPos + new Vector3Int(0, 2, 0)) == null &&
        tileRogic.mainTilemap.GetTile(cellPos + new Vector3Int(0, 3, 0)) == null;
        Vector3Int downCell = cellPos + Vector3Int.down;

        if (isThreeAboveEmpty && tileRogic.mainTilemap.GetTile(downCell) != null)
        {
            yield return null;

            for (int i = 0; i < 3; i++)
            {
                Rpc_DestroyTile(cellPos + new Vector3Int(0, i, 0));
                Rpc_DestroyItem(cellPos + new Vector3Int(0, i, 0));
            }

            Vector3 worldPos = tileRogic.mainTilemap.GetCellCenterWorld(cellPos);

            RPC_SpawnTrap(worldPos, 0);
        }
        else
        {
            RPC_Create_Tile(cellPos);
        }
    }


    // 위 아래 셀에 타일이 없을 때 돌 함정 생성 (빈 공간 방지)
    public void TileSpawnBool(Vector3Int cellPos)
    {
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

        Vector3 worldPos = tileRogic.mainTilemap.GetCellCenterWorld(cellPos);

        if (isThreeAboveEmpty)
        {
            RPC_SpawnTrap(worldPos, 1);
            Rpc_DestroyItem(cellPos);
        }
        else
        {
            RPC_Create_Tile(cellPos);
        }
    }


    private IEnumerator TestTileda(Vector3Int cellPos)
    {
        yield return new WaitForSeconds(0.5f);

        TileBase tileUp = tileRogic.mainTilemap.GetTile(cellPos + Vector3Int.up);
        TileBase tileUp2 = tileRogic.mainTilemap.GetTile(cellPos + new Vector3Int(0, 2, 0));
        TileBase tileUp3 = tileRogic.mainTilemap.GetTile(cellPos + new Vector3Int(0, 3, 0));
        TileBase tileDown = tileRogic.mainTilemap.GetTile(cellPos + Vector3Int.down);
        TileBase tileCenter = tileRogic.mainTilemap.GetTile(cellPos);

        // 셀 위치에 오브젝트가 있는지 확인
        Vector3 worldCenter = tileRogic.mainTilemap.GetCellCenterWorld(cellPos);
        Vector3 worldCenter1 = tileRogic.mainTilemap.GetCellCenterWorld(cellPos + Vector3Int.up);
        Vector3 worldCenter2 = tileRogic.mainTilemap.GetCellCenterWorld(cellPos + new Vector3Int(0, 2, 0));
        Vector3 worldCenter3 = tileRogic.mainTilemap.GetCellCenterWorld(cellPos + new Vector3Int(0, 3, 0));
        Collider2D colUp = Physics2D.OverlapPoint(worldCenter, LayerMask.GetMask("Trap")); // 검사할 레이어 지정
        Collider2D colUp1 = Physics2D.OverlapPoint(worldCenter1, LayerMask.GetMask("Trap")); // 검사할 레이어 지정
        Collider2D colUp2 = Physics2D.OverlapPoint(worldCenter1, LayerMask.GetMask("Trap")); // 검사할 레이어 지정
        Collider2D colUp3 = Physics2D.OverlapPoint(worldCenter1, LayerMask.GetMask("Trap")); // 검사할 레이어 지정

        bool isObjectOnTile = colUp1 != null;

        bool isTileEmpty =
            colUp == null && colUp1 == null && colUp2 == null && colUp3 == null && tileUp == null && tileUp2 == null && tileUp3 == null && tileDown != null && tileCenter == null ||
                        colUp == null && colUp1 == null && colUp2 == null && tileUp == null && tileUp2 == null && tileDown != null && tileCenter == null ||
                                    colUp == null && colUp1 == null && tileUp == null && tileDown != null && tileCenter == null ||
                                                colUp == null && tileDown != null && tileCenter == null ||
                                                            colUp == null && tileUp != null && tileCenter == null;

        if (isTileEmpty)
        {
            RPC_Create_Tile(cellPos);
        }
    }
    #endregion


    #region Trap 관련 RPC 메서드

    // 함정 생성
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_SpawnTrap(Vector3 worldPos, int trapIndex)
    {
        Instantiate(tileRogic.trap[trapIndex], worldPos, Quaternion.identity, tileRogic.parentTrans);
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SpawnArrow(Vector3 position, Vector2 velocity)
    {
        if (!Object.HasStateAuthority) return; // 클라이언트는 스폰 못 함

        var arrowInstance = Runner.Spawn(tileRogic.launchTrapPrefab, position, Quaternion.identity);
        spawnedArrows.Add(arrowInstance);

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


    // 모든 화살 제거
    public void ClearAllArrows()
    {
        foreach (var arrow in spawnedArrows)
        {
            if (arrow != null)
            {
                Runner.Despawn(arrow);
            }
        }
        spawnedArrows.Clear();
    }
    #endregion
}
