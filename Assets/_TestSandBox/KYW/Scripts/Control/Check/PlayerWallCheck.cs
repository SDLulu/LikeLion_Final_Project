using Fusion;
using UnityEngine;

// 🧱 플레이어 벽 감지 컴포넌트 (Ground 레이어 사용)
// 플레이어 하위에 WallCheck(Left/Right) 오브젝트를 배치하여 좌/우 벽 감지
public class PlayerWallCheck : NetworkBehaviour
{
    [Header("Wall Detection")]
    [SerializeField] private LayerMask wallLayer = 1; // Ground 레이어 사용
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.1f, 1.0f);
    [SerializeField] private Transform leftCheck;   // 왼쪽 벽 감지 위치
    [SerializeField] private Transform rightCheck;  // 오른쪽 벽 감지 위치

    // 🌐 네트워크 동기화
    [Networked] public bool IsTouchingWallLeft { get; private set; }
    [Networked] public bool IsTouchingWallRight { get; private set; }

    public override void FixedUpdateNetwork()
    {
        // 왼쪽 벽 감지
        if (leftCheck != null)
            IsTouchingWallLeft = Physics2D.OverlapBox(leftCheck.position, wallCheckSize, 0f, wallLayer);
        // 오른쪽 벽 감지
        if (rightCheck != null)
            IsTouchingWallRight = Physics2D.OverlapBox(rightCheck.position, wallCheckSize, 0f, wallLayer);
    }

    // 🎯 기즈모로 감지 영역 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        if (leftCheck != null)
            Gizmos.DrawWireCube(leftCheck.position, wallCheckSize);
        if (rightCheck != null)
            Gizmos.DrawWireCube(rightCheck.position, wallCheckSize);
    }
} 