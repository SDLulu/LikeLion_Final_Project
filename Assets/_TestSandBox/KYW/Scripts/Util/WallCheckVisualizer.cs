using UnityEngine;

// 🧱 벽 체크 시각화 전용 컴포넌트
// NetworkBehaviour 문제 해결을 위한 별도 MonoBehaviour
public class WallCheckVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerWallCheck wallCheckComponent;
    [SerializeField] private Transform leftCheck;
    [SerializeField] private Transform rightCheck;
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.1f, 1.0f);

    [Header("Visual Settings")]
    [SerializeField] private bool showAlways = true;
    [SerializeField] private Color touchingColor = Color.cyan;
    [SerializeField] private Color notTouchingColor = Color.gray;
    [SerializeField] private Color defaultColor = Color.yellow;

    private void Start()
    {
        // 자동으로 컴포넌트 찾기
        if (wallCheckComponent == null)
            wallCheckComponent = GetComponent<PlayerWallCheck>();
        if (leftCheck == null || rightCheck == null)
        {
            // PlayerWallCheck에서 참조 가져오기
            if (wallCheckComponent != null)
            {
                leftCheck = wallCheckComponent.GetType().GetField("leftCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(wallCheckComponent) as Transform;
                rightCheck = wallCheckComponent.GetType().GetField("rightCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(wallCheckComponent) as Transform;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showAlways) return;
        DrawGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmo();
        DrawDetailedInfo();
    }

    private void DrawGizmo()
    {
        // 왼쪽 벽 감지
        Color leftColor = defaultColor;
        if (Application.isPlaying && wallCheckComponent != null)
            leftColor = wallCheckComponent.IsTouchingWallLeft ? touchingColor : notTouchingColor;
        if (leftCheck != null)
        {
            Gizmos.color = leftColor;
            Gizmos.DrawWireCube(leftCheck.position, wallCheckSize);
            Color fillColor = leftColor; fillColor.a = 0.2f;
            Gizmos.color = fillColor;
            Gizmos.DrawCube(leftCheck.position, wallCheckSize);
        }
        // 오른쪽 벽 감지
        Color rightColor = defaultColor;
        if (Application.isPlaying && wallCheckComponent != null)
            rightColor = wallCheckComponent.IsTouchingWallRight ? touchingColor : notTouchingColor;
        if (rightCheck != null)
        {
            Gizmos.color = rightColor;
            Gizmos.DrawWireCube(rightCheck.position, wallCheckSize);
            Color fillColor = rightColor; fillColor.a = 0.2f;
            Gizmos.color = fillColor;
            Gizmos.DrawCube(rightCheck.position, wallCheckSize);
        }
    }

    private void DrawDetailedInfo()
    {
#if UNITY_EDITOR
        if (leftCheck != null)
        {
            Vector3 labelPos = leftCheck.position + Vector3.up * 0.5f;
            string info = "WallCheck Left\n";
            if (Application.isPlaying && wallCheckComponent != null)
                info += $"Touching: {wallCheckComponent.IsTouchingWallLeft}\n";
            info += $"Size: {wallCheckSize}";
            UnityEditor.Handles.Label(labelPos, info);
        }
        if (rightCheck != null)
        {
            Vector3 labelPos = rightCheck.position + Vector3.up * 0.5f;
            string info = "WallCheck Right\n";
            if (Application.isPlaying && wallCheckComponent != null)
                info += $"Touching: {wallCheckComponent.IsTouchingWallRight}\n";
            info += $"Size: {wallCheckSize}";
            UnityEditor.Handles.Label(labelPos, info);
        }
#endif
    }
} 