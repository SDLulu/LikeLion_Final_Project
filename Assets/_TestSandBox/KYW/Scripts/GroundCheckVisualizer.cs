using UnityEngine;

// 🎨 그라운드체크 시각화 전용 컴포넌트
// NetworkBehaviour 문제 해결을 위한 별도 컴포넌트
public class GroundCheckVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerGroundCheck groundCheckComponent;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);
    
    [Header("Visual Settings")]
    [SerializeField] private bool showAlways = true;
    [SerializeField] private Color groundedColor = Color.green;
    [SerializeField] private Color airborneColor = Color.red;
    [SerializeField] private Color defaultColor = Color.yellow;
    
    private void Start()
    {
        // 자동으로 컴포넌트 찾기
        if (groundCheckComponent == null)
            groundCheckComponent = GetComponent<PlayerGroundCheck>();
    }
    
    private void OnDrawGizmos()
    {
        if (!showAlways || groundCheck == null) return;
        DrawGizmo();
    }
    
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        DrawGizmo();
        DrawDetailedInfo();
    }
    
    private void DrawGizmo()
    {
        // 색상 결정
        Color boxColor = defaultColor;
        
        if (Application.isPlaying && groundCheckComponent != null)
        {
            boxColor = groundCheckComponent.IsGrounded ? groundedColor : airborneColor;
        }
        
        // 박스 그리기
        Gizmos.color = boxColor;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        
        // 내부 채우기
        Color fillColor = boxColor;
        fillColor.a = 0.2f;
        Gizmos.color = fillColor;
        Gizmos.DrawCube(groundCheck.position, groundCheckSize);
    }
    
    private void DrawDetailedInfo()
    {
        #if UNITY_EDITOR
        Vector3 labelPos = groundCheck.position + Vector3.up * 0.5f;
        string info = "GroundCheck Visualizer\n";
        
        if (Application.isPlaying && groundCheckComponent != null)
        {
            info += $"Grounded: {groundCheckComponent.IsGrounded}\n";
        }
        
        info += $"Size: {groundCheckSize}";
        UnityEditor.Handles.Label(labelPos, info);
        #endif
    }
} 