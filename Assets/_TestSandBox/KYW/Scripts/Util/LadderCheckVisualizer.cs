using UnityEngine;

// 🪜 사다리 체크 시각화 전용 컴포넌트
// NetworkBehaviour 문제 해결을 위한 별도 MonoBehaviour
public class LadderCheckVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerLadderCheck ladderCheckComponent;
    [SerializeField] private Transform ladderCheck;
    [SerializeField] private Vector2 ladderCheckSize = new Vector2(0.8f, 1.5f);

    [Header("Visual Settings")]
    [SerializeField] private bool showAlways = true;
    [SerializeField] private Color nearLadderColor = Color.cyan;
    [SerializeField] private Color notNearLadderColor = Color.gray;
    [SerializeField] private Color defaultColor = Color.yellow;

    private void Start()
    {
        // 자동으로 컴포넌트 찾기
        if (ladderCheckComponent == null)
            ladderCheckComponent = GetComponent<PlayerLadderCheck>();
        if (ladderCheck == null)
            ladderCheck = transform;
    }

    private void OnDrawGizmos()
    {
        if (!showAlways || ladderCheck == null) return;
        DrawGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (ladderCheck == null) return;
        DrawGizmo();
        DrawDetailedInfo();
    }

    private void DrawGizmo()
    {
        // 색상 결정
        Color boxColor = defaultColor;
        if (Application.isPlaying && ladderCheckComponent != null)
        {
            boxColor = ladderCheckComponent.IsNearLadder ? nearLadderColor : notNearLadderColor;
        }
        Gizmos.color = boxColor;
        Gizmos.DrawWireCube(ladderCheck.position, ladderCheckSize);
        // 내부 채우기
        Color fillColor = boxColor;
        fillColor.a = 0.2f;
        Gizmos.color = fillColor;
        Gizmos.DrawCube(ladderCheck.position, ladderCheckSize);
    }

    private void DrawDetailedInfo()
    {
#if UNITY_EDITOR
        Vector3 labelPos = ladderCheck.position + Vector3.up * 0.5f;
        string info = "LadderCheck Visualizer\n";
        if (Application.isPlaying && ladderCheckComponent != null)
        {
            info += $"NearLadder: {ladderCheckComponent.IsNearLadder}\n";
        }
        info += $"Size: {ladderCheckSize}";
        UnityEditor.Handles.Label(labelPos, info);
#endif
    }
} 