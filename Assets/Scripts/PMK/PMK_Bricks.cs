using UnityEngine;
using UnityEngine.Tilemaps;

public class PMK_Bricks : MonoBehaviour
{
    [SerializeField] private Tilemap _TileMap;

    private void Start()
    {
        _TileMap = GetComponent<Tilemap>();
    }

    public void MakeDot(Vector3 Pos)
    {
        Debug.DrawRay(Pos, Vector2.up * 0.2f, Color.red, 1f);

        Vector3Int cellPosition = _TileMap.WorldToCell(Pos);

        Debug.Log($"지우려는 타일 위치: {Pos}");

        if (_TileMap.HasTile(cellPosition))
        {
            _TileMap.SetTile(cellPosition, null);  // 타일 제거
            _TileMap.RefreshTile(cellPosition);
            Debug.Log("타일 삭제 성공");
        }
        else
        {
            Debug.Log("타일 없음 → 삭제 실패");
        }
    }
}
