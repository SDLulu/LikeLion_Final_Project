using UnityEngine;

// 🪜 사다리 정보 제공 컴포넌트 (오브젝트에 부착)
[DisallowMultipleComponent]
[AddComponentMenu("Platformer/Ladder")]
public class Ladder : MonoBehaviour
{
    [Header("Ladder Info")]
    [Tooltip("사다리의 길이(높이)")]
    public float Length = 2f;

    [Tooltip("일방향 사다리 여부(위에서만 진입 가능)")]
    public bool IsOneWay = false;

    // 사다리 중앙 위치 (기본적으로 transform.position)
    public Vector2 CenterPosition
    {
        get { return transform.position; }
    }

    // 추후 필요시: 사다리 위/아래 끝 좌표 등도 추가 가능
} 