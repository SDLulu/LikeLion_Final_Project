using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class PMK_TileRogic : MonoBehaviour
{
    [field: SerializeField] public static PMK_TileRogic Instance { get; private set; }

    [Header("생성할 타일 개수")]
    [SerializeField] private int maxTileX = 5; // 맵의 가로 기준
    [SerializeField] private int maxTileY = 5; // 맵의 세로 기준

    [Header("다음 타일과의 거리")]
    [SerializeField] private float nextTileX = 17; // 맵의 가로 기준 길이
    [SerializeField] private float nextTileY = 11; // 맵의 가로 기준 길이
    [field: SerializeField] public Transform parentTrans { get; private set; }

    [Header("타일 오브젝트")]
    private Dictionary<string, GameObject[]> mapPrefabDict; //타일 이름 저장

    [SerializeField] private GameObject[] Clear_Map_Prefab;        // 스폰지점 or 클리어 맵 생성 (스폰지점 : 좌,우 확정) (클리어 : 좌,우,위 확정)
    [SerializeField] private GameObject[] LR_Exit_Map_Prefab;      // 좌,우 출구가 확정인 맵 생성 (위,아래 랜덤)
    [SerializeField] private GameObject[] D_Exit_Map_Prefab;       // 아래 출구가 확정인 맵 생성 (좌,우,위 랜덤)
    [SerializeField] private GameObject[] W_Exit_Map_Prefab;       // 아래 출구가 확정인 맵 생성 (좌,우,위 랜덤)
    [SerializeField] private GameObject[] WD_Exit_Map_Prefab;      // 위,아래 출구가 확정인 맵 생성 (좌,우 랜덤)
    [SerializeField] private GameObject[] Special_Map_Prefab;      // 상점이나 특별한 맵 생성 (좌,우 확정)


    [Header("타일 아이템 오브젝트")]

    [SerializeField] private int itemSpawnChance = 35; // 아이템 생성 확률 (0.0f ~ 1.0f) - 50% 확률로 아이템 생성
    [SerializeField] private List<PMK_TileTable> tileItems;





    private Vector2[,] mapXY; // 맵 타일 위치 저장용 2차원 배열
    private bool[,] useMapXY; // 맵 타일이 생성되었는지 여부를 저장하는 2차원 배열

    private int removeMapX; // 정하고 싶지 않는 맵의 X위치를 저장합니다.
    private List<int> LR_Choose = new List<int>(); // 왼쪽, 오른쪽 맵 위치를 저장하는 리스트 입니다.

    [field:SerializeField] public Tilemap mainTilemap { get; private set; } // 병합할 타일맵 (씬에 존재하는 타일맵)


    private void Awake()
    {
        Instance = this;

        mapPrefabDict = new Dictionary<string, GameObject[]>
        {
            { "C", Clear_Map_Prefab },
            { "LR", LR_Exit_Map_Prefab },
            { "D", D_Exit_Map_Prefab },
            { "W", W_Exit_Map_Prefab },
            { "WD", WD_Exit_Map_Prefab },
            { "S", Special_Map_Prefab }
        };
    }

    private void Start()
    {
        SaveMapPos(); // 전체 맵 위치 저장하기

        ResetMap();

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
        mainTilemap.ClearAllTiles();

        foreach (Transform child in parentTrans)
        {
            if (child.GetComponent<PMK_Bricks>() != null)
                continue;

            Destroy(child.gameObject);
        }

        // 맵 다시 생성
        SaveMapPos();
        SpawnMap_Instantiate();
        DownExit_Map_Instantiate(removeMapX);
        Create_Special_Map(0, 10);
        Create_EmptyMap();
    }
    #endregion


    #region 전체 맵 위치 저장
    private void SaveMapPos()
    {
        // 정한 맵 크기 만큼 타일위치를 mapX에 저장

        mapXY = new Vector2[maxTileX, maxTileY];
        useMapXY = new bool[maxTileX, maxTileY];

        for (int y = 0; y < maxTileY; y++)
        {
            for (int x = 0; x < maxTileX; x++)
            {
                mapXY[x, y] = new Vector2(x * nextTileX, y * -nextTileY);
                useMapXY[x, y] = false;
                Debug.Log($"x: {x}, y: {y} → pos: {mapXY[x, y]}");
            }
        }
    }
    #endregion


    #region 원하는 맵 생성
    private void Create_Map(string mapType, int randomIndex, float spawnXpos, float spawnYpos)
    {
        if (mapPrefabDict.TryGetValue(mapType, out GameObject[] prefabs))
        {
            if (mapType != "C") //클리어맵이 아니라면 모두 랜덤 돌리기
            {
                randomIndex = Random.Range(0, prefabs.Length);
            }
            GameObject temp = Instantiate(prefabs[randomIndex], Vector3.zero, Quaternion.identity);

            Tilemap[] tilemaps = temp.GetComponentsInChildren<Tilemap>();
            Vector3Int offset = new Vector3Int((int)spawnXpos, (int)spawnYpos, 0);

            // --- 타일 복붙 ---
            foreach (Tilemap sourceTilemap in tilemaps)
            {
                BoundsInt bounds = sourceTilemap.cellBounds;
                TileBase[] allTiles = sourceTilemap.GetTilesBlock(bounds);

                for (int x = 0; x < bounds.size.x; x++)
                {
                    for (int y = 0; y < bounds.size.y; y++)
                    {
                        TileBase tile = allTiles[x + y * bounds.size.x];
                        if (tile != null)
                        {
                            Vector3Int sourcePos = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);
                            Vector3Int targetPos = sourcePos + offset;
                            mainTilemap.SetTile(targetPos, tile);


                            // 타일 아이템 생성
                            Create_TileItem(targetPos);
                        }
                    }
                }
            }

            // --- 오브젝트 복붙 ---
            foreach (Transform child in temp.transform)
            {
                // 타일맵이 아닌 일반 오브젝트만 선택
                if (child.GetComponent<Tilemap>() == null)
                {
                    // 타일맵 기준 좌표계로 병합된 위치 계산
                    Vector3 spawnPosition = child.position + new Vector3(offset.x, offset.y, 0f);

                    GameObject clone = Instantiate(child.gameObject, spawnPosition, child.rotation, parentTrans);
                    clone.name = child.name; // 이름 유지 (디버깅 편의)
                }
            }

            Destroy(temp); // 임시 프리팹 제거
            mainTilemap.RefreshAllTiles(); // 타일맵 최신화

            // 좌표 기록
            Vector2 pos = new Vector2(spawnXpos, spawnYpos);
            for (int y = 0; y < maxTileY; y++)
            {
                for (int x = 0; x < maxTileX; x++)
                {
                    if (mapXY[x, y] == pos)
                    {
                        useMapXY[x, y] = true;
                        return;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"MapType '{mapType}' 입력된 이름이 아님!");
        }
    }
    #endregion


    #region 타일에 아이템 생성
    public void Create_TileItem(Vector3Int targetPos)
    {
        // 타일에 아이템 생성
        Vector3 worldPos = mainTilemap.GetCellCenterWorld(targetPos);

        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.1f);
        bool hasSameTag = false;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Tileitem")) // 프리팹에 이 태그 설정 필수
            {
                hasSameTag = true;
                break;
            }
        }

        // 아이템 생성이 확정된다면 랜덤확률로 아이템 생성
        if (!hasSameTag)
        {
            if (Random.Range(0, 100) > itemSpawnChance) return;

            int totalChance = tileItems.Sum(t => t.spawnChance);
            int roll = Random.Range(0, totalChance);

            int current = 0;
            foreach (var item in tileItems)
            {
                current += item.spawnChance;
                if (roll < current)
                {
                    Instantiate(item.prefab, worldPos, Quaternion.identity, parentTrans);
                    break;
                }
            }
        }

    }
    #endregion


    #region 스폰맵 생성
    private void SpawnMap_Instantiate()
    {
        //캐릭터 스폰 맵 생성하기
        removeMapX = Random.Range(0, maxTileX); // x값 랜덤 생성하여 y0에 시작맵 생성
        Vector2 spawnPos = mapXY[removeMapX, 0];
        Create_Map("C", 0, spawnPos.x, spawnPos.y);
    }
    #endregion


    #region 원하는 맵 기준 왼쪽, 오른쪽 선택 및 저장
    private void LR_RandomChoose(int removeTile) // 원하는 맵 기준으로 왼쪽, 오른쪽 맵 위치를 정하기
    {
        LR_Choose.Clear(); // 이전에 저장된 리스트 초기화

        bool is_LR = Random.Range(0, 2) == 0; // 왼쪽 오른쪽 정하기

        if (is_LR && removeTile > 0)
        {
            for (int i = 0; i < removeTile; i++) // 왼쪽 후보
                LR_Choose.Add(i);
        }
        else if (LR_Choose.Count == 0)
        {
            for (int i = removeTile + 1; i < maxTileX; i++) // 오른쪽 후보
                LR_Choose.Add(i);
        }

        if (!is_LR && removeTile != maxTileX - 1)
        {
            for (int i = removeTile + 1; i < maxTileX; i++) // 오른쪽 후보
                LR_Choose.Add(i);
        }
        else if (LR_Choose.Count == 0)
        {
            for (int i = 0; i < removeTile; i++) // 왼쪽 후보
                LR_Choose.Add(i);
        }
    }
    #endregion


    #region 원하는 맵 기준 탈출 맵 생성 및 사이를 양옆이 뚥린 맵으로 채움
    private void DownExit_Map_Instantiate(int removeTile)
    {
        for (int y = 0; y < maxTileY; y++)
        {
            LR_RandomChoose(removeTile); // 왼쪽, 오른쪽 맵 위치 정하기

            // 좌우 선택된 위치 중에서 랜덤으로 탈출 맵 생성
            int exitDownMap = LR_Choose[Random.Range(0, LR_Choose.Count)];
            Vector2 exitPos = mapXY[exitDownMap, y];

            if (y != maxTileY - 1) // 마지막 y값이 아닐 때는 아래 탈출구가 확정된 맵 생성
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
                Vector2 fillPos = mapXY[x, y];
                Create_Map("LR", 0, fillPos.x, fillPos.y); // 좌우만 뚫린 맵으로 채움
            }



            // 탈출구 y값에 다음 탈출구가 확정 되어있는 타일 생성

            if (y + 1 < maxTileY)
            {
                Vector2 nextExit_Pos = mapXY[exitDownMap, y + 1];

                if (exitDownMap != Random.Range(0, maxTileX)) // 랜덤값이 현재 탈출 맵과 같지 않다면 좌,우 탈출구가 확정인 맵 생성
                {
                    Create_Map("W", 0, nextExit_Pos.x, nextExit_Pos.y);
                    removeTile = exitDownMap;
                }
                else
                {
                    Create_Map("WD", 0, nextExit_Pos.x, nextExit_Pos.y); // 랜덤값이 현재 탈출 맵과 같다면 위,아래가 탈출구가 확정인 맵 생성
                    removeTile = exitDownMap;

                    if (y + 2 < maxTileY) // 이 안에서 또 다음 타일 확인
                    {
                        Vector2 deeperExit_Pos = mapXY[exitDownMap, y + 2];
                        Create_Map("W", 0, deeperExit_Pos.x, deeperExit_Pos.y); // 다음 타일에 dkfo 탈출구가 확정인 맵 생성
                        removeTile = exitDownMap;
                        y++;
                    }
                }
            }

        }
    }
    #endregion


    #region 빈 공간에 특별한 맵 생성
    private void Create_Special_Map(int Map_Number, int Percent)
    {
        if (Random.Range(0, 100) < Percent)
        {
            const int maxAttempts = 100; // 극악의 확률이지만 모든 맵이 차면 오류가 나기에 최대 100번 시도합니다.
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int x = Random.Range(0, maxTileX);
                int y = Random.Range(0, maxTileY);

                if (!useMapXY[x, y])
                {
                    Vector2 emptyPos = mapXY[x, y];
                    Create_Map("S", Map_Number, emptyPos.x, emptyPos.y);
                    break; // 빈 공간에 성공적으로 생성했으니 반복 종료
                }
            }
        }
    }
    #endregion


    #region 빈 공간에 랜덤 맵 채우기
    private void Create_EmptyMap()
    {
        for (int y = 0; y < maxTileY; y++)
        {
            for (int x = 0; x < maxTileX; x++)
            {
                if (!useMapXY[x, y]) // 해당 위치에 맵이 생성되지 않았다면
                {
                    Vector2 emptyPos = mapXY[x, y];
                    Create_Map("LR", 0, emptyPos.x, emptyPos.y); // 좌우가 확정인 맵 생성 (나중에 올 랜덤으로 바꾸기)
                }
            }
        }
    }
    #endregion
}