using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;
using Fusion;

// 맵 생성을 호스트가 담당하고, 클라이언트는 호스트가 생성한 맵을 받아서 타일맵에 추가하는 구조입니다.
// 맵 프리팹에는 네트워크 오브젝트가 포함되어있지 않습니다.




[System.Serializable]
public class MapPrefabSet
{
    public string mapType;
    public GameObject[] prefabs;
}

public partial class PMK_TileRogic : NetworkBehaviour
{
    public static PMK_TileRogic Instance { get; private set; }
    private PMK_TileRPC_Manager tileRPCManager => PMK_TileRPC_Manager.Instance; // 타일 RPC 매니저 인스턴스 (타일 아이템 생성 및 파괴를 담당)


    [field: SerializeField] public Transform parentTrans { get; private set; } // 부모 오브젝트 (맵 생성시 자식으로 추가됨)
    [field: SerializeField] public Tilemap mainTilemap { get; private set; } // 메인 타일맵 (맵 생성시 타일을 추가하는 타일맵)

    [Header("최대 타일 설정")]
    [SerializeField] private int maxTileX = 5; // x위치에 생성할 최대 타일값
    [SerializeField] private int maxTileY = 5; // y위치에 생성할 최대 타일값

    [Header("타일 사이의 거리 설정")]
    [SerializeField] private float nextTileX = 17; // 다음 타일까지 넘어갈 x위치
    [SerializeField] private float nextTileY = 11; // 다음 타일까지 넘어갈 y위치


    [Header("맵 프리팹 설정")]
    [SerializeField] private MapPrefabSet[] mapPrefabSets;
    private Dictionary<string, GameObject[]> mapPrefabDict;


    [Header("타일 아이템 설정")]
    [SerializeField] private int itemSpawnChance = 35; // 타일안에 아이템 생성 확률 (0~100 사이의 값, 0은 생성 안함, 100은 항상 생성됨)
    [field: SerializeField] public List<PMK_TileTable> tileItems { get; private set; }


    [Header("TileZoneSpawner 설정")]
    [field: SerializeField] public TileBase ruleTile { get; private set; }// 룰 타일 (PMK_TileZoneSpawner에서 사용되는 룰 타일)
    [field: SerializeField] public GameObject[] trap { get; private set; } // 함정 타일 (PMK_TileZoneSpawner에서 사용되는 함정 타일) 0. 즉사함정, 1. 돌함정


    private Vector2[,] mapXY; // 전체 맵의 위치를 저장하기 위한 2차원 배열 (x, y 좌표에 해당하는 위치를 저장)
    private bool[,] useMapXY; // 전체 맵의 위치가 사용되었는지 여부를 저장하기 위한 2차원 배열 (true: 사용됨, false: 사용되지 않음)

    private int removeMapX; // 선택할 맵의 기준이 되는 x좌표
    private List<int> LR_Choose = new List<int>(); // 왼쪽, 오른쪽 선택을 위한 리스트 (탈출 맵 생성 시 좌우를 선택하기 위한 리스트)



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // 싱글톤 패턴을 위해 중복 생성 방지
        }
    }


    public override void Spawned()
    {
        SaveMapPos(); // 전체 맵 위치 저장

        if (mapPrefabDict == null)
        {
            mapPrefabDict = new Dictionary<string, GameObject[]>();

            foreach (var set in mapPrefabSets)
            {
                if (!mapPrefabDict.ContainsKey(set.mapType))
                    mapPrefabDict.Add(set.mapType, set.prefabs);
            }
        }
        if (!HasStateAuthority) return;
        RPC_ResetMap(); // 맵 초기화 및 재생성
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Alpha1)) // 1번 키를 누르면 맵 초기화 및 재생성
        {
            if (!HasStateAuthority) return;
            RPC_ResetMap();
        }
    }


    #region 맵 초기화 및 재생성
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetMap()
    {
        ResetMap();
    }


    public void ResetMap()
    {
        mainTilemap.ClearAllTiles();

        foreach (Transform child in parentTrans)
        {
            if (child.GetComponent<PMK_TileRogic>() != null)
                continue;

            Destroy(child.gameObject);
        }

        // 초기화
        SaveMapPos();
        SpawnMap_Instantiate();
        DownExit_Map_Instantiate(removeMapX);
        Create_Special_Map(0, 10);
        Create_EmptyMap();
        mainTilemap.RefreshAllTiles();
    }
    #endregion


    #region 전체 맵 위치 저장
    private void SaveMapPos()
    {
        mapXY = new Vector2[maxTileX, maxTileY];
        useMapXY = new bool[maxTileX, maxTileY];

        for (int y = 0; y < maxTileY; y++)
        {
            for (int x = 0; x < maxTileX; x++)
            {
                mapXY[x, y] = new Vector2(x * nextTileX, y * -nextTileY);
                useMapXY[x, y] = false;
            }
        }
    }
    #endregion


    #region 원하는 맵 생성
    private void Create_Map(string mapType, int randomIndex, float spawnXpos, float spawnYpos)
    {
        if (!HasStateAuthority) return;

        if (mapPrefabDict.TryGetValue(mapType, out GameObject[] prefabs))
        {
            randomIndex = mapType != "C" ? Random.Range(0, prefabs.Length) : 0;

            RPC_Create_Map(mapType, randomIndex, spawnXpos, spawnYpos);
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Create_Map(string mapType, int randomIndex, float spawnXpos, float spawnYpos)
    {
        if (mapPrefabDict.TryGetValue(mapType, out GameObject[] prefabs))
        {
            GameObject temp = Instantiate(prefabs[randomIndex], Vector3.zero, Quaternion.identity); // 맵 프리팹 저장
            Tilemap[] tilemaps = temp.GetComponentsInChildren<Tilemap>(); // 타일맵 컴포넌트 가져오기
            Vector3Int offset = new Vector3Int((int)spawnXpos, (int)spawnYpos, 0); // 생성할 위치 저장

            // 생성할 위치가 타일맵의 셀 좌표로 변환
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

                            // 랜덤한 확률로 아이템 생성
                            Create_TileItem(targetPos);
                        }
                    }
                }
            }

            // 생성된 맵을 부모 오브젝트에 자식으로 추가
            foreach (Transform child in temp.transform)
            {
                if (child.GetComponent<Tilemap>() == null)
                {
                    Vector3 spawnPosition = child.position + new Vector3(offset.x, offset.y, 0f);

                    Instantiate(child.gameObject, spawnPosition, child.rotation, parentTrans); // 자식으로 추가
                    child.name = child.name; // 이름을 원본과 동일하게 설정
                }
            }

            // 원래 프리팹은 삭제 (타일맵에 추가를 하였으므로)
            Destroy(temp);


            // 생성된 맵의 위치를 useMapXY에 저장
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
    }
    #endregion


    #region 타일에 아이템 생성
    // 호스트가 타일 랜덤값을 적용후 공유함
    public void Create_TileItem(Vector3Int targetPos)
    {
        if (!HasStateAuthority) return;

        if (Random.Range(0, 100) > itemSpawnChance) return;

        Vector3 worldPos = mainTilemap.GetCellCenterWorld(targetPos); // 타일의 월드 좌표로 변환

        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.1f); // 타일 위치에 있는 모든 콜라이더를 가져옴
        bool hasSameTag = hits.Any(hit => hit.gameObject.layer == LayerMask.NameToLayer("Item")); // 아이템 레이어에 해당하는지 확인

        if (!hasSameTag)
        {
            int totalChance = tileItems.Sum(t => t.spawnChance);
            int roll = Random.Range(0, totalChance);
            int current = 0;

            foreach (var item in tileItems)
            {
                current += item.spawnChance;
                if (roll < current)
                {
                    int index = tileItems.IndexOf(item);
                    tileRPCManager.RPC_Create_TileItem(index, worldPos);
                    break;
                }
            }
        }

    }
    #endregion


    #region 스폰맵 생성
    private void SpawnMap_Instantiate()
    {
        removeMapX = Random.Range(0, maxTileX);
        Vector2 spawnPos = mapXY[removeMapX, 0];
        Create_Map("C", 0, spawnPos.x, spawnPos.y);
    }
    #endregion


    #region 원하는 맵 기준 왼쪽, 오른쪽 선택 및 저장
    private void LR_RandomChoose(int removeTile)
    {
        LR_Choose.Clear(); // 기존 결과값 제거

        bool is_LR = Random.Range(0, 2) == 0;

        if (is_LR && removeTile > 0) // 왼쪽 선택
        {
            for (int i = 0; i < removeTile; i++)
                LR_Choose.Add(i);
        }
        else if (LR_Choose.Count == 0) // 오른쪽 선택
        {
            for (int i = removeTile + 1; i < maxTileX; i++)
                LR_Choose.Add(i);
        }

        if (!is_LR && removeTile != maxTileX - 1) // 왼쪽에 공간이 없다면 오른쪽 선택
        {
            for (int i = removeTile + 1; i < maxTileX; i++)
                LR_Choose.Add(i);
        }
        else if (LR_Choose.Count == 0)
        {
            for (int i = 0; i < removeTile; i++) // 오른쪽에 공간이 없다면 왼쪽 선택
                LR_Choose.Add(i);
        }
    }
    #endregion


    #region 원하는 맵 기준 탈출 맵 생성 및 사이를 양옆이 뚤린 맵으로 채움
    private void DownExit_Map_Instantiate(int removeTile)
    {
        for (int y = 0; y < maxTileY; y++)
        {
            // 하나의 맵을 기준으로 좌우를 정하여 맵 생성
            LR_RandomChoose(removeTile);
            int exitDownMap = LR_Choose[Random.Range(0, LR_Choose.Count)];
            Vector2 exitPos = mapXY[exitDownMap, y];


            // 더 이상 맵이 없으면 탈출 맵 생성 , 아래에 공간이 없다면 클리어 맵 생성
            if (y != maxTileY - 1)
            {
                Create_Map("D", 0, exitPos.x, exitPos.y);
            }
            else
            {
                Create_Map("C", 1, exitPos.x, exitPos.y);
            }


            // 탈출 맵 사이를 양옆이 뚤린 맵으로 채움
            int min = Mathf.Min(removeTile, exitDownMap);
            int max = Mathf.Max(removeTile, exitDownMap);

            for (int x = min + 1; x < max; x++)
            {
                Vector2 fillPos = mapXY[x, y];
                Create_Map("LR", 0, fillPos.x, fillPos.y);
            }



            // 모든 x값을 랜덤으로 돌리고 탈출맵과 위치가 같을 경우 탈출 맵 아래에 탈출 맵 생성
            if (y + 1 < maxTileY)
            {
                Vector2 nextExit_Pos = mapXY[exitDownMap, y + 1];

                if (exitDownMap != Random.Range(0, maxTileX)) // 탈출 맵과 다음 탈출 맵의 위치가 같지 않다면
                {
                    Create_Map("W", 0, nextExit_Pos.x, nextExit_Pos.y); // 탈출 맵 아래에 탈출 맵 생성
                    removeTile = exitDownMap;
                }
                else
                {
                    Create_Map("WD", 0, nextExit_Pos.x, nextExit_Pos.y); // 탈출 맵 아래에 탈출 맵 생성
                    removeTile = exitDownMap;

                    if (y + 2 < maxTileY) // 탈출 맵 아래에 공간이 있다면 탈출맵 생성
                    {
                        Vector2 deeperExit_Pos = mapXY[exitDownMap, y + 2];
                        Create_Map("W", 0, deeperExit_Pos.x, deeperExit_Pos.y);
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
            const int maxAttempts = 100; // 최대 시도 횟수 (무한 루프 방지용)
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int x = Random.Range(0, maxTileX);
                int y = Random.Range(0, maxTileY);

                // 빈 공간인지 확인 후 특별한 맵 생성
                if (!useMapXY[x, y])
                {
                    Vector2 emptyPos = mapXY[x, y];
                    Create_Map("S", Map_Number, emptyPos.x, emptyPos.y);
                    break;
                }
            }
        }
    }
    #endregion


    #region 빈 공간에 맵 생성
    private void Create_EmptyMap()
    {
        for (int y = 0; y < maxTileY; y++)
        {
            for (int x = 0; x < maxTileX; x++)
            {
                if (!useMapXY[x, y]) // 비어있는 공간에 맵 생성
                {
                    Vector2 emptyPos = mapXY[x, y];
                    Create_Map("LR", 0, emptyPos.x, emptyPos.y); // 좌우가 뚥린 맵 생성
                }
            }
        }
    }
    #endregion
}