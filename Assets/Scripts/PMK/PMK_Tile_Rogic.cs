using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections; // Coroutine을 위해 추가

public class PMK_Tile_Rogic : MonoBehaviour
{
    [Header("타일 길이")]
    [SerializeField] private int _MaxTileX = 3; // 맵의 가로 기준 길이
    [SerializeField] private int _MaxTileY = 3; // 맵의 세로 기준 길이

    [Header("다음 타일과의 거리")]
    [SerializeField] private int _NextTile = 16; // 맵의 가로 기준 길이
    [SerializeField] private Transform _ParentTransform;

    [Header("타일 오브젝트")]
    [SerializeField] private GameObject[] _Clear_Map_Prefab;        // 0
    [SerializeField] private GameObject[] _LR_Exit_Map_Prefab;      // 1
    [SerializeField] private GameObject[] _LRD_Exit_Map_Prefab;     // 2
    [SerializeField] private GameObject[] _LRDW_Exit_Map_Prefab;    // 3
    [SerializeField] private GameObject[] _Special_Map_Prefab;      // 4


    private Vector2Int[,] mapXY;


    private void Start()
    {
        SaveMapPos();
        StartMap();
    }

    // y0 { 0 16 32 }
    // y1 { 0 16 32 }
    // y2 { 0 16 32 }


    private void SaveMapPos()
    {
        // 정한 맵 크기 만큼 타일위치를 mapX에 저장

        mapXY = new Vector2Int[_MaxTileX, _MaxTileY];

        for (int y = 0; y < _MaxTileY; y++)
        {
            for (int x = 0; x < _MaxTileX; x++)
            {
                mapXY[x, y] = new Vector2Int(x * _NextTile, y * -_NextTile);
                Debug.Log($"x: {x}, y: {y} → pos: {mapXY[x, y]}");
            }
        }
    }

    private void StartMap()
    {
        //캐릭터 스폰 맵 생성하기
        int spawnMapX = Random.Range(0, _MaxTileX); // x값 랜덤 생성하여 y0에 시작맵 생성
        Vector2Int spawnPos = mapXY[spawnMapX, 0];
        Instantiate_Map_Prefab(0,0,spawnPos.x,spawnPos.y);
        Debug.Log($"{spawnPos}");



        // 스폰 맵을 제외한 y0에 확정 탈출구 생성하기 (2번 맵)
        List<int> availableX = new List<int>();
        for (int i = 0; i < _MaxTileX; i++)
        {
            if (i != spawnMapX)
                availableX.Add(i);
        }

        int y0_DownMapX = availableX[Random.Range(0, availableX.Count)];
        Vector2Int y0_DownPos = mapXY[y0_DownMapX, 0];
        Instantiate_Map_Prefab(2, 0, y0_DownPos.x, y0_DownPos.y);

        // 확정 탈출구 아래에 윗 탈출구와 연결하기 (3번 맵)
        Instantiate_Map_Prefab(3, 0, y0_DownPos.x, y0_DownPos.y + -_NextTile);



        // 시작 맵과 확정 출구를 제외한 y0 라인에 맵 생성하기
        for (int i = 0; i < _MaxTileX; i++)
        {
            if (i == spawnMapX || i == y0_DownMapX)
            {
                continue; // 시작 맵은 이미 생성했으므로 건너뜀
            }

            Vector2Int spawnY0PosX = mapXY[i, 0];
            int RadMap = Random.Range(1, 4);
            Instantiate_Map_Prefab(RadMap, 0, spawnY0PosX.x, spawnY0PosX.y);
        }

        // y1 라인에 맵 생성하고 y0에 있는 확정 탈출구

    }


    #region 랜덤 맵 생성
    private void Instantiate_Map_Prefab(int chose_map , int random_map , int spawnXpos , int spawnYpos)
    {
        switch (chose_map)
        {
            case 0:
                Instantiate(_Clear_Map_Prefab[random_map], new Vector3(spawnXpos, spawnYpos, 0), Quaternion.identity, _ParentTransform);
                break;
            case 1:
                Instantiate(_LR_Exit_Map_Prefab[random_map], new Vector3(spawnXpos, spawnYpos, 0), Quaternion.identity, _ParentTransform);
                break;
            case 2:
                Instantiate(_LRD_Exit_Map_Prefab[random_map], new Vector3(spawnXpos, spawnYpos, 0), Quaternion.identity, _ParentTransform);
                break;
            case 3:
                Instantiate(_LRDW_Exit_Map_Prefab[random_map], new Vector3(spawnXpos, spawnYpos, 0), Quaternion.identity, _ParentTransform);
                break;
            case 4:
                Instantiate(_Special_Map_Prefab[random_map], new Vector3(spawnXpos, spawnYpos, 0), Quaternion.identity, _ParentTransform);
                break;
        }
    }
    #endregion
}