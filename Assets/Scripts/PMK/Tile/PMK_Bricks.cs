using GoogleSheet.Type;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PMK_Bricks : MonoBehaviour
{
    private Tilemap tileMap;

    private void Start()
    {
        tileMap = PMK_TileRogic.Instance.mainTilemap;
    }

    public void MakeDot(Vector3 Pos)
    {
        Debug.DrawRay(Pos, Vector2.up * 0.2f, Color.red, 1f);

        Vector3Int cellPosition = tileMap.WorldToCell(Pos);

        if (tileMap.HasTile(cellPosition))
        {
            tileMap.SetTile(cellPosition, null);  // 타일 제거
            tileMap.RefreshTile(cellPosition);
        }
    }
}
