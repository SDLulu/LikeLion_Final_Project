using Fusion;
using UnityEngine;

// 🌍 플레이어 지면 감지 컴포넌트
// 지면 감지 로직만 담당
public class PlayerGroundCheck : NetworkBehaviour
{
    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer = 1;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);
    
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool forceShowGizmos = true; // 강제로 기즈모 표시
    
    // 🌐 네트워크 동기화
    [Networked] public bool IsGrounded { get; private set; }
    
    public override void FixedUpdateNetwork()
    {
        CheckGround();
    }
    
    private void CheckGround()
    {
        if (groundCheck != null)
        {
            IsGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        }
    }
    
    // 🎨 에디터 전용 기즈모 (더 강력한 조건)
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 더 강력한 조건 체크
        if (!Application.isEditor) return;
        if (!showGizmos && !forceShowGizmos) return;
        if (groundCheck == null) return;
        
        // 기본 기즈모 그리기
        DrawGroundCheckGizmos();
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!Application.isEditor) return;
        if (groundCheck == null) return;
        
        // 선택 시 기즈모 그리기
        DrawGroundCheckGizmos();
        DrawDebugInfo();
    }
    
    private void DrawGroundCheckGizmos()
    {
        // 지면 감지 상태에 따라 색상 변경
        Color boxColor = IsGrounded ? Color.green : Color.red;
        
        // 게임 실행 중이 아니면 노란색으로 표시
        if (!Application.isPlaying)
        {
            boxColor = Color.yellow;
        }
        
        // 박스 외곽선 (진한 색)
        Gizmos.color = boxColor;
        DrawWireBox(groundCheck.position, groundCheckSize);
        
        // 내부 채우기 (투명한 색)
        Color fillColor = boxColor;
        fillColor.a = 0.2f;
        Gizmos.color = fillColor;
        Gizmos.DrawCube(groundCheck.position, groundCheckSize);
    }
    
    private void DrawDebugInfo()
    {
        // 텍스트 정보 표시
        Vector3 textPos = groundCheck.position + Vector3.up * 0.5f;
        string info = $"GroundCheck\nGrounded: {IsGrounded}\nLayer: {groundLayer.value}\nSize: {groundCheckSize}";
        
        UnityEditor.Handles.Label(textPos, info);
        
        // 중심점 표시
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(groundCheck.position, 0.05f);
    }
#endif
    
    // 와이어 박스 그리기 헬퍼 메서드
    private void DrawWireBox(Vector3 center, Vector2 size)
    {
        Vector3 topLeft = center + new Vector3(-size.x/2, size.y/2, 0);
        Vector3 topRight = center + new Vector3(size.x/2, size.y/2, 0);
        Vector3 bottomLeft = center + new Vector3(-size.x/2, -size.y/2, 0);
        Vector3 bottomRight = center + new Vector3(size.x/2, -size.y/2, 0);
        
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
    
    // 🛠️ 디버깅용 수동 테스트 메서드
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestGroundCheck()
    {
        if (groundCheck != null)
        {
            bool testResult = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
            Debug.Log($"[GroundCheck Test] Position: {groundCheck.position}, Result: {testResult}, Layer: {groundLayer.value}");
        }
        else
        {
            Debug.LogError("[GroundCheck Test] GroundCheck Transform is null!");
        }
    }
} 