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
    
    // GroundCheckVisualizer.cs에서 시각화를 담당하므로 여기서는 기즈모 코드 제거
} 