using Fusion;
using UnityEngine;

// �� 플레이어 지면 감지 컴포넌트
// GroundCheck 하위 오브젝트에 위치하며, 자신의 위치에서 지면 감지
// 씬에서 이 오브젝트를 드래그하여 감지 위치 조정 가능
public class PlayerGroundCheck : NetworkBehaviour
{
    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer = 1;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);
    
    // 🌐 네트워크 동기화
    [Networked] public bool IsGrounded { get; private set; }
    
    public override void FixedUpdateNetwork()
    {
        CheckGround();
    }
    
    private void CheckGround()
    {
        // 자신의 위치에서 직접 지면 체크
        IsGrounded = Physics2D.OverlapBox(transform.position, groundCheckSize, 0f, groundLayer);
    }
    
    // 🎯 기즈모로 감지 영역 시각화
    private void OnDrawGizmosSelected()
    {
        // Runner == null이면 네트워크 객체가 Spawned 상태가 아님
        if (Runner != null)
        {
            bool isGrounded = IsGrounded;
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position, groundCheckSize);
        }
        // Spawned 전에는 [Networked] 값 접근하지 않음
    }
} 