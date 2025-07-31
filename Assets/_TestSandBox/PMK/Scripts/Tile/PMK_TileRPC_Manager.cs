using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

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


    // 타일 아이템 생성
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Create_TileItem(int itemIndex, Vector3 worldPos)
    {
        Instantiate(tileRogic.tileItems[itemIndex].prefab, worldPos, Quaternion.identity, tileRogic.parentTrans); // 타일 아이템 생성
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


    // 타일 아이템 파괴
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_DestroyItem(Vector3 pos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.05f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Tileitem"))
            {
                Debug.Log($"타일아이템 삭제");
                Destroy(hit.gameObject);
            }
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_DestroyGameObject(string objectName)
    {
        if (!HasStateAuthority) return;

        GameObject obj = GameObject.Find(objectName);
        if (obj != null)
        {
            Destroy(obj);
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_DestroyGameObject(int rnd, Vector3Int cellPos, int trapSpawnChance)
    {
        // 해당 셀에 타일이 없으면 함정을 배치합니다.
        if (tileRogic.mainTilemap.GetTile(cellPos) == null)
        {
            if (rnd > trapSpawnChance)
            {
                // 타일이 없으면 타일을 배치합니다.
                tileRogic.mainTilemap.SetTile(cellPos, tileRogic.ruleTile);
                Physics2D.SyncTransforms();

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
                    Vector3 worldPos = tileRogic.mainTilemap.GetCellCenterWorld(cellPos);
                    Instantiate(tileRogic.trap, worldPos, Quaternion.identity, tileRogic.parentTrans);
                }
            }
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void RPC_SetPlayerGravity([RpcTarget] PlayerRef player, float gravityScale)
    {
        Debug.Log("RPC실행됨");
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
