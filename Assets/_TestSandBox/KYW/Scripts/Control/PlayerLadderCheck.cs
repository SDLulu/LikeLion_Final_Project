using Fusion;
using UnityEngine;

// 🪜 플레이어 사다리 감지 컴포넌트
// LadderCheck 하위 오브젝트에 위치하며, 자신의 위치에서 사다리 감지
public class PlayerLadderCheck : NetworkBehaviour
{
    [Header("Ladder Detection")]
    [SerializeField] private LayerMask ladderLayer = 1 << 8; // 사다리 레이어 지정 (필요시 인스펙터에서 변경)
    [SerializeField] private Vector2 ladderCheckSize = new Vector2(0.8f, 1.5f);

    // 🌐 네트워크 동기화
    [Networked] public bool IsNearLadder { get; private set; }

    public override void FixedUpdateNetwork()
    {
        CheckLadder();
    }

    private void CheckLadder()
    {
        // 자신의 위치에서 사다리 체크 (OverlapBox)
        IsNearLadder = Physics2D.OverlapBox(transform.position, ladderCheckSize, 0f, ladderLayer);
    }

    // 🎯 기즈모로 감지 영역 시각화 (GroundCheckVisualizer 스타일)
    private void OnDrawGizmos()
    {
        DrawLadderGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        DrawLadderGizmo();
    }

    private void DrawLadderGizmo()
    {
        Color boxColor = IsNearLadder ? Color.cyan : Color.gray;
        Gizmos.color = boxColor;
        Gizmos.DrawWireCube(transform.position, ladderCheckSize);
        // 내부 채우기
        Color fillColor = boxColor;
        fillColor.a = 0.2f;
        Gizmos.color = fillColor;
        Gizmos.DrawCube(transform.position, ladderCheckSize);
    }
}