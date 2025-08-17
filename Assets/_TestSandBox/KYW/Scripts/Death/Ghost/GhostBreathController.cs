using Fusion;
using UnityEngine;

// 💨 고스트 입김 프리팹 컨트롤러
// 입김 프리팹이 점점 커지면서 이동하고 충돌 시 넉백만 적용
public class GhostBreathController : NetworkBehaviour
{
    [Header("입김 설정")]
    [SerializeField] private float scaleMultiplier = 2f; // 크기 배수 (초기 크기의 몇 배로 커질지)
    [SerializeField] private float knockbackForce = 5f; // 넉백 힘
    [SerializeField] private float knockbackDuration = 0.5f; // 넉백 지속시간
    
    // 네트워크 상태
    [Networked] private Vector2 moveDirection { get; set; }
    [Networked] private float moveSpeed { get; set; }
    [Networked] private float duration { get; set; }
    [Networked] private TickTimer lifeTimer { get; set; }
    
    // 로컬 참조
    private Transform breathTransform;
    private Vector3 initialScale;
    private Rigidbody2D breathRigidbody;
    
    public override void Spawned()
    {
        // 네트워크 시뮬레이션 활성화
        Runner.SetIsSimulated(Object, true);
        breathTransform = transform;
        initialScale = breathTransform.localScale;
        breathRigidbody = GetComponent<Rigidbody2D>();
        
        Debug.Log($"[{name}] 고스트 입김 컨트롤러 초기화 완료!");
    }
    
    // 💨 입김 초기화 (PlayerGhostController에서 호출)
    public void InitializeBreath(Vector2 direction, float speed, float lifeDuration)
    {
        if (!HasStateAuthority) return;
        
        moveDirection = direction;
        moveSpeed = speed;
        duration = lifeDuration;
        lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeDuration);
        
        // 회전은 PlayerGhostController에서 이미 설정됨
        
        // 유령 입김 소리 재생
        RPC_PlayBreathSound();
        
        Debug.Log($"[{name}] 입김 초기화: 방향={direction}, 속도={speed}, 지속시간={lifeDuration}");
    }
    
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        
        // 수명 체크
        if (lifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }
        
        // 이동 (Rigidbody2D를 통한 물리적 이동)
        if (breathRigidbody != null)
        {
            Vector2 moveVector = moveDirection * moveSpeed * Runner.DeltaTime;
            Vector2 newPosition = breathRigidbody.position + moveVector;
            breathRigidbody.MovePosition(newPosition);
        }
        else
        {
            // 백업: Transform 직접 이동
            Vector3 moveVector = (Vector3)(moveDirection * moveSpeed * Runner.DeltaTime);
            transform.position += moveVector;
        }
        
        // 크기 증가 (시간에 따라 점점 커짐)
        float remainingTime = lifeTimer.RemainingTime(Runner) ?? 0f;
        float progress = 1f - (remainingTime / duration);
        float currentScale = Mathf.Lerp(1f, scaleMultiplier, progress);
        breathTransform.localScale = initialScale * currentScale;
        
        // 디버그 로그 (확인용)
        if (Runner.IsForward) // 호스트에서만 로그 출력
        {
            Debug.Log($"[{name}] Scale: {currentScale:F2}, Progress: {progress:F2}, Remaining: {remainingTime:F2}");
        }
    }
    
    // 💥 충돌 처리 (넉백만 적용)
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[{name}] OnTriggerEnter2D 호출됨: {other.name}, Layer: {other.gameObject.layer}");
        
        if (!HasStateAuthority) return;
        
        
        // 충돌 지점에서 타겟으로의 방향 계산 (AttackCollisionHandler 방식)
        Vector2 knockbackDirection = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
        Vector2 knockbackForceVector = knockbackDirection * knockbackForce;
        
        // 플레이어나 적에게 넉백 적용
        var playerInteraction = other.GetComponent<IPlayerInteraction>();
        if (playerInteraction != null)
        {
            // 넉백 적용 (데미지는 없음)
            playerInteraction.ApplyKnockback(knockbackForceVector, knockbackDuration);
            // 타격 소리 재생
            RPC_PlayHitSound(other.transform.position);
            Debug.Log($"[{name}] {other.name}에게 넉백 적용: {knockbackForceVector}, 지속시간: {knockbackDuration}");
        }
        
        // 아이템에도 넉백 적용
        var itemInteraction = other.GetComponent<IItemInteraction>();
        if (itemInteraction != null)
        {
            itemInteraction.ApplyKnockback(knockbackForceVector, knockbackDuration);
            // 타격 소리 재생
            RPC_PlayHitSound(other.transform.position);
            Debug.Log($"[{name}] {other.name} 아이템에게 넉백 적용: {knockbackForceVector}, 지속시간: {knockbackDuration}");
        }
    }
    
    // --- RPC 메서드들 ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayBreathSound()
    {
        // 유령 입김 소리 재생
        AudioManager.Inst.PlaySound("유령입김", transform.position);
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayHitSound(Vector3 hitPosition)
    {
        // 타격 소리 재생
        AudioManager.Inst.PlaySound("충돌", hitPosition);
    }
} 