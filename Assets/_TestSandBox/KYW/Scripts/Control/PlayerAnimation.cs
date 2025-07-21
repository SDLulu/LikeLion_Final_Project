using Fusion;
using UnityEngine;

// 🎭 플레이어 애니메이션 컴포넌트 (상태 패턴 기반 리팩토링)
// SpelunkyPlayerController의 CurrentState만 참조하여 애니메이션 제어
public class PlayerAnimation : NetworkBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // 상태 패턴 기반 참조
    private SpelunkyPlayerController playerController;

    public override void Spawned()
    {
        // 컴포넌트 참조
        if (animator == null)
            animator = GetComponent<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Player 오브젝트 찾기
        Transform playerObject = transform.parent;
        if (playerObject != null)
        {
            playerController = playerObject.GetComponent<SpelunkyPlayerController>();
            if (playerController == null)
                Debug.LogError($"[{name}] SpelunkyPlayerController 컴포넌트를 찾을 수 없습니다!");
            if (animator == null)
                Debug.LogError($"[{name}] Animator 컴포넌트를 찾을 수 없습니다!");
            if (spriteRenderer == null)
                Debug.LogError($"[{name}] SpriteRenderer 컴포넌트를 찾을 수 없습니다!");
        }
        else
        {
            Debug.LogError($"[{name}] PlayerAnimation이 Player 오브젝트의 하위에 없습니다. Player/Visual 하위에 배치해주세요.");
        }
    }

    // 애니메이션 동기화
    public override void Render()
    {
        UpdateAnimations();
        UpdateSpriteDirection();
    }

    private void UpdateAnimations()
    {
        if (animator == null || playerController == null) return;
        // 상태 패턴 기반으로 State(int) 파라미터만 설정
        animator.SetInteger("State", (int)playerController.CurrentState);
    }

    private void UpdateSpriteDirection()
    {
        if (spriteRenderer != null && playerController != null)
        {
            // PlayerMovement의 IsFacingLeft를 그대로 사용하려면 playerController에서 노출 필요
            // 임시로 기존 방식 유지 (추후 playerController에 방향 프로퍼티 추가 권장)
            var movement = playerController.GetComponent<PlayerMovement>();
            if (movement != null)
                spriteRenderer.flipX = movement.IsFacingLeft;
        }
    }
} 