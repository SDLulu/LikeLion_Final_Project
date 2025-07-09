using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Collections; // Coroutine을 위해 추가

public class PMK_Tile_Rogic : MonoBehaviour
{
    [Header("타일 길이")]
    [SerializeField] private int _MapX = 10; // 맵의 가로 기준 길이
    [SerializeField] private int _MapY = 10; // 맵의 세로 기준 길이
    [SerializeField] private int _MaxMapSize = 5; // 맵의 최대 크기 배율

    [Header("타일 오브젝트")]
    [SerializeField] private Tilemap _TargetTilemap; // 타일을 배치할 Tilemap 컴포넌트
    [SerializeField] private TileBase _LandTile;

    private List<Vector3Int>[] _LY_Tiles;
    private List<Vector3Int>[] _RY_Tiles;
    private List<Vector3Int>[] _DX_Tiles;


    private void Awake()
    {
        if (_TargetTilemap == null)
        {
            Debug.LogError("Error: Target Tilemap이 할당되지 않았습니다. 인스펙터에서 할당해주세요.");
            return;
        }
        if (_LandTile == null)
        {
            Debug.LogError("Error: Land Tile이 할당되지 않았습니다. 인스펙터에서 할당해주세요.");
            return;
        }

        _LY_Tiles = new List<Vector3Int>[_MaxMapSize * _MaxMapSize]; // x * y 크기 타일의 리스트를 생성합니다.
        _RY_Tiles = new List<Vector3Int>[_MaxMapSize * _MaxMapSize]; // x * y 크기 타일의 리스트를 생성합니다.
        _DX_Tiles = new List<Vector3Int>[_MaxMapSize * _MaxMapSize]; // x * y 크기 타일의 리스트를 생성합니다.


        for (int i = 0; i < _MaxMapSize * _MaxMapSize; i++)
        {
            _LY_Tiles[i] = new List<Vector3Int>(); // 각 배열 요소를 List<Vector3Int>로 초기화
            _RY_Tiles[i] = new List<Vector3Int>(); // 각 배열 요소를 List<Vector3Int>로 초기화
            _DX_Tiles[i] = new List<Vector3Int>(); // 각 배열 요소를 List<Vector3Int>로 초기화
        }
    }

    private void Start()
    {
        CreateMap();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) // 맵을 다시 생성하는 키 입력 (1번)
        {
            CreateMap();
            Debug.Log("맵 다시 생성");
        }
    }

    private void CreateMap()
    {
        _TargetTilemap.ClearAllTiles(); // 기존 타일맵을 초기화합니다.

        // 전체 맵의 최종 크기를 계산합니다.
        int maxMapX = _MapX * _MaxMapSize;
        int maxMapY = _MapY * _MaxMapSize;

        for (int i = 0; i < _MaxMapSize; i++)
        {
            for (int x = 0; x < maxMapX; x++) // x축을 최종 확장된 크기만큼 만듭니다.
            {
                for (int y = i * _MapY; y < (i + 1) * _MapY; y++) // 현재 y 확장 단계에 맞는 y 범위
                {
                    // 맵의 전체 높이를 넘지 않도록 안전 장치 추가 (필수)
                    if (y >= maxMapY)
                    {
                        continue;
                    }

                    Vector3Int cellPosition = new Vector3Int(x, y, 0); // x, y 좌표를 사용하여 타일의 위치를 설정합니다.
                    _TargetTilemap.SetTile(cellPosition, _LandTile); // 위치를 설정한 타일을 타일맵에 배치합니다.
                }
            }
        }

        //StartCoroutine(LY_ExitTile()); // 왼쪽 y축 타일 제거 코루틴 시작
        //StartCoroutine(RY_ExitTile()); // 오른쪽 y축 타일 제거 코루틴 시작
        StartCoroutine(DX_ExitTile()); // 아래 X축 타일 제거 코루틴 시작
    }

    #region 왼쪽 y축 타일 제거 코루틴
    private IEnumerator LY_ExitTile() // 왼쪽 y축 타일 제거 코루틴
    {
        int a = 0;

        for (int i = 0; i < _MaxMapSize; i++)
        {
            int targetX = 0; // 첫번째 열을 마무리했다면 0으로 초기화시켜 두번째 열의 첫 칸부터 계산되게 합니다.


            for (int x = 0; x < _MaxMapSize; x++)
            {
                targetX = x * _MapX; // 각 칸의 첫번째 열의 x 좌표를 지정합니다.

                if (x > 0) // x가 0이 아닐 때만 a를 증가시킵니다. (첫번째 열은 제외)
                {
                    a += 1; // a를 증가시켜 _LY_Tiles을 순서대로 저장시킵니다.
                }

                _LY_Tiles[a].Clear(); // 이전에 저장된 타일 위치를 초기화합니다。


                for (int y = i * _MapY; y < (i + 1) * _MapY; y++) // y축이 완료되면 i의 값만큼 곱하여 위에서 부터 생성합니다.
                {
                    Vector3Int cellPosition = new Vector3Int(targetX, y, 0); // 첫번째 열의 x 좌표값과 y 좌표값을 저장합니다.

                    if (_TargetTilemap.GetTile(cellPosition) != null) // 타일이 존재하는지 확인합니다.
                    {
                        if (a >= 0 && a < _LY_Tiles.Length)
                        {
                            _LY_Tiles[a].Add(cellPosition); // 각 칸의 타일 위치를 저장합니다.
                        }
                    }
                }

                if (_LY_Tiles[a].Count > 0)
                {
                    int randomY_Tile = Random.Range(0, _LY_Tiles[a].Count); // 저장된 LY_Tiles[a]에서 랜덤한 타일을 선택합니다.
                    Vector3Int deleteY_Tile1 = _LY_Tiles[a][randomY_Tile];
                    _TargetTilemap.SetTile(deleteY_Tile1, null); // 랜덤 타일 제거
                    Debug.Log($"블록에서 랜덤하게 한 타일 제거: {deleteY_Tile1}");
                }

                yield return null;
            }
        }
    }
    #endregion

    #region 오른쪽 y축 타일 제거 코루틴
    private IEnumerator RY_ExitTile() // 오른쪽 y축 타일 제거 코루틴
    {
        int a = 0;

        for (int i = 0; i < _MaxMapSize; i++)
        {
            int targetX = 9; // 첫번째 열을 마무리했다면 0으로 초기화시켜 두번째 열의 첫 칸부터 계산되게 합니다.


            for (int x = 0; x < _MaxMapSize; x++)
            {

                if (x > 0) // x가 0이 아닐 때만 a를 증가시킵니다. (첫번째 열은 제외)
                {
                    targetX = (x + 1) * _MapX - 1; // 각 칸의 첫번째 열의 x 좌표를 지정합니다.
                    a += 1; // a를 증가시켜 _LY_Tiles을 순서대로 저장시킵니다.
                }

                _RY_Tiles[a].Clear();


                for (int y = i * _MapY; y < (i + 1) * _MapY; y++) // y축이 완료되면 i의 값만큼 곱하여 위에서 부터 생성합니다.
                {
                    Vector3Int cellPosition = new Vector3Int(targetX, y, 0); // 첫번째 열의 x 좌표값과 y 좌표값을 저장합니다.

                    if (_TargetTilemap.GetTile(cellPosition) != null) // 타일이 존재하는지 확인합니다.
                    {
                        if (a >= 0 && a < _RY_Tiles.Length)
                        {
                            _RY_Tiles[a].Add(cellPosition); // 각 칸의 타일 위치를 저장합니다.
                        }
                    }
                }

                if (_RY_Tiles[a].Count > 0)
                {
                    int randomY_Tile = Random.Range(0, _RY_Tiles[a].Count); // 저장된 LY_Tiles[a]에서 랜덤한 타일을 선택합니다.
                    Vector3Int deleteY_Tile1 = _RY_Tiles[a][randomY_Tile];
                    _TargetTilemap.SetTile(deleteY_Tile1, null); // 랜덤 타일 제거
                    Debug.Log($"RY 블록에서 랜덤하게 한 타일 제거: {deleteY_Tile1}");
                }

                yield return null;
            }
        }
    }
    #endregion

    #region 아래쪽 X축 타일 제거 코루틴
    private IEnumerator DX_ExitTile() // 아래 X축 타일 제거 코루틴
    {
        int a = 0;

        for (int i = 0; i < _MaxMapSize; i++)
        {
            int targetY = 0; // 첫번째 열을 마무리했다면 0으로 초기화시켜 두번째 열의 첫 칸부터 계산되게 합니다.


            for (int y = 0; y < _MaxMapSize; y++)
            {
                targetY = y * _MapX; // 각 칸의 첫번째 열의 x 좌표를 지정합니다.
                int targetX = 0;

                if (y > 0) // x가 0이 아닐 때만 a를 증가시킵니다. (첫번째 열은 제외)
                {
                    a += 1; // a를 증가시켜 _LY_Tiles을 순서대로 저장시킵니다.
                }

                _DX_Tiles[a].Clear(); // 이전에 저장된 타일 위치를 초기화합니다。

                for (int u = 0; u < _MaxMapSize; u++)
                {
                    targetX = u * _MapX;

                    for (int x = i * _MapY; x < (i + 1) * _MapY; x++) // y축이 완료되면 i의 값만큼 곱하여 위에서 부터 생성합니다.
                    {
                        Vector3Int cellPosition = new Vector3Int(x + targetX, targetY, 0); // 첫번째 열의 x 좌표값과 y 좌표값을 저장합니다.

                        if (_TargetTilemap.GetTile(cellPosition) != null) // 타일이 존재하는지 확인합니다.
                        {
                            if (a >= 0 && a < _DX_Tiles.Length)
                            {
                                _DX_Tiles[a].Add(cellPosition); // 각 칸의 타일 위치를 저장합니다.
                            }
                        }
                    }
                }

                if (_DX_Tiles[a].Count > 0)
                {
                    int randomY_Tile = Random.Range(0, _DX_Tiles[a].Count); // 저장된 LY_Tiles[a]에서 랜덤한 타일을 선택합니다.
                    Vector3Int deleteY_Tile1 = _DX_Tiles[a][randomY_Tile];
                    _TargetTilemap.SetTile(deleteY_Tile1, null); // 랜덤 타일 제거
                    Debug.Log($"블록에서 랜덤하게 한 타일 제거: {deleteY_Tile1}");
                }

                yield return null;
            }
        }
    }
    #endregion
}