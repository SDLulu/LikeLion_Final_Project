using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.CompilerServices; // Coroutine을 위해 추가

public class PMK_Tile_Rogic : MonoBehaviour
{
    [Header("타일 길이")]
    [SerializeField] private int _MaxTileX = 3; // 맵의 가로 기준 길이
    [SerializeField] private int _MaxTileY = 3; // 맵의 세로 기준 길이

    [Header("다음 타일과의 거리")]
    [SerializeField] private float _NextTile = 17.6f; // 맵의 가로 기준 길이
    [SerializeField] private Transform _ParentTransform;

    [Header("타일 오브젝트")]
    private Dictionary<string, GameObject[]> _mapPrefabDict; //타일 이름 저장

    [SerializeField] private GameObject[] _Clear_Map_Prefab;        // 스폰지점 or 클리어 맵 생성 (스폰지점 : 좌,우 확정) (클리어 : 좌,우,위 확정)
    [SerializeField] private GameObject[] _LR_Exit_Map_Prefab;      // 좌,우 출구가 확정인 맵 생성 (위,아래 랜덤)
    [SerializeField] private GameObject[] _D_Exit_Map_Prefab;       // 아래 출구가 확정인 맵 생성 (좌,우,위 랜덤)
    [SerializeField] private GameObject[] _WD_Exit_Map_Prefab;      // 위,아래 출구가 확정인 맵 생성 (좌,우 랜덤)
    [SerializeField] private GameObject[] _Special_Map_Prefab;      // 상점이나 특별한 맵 생성 (좌,우 확정)

    private Vector2[,] _MapXY; // 맵 타일 위치 저장용 2차원 배열
    private bool[,] _UseMapXy; // 맵 타일이 생성되었는지 여부를 저장하는 2차원 배열

    private int _removeMapX; // 정하고 싶지 않는 맵의 X위치를 저장합니다.
    private List<int> _LR_Choose = new List<int>(); // 왼쪽, 오른쪽 맵 위치를 저장하는 리스트 입니다.


    private void Awake()
    {
        _mapPrefabDict = new Dictionary<string, GameObject[]>
    {
        { "C", _Clear_Map_Prefab },
        { "LR", _LR_Exit_Map_Prefab },
        { "D", _D_Exit_Map_Prefab },
        { "WD", _WD_Exit_Map_Prefab },
        { "S", _Special_Map_Prefab }
    };
    }

    private void Start()
    {
        SaveMapPos();

        SpawnMap_Instantiate(); // 스폰 맵 생성하기

        DownExit_Map_Instantiate(_removeMapX); // 시작맵을 제외한 y0 라인에 있는 맵 중 하나를 선택하여 확정 탈출구 생성하기

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) // 1번키를 눌렀을 때 맵을 초기화하고 다시 생성합니다.
        {
            ResetMap();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) // 2번키를 눌렀을 때 빈 공간에 랜덤 맵을 채웁니다.
        {
            Create_EmptyMap();
        }
    }


    #region 맵 초기화 및 재생성
    private void ResetMap()
    {
        // 부모 오브젝트의 자식 오브젝트를 모두 제거합니다.
        foreach (Transform child in _ParentTransform)
        {
            Destroy(child.gameObject);
        }

        // 맵 다시 생성
        SpawnMap_Instantiate();
        LR_RandomChoose(_removeMapX);
        DownExit_Map_Instantiate(_removeMapX);
    }
    #endregion


    #region 전체 맵 위치 저장
    private void SaveMapPos()
    {
        // 정한 맵 크기 만큼 타일위치를 mapX에 저장

        _MapXY = new Vector2[_MaxTileX, _MaxTileY];
        _UseMapXy = new bool[_MaxTileX, _MaxTileY];

        for (int y = 0; y < _MaxTileY; y++)
        {
            for (int x = 0; x < _MaxTileX; x++)
            {
                _MapXY[x, y] = new Vector2(x * _NextTile, y * -_NextTile);
                _UseMapXy[x, y] = false;
                Debug.Log($"x: {x}, y: {y} → pos: {_MapXY[x, y]}");
            }
        }
    }
    #endregion


    #region 원하는 맵 생성
    private void Create_Map(string mapType, int randomIndex, float spawnXpos, float spawnYpos)
    {
        if (_mapPrefabDict.TryGetValue(mapType, out GameObject[] prefabs))
        {
            Instantiate(prefabs[randomIndex], new Vector3(spawnXpos, spawnYpos, 0), Quaternion.identity, _ParentTransform);


            Vector2 pos = new Vector2(spawnXpos, spawnYpos);
            for (int y = 0; y < _MaxTileY; y++)
            {
                for (int x = 0; x < _MaxTileX; x++)
                {
                    if (_MapXY[x, y] == pos)
                    {
                        _UseMapXy[x, y] = true;
                        return;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"MapType '{mapType}'입력된 이름이 아님!");
        }
    }
    #endregion

    // 빈 공간에 랜덤 맵 채우기
    private void Create_EmptyMap()
    {
        for (int y = 0; y < _MaxTileY; y++)
        {
            for (int x = 0; x < _MaxTileX; x++)
            {
                if (!_UseMapXy[x, y]) // 해당 위치에 맵이 생성되지 않았다면
                {
                    Vector2 emptyPos = _MapXY[x, y];
                    Create_Map("LR", 0, emptyPos.x, emptyPos.y); // 좌우가 확정인 맵 생성
                }
            }
        }
    }


    #region 스폰맵 생성
    private void SpawnMap_Instantiate()
    {
        //캐릭터 스폰 맵 생성하기
        _removeMapX = Random.Range(0, _MaxTileX); // x값 랜덤 생성하여 y0에 시작맵 생성
        Vector2 spawnPos = _MapXY[_removeMapX, 0];
        Create_Map("C", 0, spawnPos.x, spawnPos.y);
    }
    #endregion


    #region 원하는 맵 기준 왼쪽, 오른쪽 선택 및 저장
    private void LR_RandomChoose(int removeTile) // 원하는 맵 기준으로 왼쪽, 오른쪽 맵 위치를 정하기
    {
        _LR_Choose.Clear(); // 이전에 저장된 리스트 초기화

        bool is_LR = Random.Range(0, 2) == 0; // 왼쪽 오른쪽 정하기

        if (is_LR && removeTile > 0)
        {
            for (int i = 0; i < removeTile; i++) // 왼쪽 후보
                _LR_Choose.Add(i);
        }
        else if (_LR_Choose.Count == 0)
        {
            for (int i = removeTile + 1; i < _MaxTileX; i++) // 오른쪽 후보
                _LR_Choose.Add(i);
        }

        if (!is_LR && removeTile != _MaxTileX - 1)
        {
            for (int i = removeTile + 1; i < _MaxTileX; i++) // 오른쪽 후보
                _LR_Choose.Add(i);
        }
        else if (_LR_Choose.Count == 0)
        {
            for (int i = 0; i < removeTile; i++) // 왼쪽 후보
                _LR_Choose.Add(i);
        }
    }
    #endregion


    #region 원하는 맵 기준 탈출 맵 생성 및 사이를 양옆이 뚥린 맵으로 채움
    private void DownExit_Map_Instantiate(int removeTile)
    {
        for (int y = 0; y < _MaxTileY; y++)
        {
            LR_RandomChoose(removeTile); // 왼쪽, 오른쪽 맵 위치 정하기

            // 좌우 선택된 위치 중에서 랜덤으로 탈출 맵 생성
            int exitDownMap = _LR_Choose[Random.Range(0, _LR_Choose.Count)];
            Vector2 exitPos = _MapXY[exitDownMap, y];

            if (y != _MaxTileY - 1) // 마지막 y값이 아닐 때는 아래 탈출구가 확정된 맵 생성
            {
                Create_Map("D", 0, exitPos.x, exitPos.y);
            }
            else // 마지막 y값일 때는 클리어 맵 생성
            {
                Create_Map("C", 1, exitPos.x, exitPos.y);
            }

            // 원하는 맵과 탈출 맵 사이를 뚫린 맵으로 채우기
            int min = Mathf.Min(removeTile, exitDownMap);
            int max = Mathf.Max(removeTile, exitDownMap);

            for (int x = min + 1; x < max; x++)
            {
                Vector2 fillPos = _MapXY[x, y];
                Create_Map("LR", 0, fillPos.x, fillPos.y); // 좌우만 뚫린 맵으로 채움
            }



            // 탈출구 y값에 다음 탈출구가 확정 되어있는 타일 생성

            if (y + 1 < _MaxTileY)
            {
                Vector2 nextExit_Pos = _MapXY[exitDownMap, y + 1];

                if (exitDownMap != Random.Range(0, _MaxTileX)) // 랜덤값이 현재 탈출 맵과 같지 않다면 좌,우 탈출구가 확정인 맵 생성
                {
                    Create_Map("LR", 0, nextExit_Pos.x, nextExit_Pos.y);
                    removeTile = exitDownMap;
                }
                else
                {
                    Create_Map("WD", 0, nextExit_Pos.x, nextExit_Pos.y); // 랜덤값이 현재 탈출 맵과 같다면 위,아래가 탈출구가 확정인 맵 생성
                    removeTile = exitDownMap;

                    if (y + 2 < _MaxTileY) // 이 안에서 또 다음 타일 확인
                    {
                        Vector2 deeperExit_Pos = _MapXY[exitDownMap, y + 2];
                        Create_Map("LR", 0, deeperExit_Pos.x, deeperExit_Pos.y); // 다음 타일에 dkfo 탈출구가 확정인 맵 생성
                        removeTile = exitDownMap;
                        y++;
                    }
                }
            }

        }
    }
    #endregion
}