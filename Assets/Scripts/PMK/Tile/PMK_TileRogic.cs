using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public partial class PMK_TileRogic : MonoBehaviour
{
     public static PMK_TileRogic Instance { get; private set; }


    [field: SerializeField] public Transform parentTrans { get; private set; } // Ÿ�� ������Ʈ�� �θ�� ������ Ʈ������ (���� �����ϴ� Ÿ�� ������Ʈ�� �θ� Ʈ������)
    [field: SerializeField] public Tilemap mainTilemap { get; private set; } // ������ Ÿ�ϸ� (���� �����ϴ� Ÿ�ϸ�)


    [Header("������ Ÿ�� ����")]
    [SerializeField] private int maxTileX = 5; // ���� ���� ����
    [SerializeField] private int maxTileY = 5; // ���� ���� ����

    [Header("���� Ÿ�ϰ��� �Ÿ�")]
    [SerializeField] private float nextTileX = 17; // ���� ���� ���� ����
    [SerializeField] private float nextTileY = 11; // ���� ���� ���� ����


    [Header("Ÿ�� ������Ʈ")]
    [SerializeField] private GameObject[] Clear_Map_Prefab;        // �������� or Ŭ���� �� ���� (�������� : ��,�� Ȯ��) (Ŭ���� : ��,��,�� Ȯ��)
    [SerializeField] private GameObject[] LR_Exit_Map_Prefab;      // ��,�� �ⱸ�� Ȯ���� �� ���� (��,�Ʒ� ����)
    [SerializeField] private GameObject[] D_Exit_Map_Prefab;       // �Ʒ� �ⱸ�� Ȯ���� �� ���� (��,��,�� ����)
    [SerializeField] private GameObject[] W_Exit_Map_Prefab;       // �Ʒ� �ⱸ�� Ȯ���� �� ���� (��,��,�� ����)
    [SerializeField] private GameObject[] WD_Exit_Map_Prefab;      // ��,�Ʒ� �ⱸ�� Ȯ���� �� ���� (��,�� ����)
    [SerializeField] private GameObject[] Special_Map_Prefab;      // �����̳� Ư���� �� ���� (��,�� Ȯ��)
    private Dictionary<string, GameObject[]> mapPrefabDict; //Ÿ�� �̸� ����


    [Header("Ÿ�� ������ ������Ʈ")]

    [SerializeField] private int itemSpawnChance = 35; // ������ ���� Ȯ�� (0.0f ~ 1.0f) - 50% Ȯ���� ������ ����
    [SerializeField] private List<PMK_TileTable> tileItems;





    private Vector2[,] mapXY; // �� Ÿ�� ��ġ ����� 2���� �迭
    private bool[,] useMapXY; // �� Ÿ���� �����Ǿ����� ���θ� �����ϴ� 2���� �迭

    private int removeMapX; // ���ϰ� ���� �ʴ� ���� X��ġ�� �����մϴ�.
    private List<int> LR_Choose = new List<int>(); // ����, ������ �� ��ġ�� �����ϴ� ����Ʈ �Դϴ�.


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
        // SaveMapPos(); // ��ü �� ��ġ �����ϱ�

        // ResetMap();

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) // 1��Ű�� ������ �� ���� �ʱ�ȭ�ϰ� �ٽ� �����մϴ�.
        {
            ResetMap();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) // 2��Ű�� ������ �� �� ������ ���� ���� ä��ϴ�.
        {
            Create_EmptyMap();
        }
    }


    #region �� �ʱ�ȭ �� �����
    public void ResetMap()
    {
        mainTilemap.ClearAllTiles();

        foreach (Transform child in parentTrans)
        {
            if (child.GetComponent<PMK_TileRogic>() != null)
                continue;

            Destroy(child.gameObject);
        }

        // �� �ٽ� ����
        SaveMapPos();
        SpawnMap_Instantiate();
        DownExit_Map_Instantiate(removeMapX);
        Create_Special_Map(0, 10);
        Create_EmptyMap();
    }
    #endregion


    #region ��ü �� ��ġ ����
    private void SaveMapPos()
    {
        // ���� �� ũ�� ��ŭ Ÿ����ġ�� mapX�� ����

        mapXY = new Vector2[maxTileX, maxTileY];
        useMapXY = new bool[maxTileX, maxTileY];

        for (int y = 0; y < maxTileY; y++)
        {
            for (int x = 0; x < maxTileX; x++)
            {
                mapXY[x, y] = new Vector2(x * nextTileX, y * -nextTileY);
                useMapXY[x, y] = false;
                Debug.Log($"x: {x}, y: {y} �� pos: {mapXY[x, y]}");
            }
        }
    }
    #endregion


    #region ���ϴ� �� ����
    private void Create_Map(string mapType, int randomIndex, float spawnXpos, float spawnYpos)
    {
        if (mapPrefabDict.TryGetValue(mapType, out GameObject[] prefabs))
        {
            if (mapType != "C") //Ŭ������� �ƴ϶�� ��� ���� ������
            {
                randomIndex = Random.Range(0, prefabs.Length);
            }
            GameObject temp = Instantiate(prefabs[randomIndex], Vector3.zero, Quaternion.identity);

            Tilemap[] tilemaps = temp.GetComponentsInChildren<Tilemap>();
            Vector3Int offset = new Vector3Int((int)spawnXpos, (int)spawnYpos, 0);

            // --- Ÿ�� ���� ---
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


                            // Ÿ�� ������ ����
                            Create_TileItem(targetPos);
                        }
                    }
                }
            }

            // --- ������Ʈ ���� ---
            foreach (Transform child in temp.transform)
            {
                // Ÿ�ϸ��� �ƴ� �Ϲ� ������Ʈ�� ����
                if (child.GetComponent<Tilemap>() == null)
                {
                    // Ÿ�ϸ� ���� ��ǥ��� ���յ� ��ġ ���
                    Vector3 spawnPosition = child.position + new Vector3(offset.x, offset.y, 0f);

                    GameObject clone = Instantiate(child.gameObject, spawnPosition, child.rotation, parentTrans);
                    clone.name = child.name; // �̸� ���� (����� ����)
                }
            }

            Destroy(temp); // �ӽ� ������ ����
            mainTilemap.RefreshAllTiles(); // Ÿ�ϸ� �ֽ�ȭ

            // ��ǥ ���
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
            Debug.LogWarning($"MapType '{mapType}' �Էµ� �̸��� �ƴ�!");
        }
    }
    #endregion


    #region Ÿ�Ͽ� ������ ����
    public void Create_TileItem(Vector3Int targetPos)
    {
        // Ÿ�Ͽ� ������ ����
        Vector3 worldPos = mainTilemap.GetCellCenterWorld(targetPos);

        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 0.1f);
        bool hasSameTag = false;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Tileitem")) // �����տ� �� �±� ���� �ʼ�
            {
                hasSameTag = true;
                break;
            }
        }

        // ������ ������ Ȯ���ȴٸ� ����Ȯ���� ������ ����
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

    public Vector2 StartPos {get; private set;}

    #region ������ ����
    private void SpawnMap_Instantiate()
    {
        //ĳ���� ���� �� �����ϱ�
        removeMapX = Random.Range(0, maxTileX); // x�� ���� �����Ͽ� y0�� ���۸� ����
        Vector2 spawnPos = mapXY[removeMapX, 0];
        Create_Map("C", 0, spawnPos.x, spawnPos.y);
        StartPos = spawnPos;
    }
    #endregion


    #region ���ϴ� �� ���� ����, ������ ���� �� ����
    private void LR_RandomChoose(int removeTile) // ���ϴ� �� �������� ����, ������ �� ��ġ�� ���ϱ�
    {
        LR_Choose.Clear(); // ������ ����� ����Ʈ �ʱ�ȭ

        bool is_LR = Random.Range(0, 2) == 0; // ���� ������ ���ϱ�

        if (is_LR && removeTile > 0)
        {
            for (int i = 0; i < removeTile; i++) // ���� �ĺ�
                LR_Choose.Add(i);
        }
        else if (LR_Choose.Count == 0)
        {
            for (int i = removeTile + 1; i < maxTileX; i++) // ������ �ĺ�
                LR_Choose.Add(i);
        }

        if (!is_LR && removeTile != maxTileX - 1)
        {
            for (int i = removeTile + 1; i < maxTileX; i++) // ������ �ĺ�
                LR_Choose.Add(i);
        }
        else if (LR_Choose.Count == 0)
        {
            for (int i = 0; i < removeTile; i++) // ���� �ĺ�
                LR_Choose.Add(i);
        }
    }
    #endregion


    #region ���ϴ� �� ���� Ż�� �� ���� �� ���̸� �翷�� �丰 ������ ä��
    private void DownExit_Map_Instantiate(int removeTile)
    {
        for (int y = 0; y < maxTileY; y++)
        {
            LR_RandomChoose(removeTile); // ����, ������ �� ��ġ ���ϱ�

            // �¿� ���õ� ��ġ �߿��� �������� Ż�� �� ����
            int exitDownMap = LR_Choose[Random.Range(0, LR_Choose.Count)];
            Vector2 exitPos = mapXY[exitDownMap, y];

            if (y != maxTileY - 1) // ������ y���� �ƴ� ���� �Ʒ� Ż�ⱸ�� Ȯ���� �� ����
            {
                Create_Map("D", 0, exitPos.x, exitPos.y);
            }
            else // ������ y���� ���� Ŭ���� �� ����
            {
                Create_Map("C", 1, exitPos.x, exitPos.y);
            }

            // ���ϴ� �ʰ� Ż�� �� ���̸� �ո� ������ ä���
            int min = Mathf.Min(removeTile, exitDownMap);
            int max = Mathf.Max(removeTile, exitDownMap);

            for (int x = min + 1; x < max; x++)
            {
                Vector2 fillPos = mapXY[x, y];
                Create_Map("LR", 0, fillPos.x, fillPos.y); // �¿츸 �ո� ������ ä��
            }



            // Ż�ⱸ y���� ���� Ż�ⱸ�� Ȯ�� �Ǿ��ִ� Ÿ�� ����

            if (y + 1 < maxTileY)
            {
                Vector2 nextExit_Pos = mapXY[exitDownMap, y + 1];

                if (exitDownMap != Random.Range(0, maxTileX)) // �������� ���� Ż�� �ʰ� ���� �ʴٸ� ��,�� Ż�ⱸ�� Ȯ���� �� ����
                {
                    Create_Map("W", 0, nextExit_Pos.x, nextExit_Pos.y);
                    removeTile = exitDownMap;
                }
                else
                {
                    Create_Map("WD", 0, nextExit_Pos.x, nextExit_Pos.y); // �������� ���� Ż�� �ʰ� ���ٸ� ��,�Ʒ��� Ż�ⱸ�� Ȯ���� �� ����
                    removeTile = exitDownMap;

                    if (y + 2 < maxTileY) // �� �ȿ��� �� ���� Ÿ�� Ȯ��
                    {
                        Vector2 deeperExit_Pos = mapXY[exitDownMap, y + 2];
                        Create_Map("W", 0, deeperExit_Pos.x, deeperExit_Pos.y); // ���� Ÿ�Ͽ� dkfo Ż�ⱸ�� Ȯ���� �� ����
                        removeTile = exitDownMap;
                        y++;
                    }
                }
            }

        }
    }
    #endregion


    #region �� ������ Ư���� �� ����
    private void Create_Special_Map(int Map_Number, int Percent)
    {
        if (Random.Range(0, 100) < Percent)
        {
            const int maxAttempts = 100; // �ؾ��� Ȯ�������� ��� ���� ���� ������ ���⿡ �ִ� 100�� �õ��մϴ�.
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int x = Random.Range(0, maxTileX);
                int y = Random.Range(0, maxTileY);

                if (!useMapXY[x, y])
                {
                    Vector2 emptyPos = mapXY[x, y];
                    Create_Map("S", Map_Number, emptyPos.x, emptyPos.y);
                    break; // �� ������ ���������� ���������� �ݺ� ����
                }
            }
        }
    }
    #endregion


    #region �� ������ ���� �� ä���
    private void Create_EmptyMap()
    {
        for (int y = 0; y < maxTileY; y++)
        {
            for (int x = 0; x < maxTileX; x++)
            {
                if (!useMapXY[x, y]) // �ش� ��ġ�� ���� �������� �ʾҴٸ�
                {
                    Vector2 emptyPos = mapXY[x, y];
                    Create_Map("LR", 0, emptyPos.x, emptyPos.y); // �¿찡 Ȯ���� �� ���� (���߿� �� �������� �ٲٱ�)
                }
            }
        }
    }
    #endregion


    public void DestoryTile(Vector3 Pos)
    {
        Debug.DrawRay(Pos, Vector2.up * 0.2f, Color.red, 1f);

        Vector3Int cellPosition = mainTilemap.WorldToCell(Pos);

        if (mainTilemap.HasTile(cellPosition))
        {
            mainTilemap.SetTile(cellPosition, null);  // Ÿ�� ����
            mainTilemap.RefreshTile(cellPosition);
        }
    }
}