using Fusion;
using UnityEngine;

// 🧪 테스트용 죽음 트리거 컴포넌트
public class DeathTrigger : NetworkBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private KeyCode deathKey = KeyCode.K; // 죽음 트리거 키
    [SerializeField] private bool showDebugInfo = true;
    
    private PlayerStunInvincibleDie playerDeath;
    private SpelunkyPlayerController playerController;
    
    public override void Spawned()
    {
        // 플레이어 죽음 컴포넌트 찾기
        playerDeath = GetComponent<PlayerStunInvincibleDie>();
        if (playerDeath == null)
        {
            playerDeath = GetComponentInParent<PlayerStunInvincibleDie>();
        }
        
        // 플레이어 컨트롤러 찾기
        playerController = GetComponent<SpelunkyPlayerController>();
        if (playerController == null)
        {
            playerController = GetComponentInParent<SpelunkyPlayerController>();
        }
        
        if (playerDeath == null)
        {
            Debug.LogError($"[{name}] PlayerStunInvincibleDie 컴포넌트를 찾을 수 없습니다!");
        }
        
        if (playerController == null)
        {
            Debug.LogError($"[{name}] SpelunkyPlayerController 컴포넌트를 찾을 수 없습니다!");
        }
    }
    
    public override void FixedUpdateNetwork()
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 입력 데이터 가져오기
        if (playerController != null)
        {
            var inputData = playerController.GetNetworkInputData();
            
            // 죽음 키 입력 감지
            if (inputData.NetworkButtons.IsSet(SpelunkyInputButtons.Death))
            {
                TriggerDeath();
            }
        }
    }
    
    // 💀 죽음 트리거
    private void TriggerDeath()
    {
        if (playerDeath == null) return;
        
        if (!playerDeath.IsDead)
        {
            playerDeath.Die();
            Debug.Log($"[{name}] 플레이어 죽음 트리거됨!");
        }
        else
        {
            playerDeath.Resurrect();
            Debug.Log($"[{name}] 플레이어 부활 트리거됨!");
        }
    }
    
    // 📊 디버그 정보 표시
    private void OnGUI()
    {
        if (!showDebugInfo || playerDeath == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"💀 Death Trigger Debug Info");
        GUILayout.Label($"Death Key: {deathKey}");
        GUILayout.Label($"Is Dead: {playerDeath.IsDead}");
        GUILayout.Label($"Is Stunned: {playerDeath.IsStunned}");
        GUILayout.Label($"Is Invincible: {playerDeath.IsInvincible}");
        GUILayout.Label($"Is Held: {playerDeath.IsHeld}");
        GUILayout.Label($"Is Thrown: {playerDeath.IsThrown}");
        GUILayout.EndArea();
    }
} 