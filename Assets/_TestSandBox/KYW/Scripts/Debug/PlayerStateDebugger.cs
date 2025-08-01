using UnityEngine;
using Fusion;

// 🧪 플레이어 상태 디버거 (테스트용)
public class PlayerStateDebugger : MonoBehaviour
{
    [Header("테스트 대상")]
    [SerializeField] private PlayerStunInvincibleDie stunInvincible;
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private PlayerInteractionBase playerInteraction;
    [SerializeField] private PlayerDeathHandler playerDeathHandler;
    
    [Header("테스트 설정")]
    [SerializeField] private float testStunDuration = 2f;
    [SerializeField] private float testInvincibleDuration = 3f;
    [SerializeField] private float testKnockbackForce = 10f;
    
    [Header("디버그 정보")]
    [SerializeField] private bool showDebugInfo = true;
    
    private void Start()
    {
        // 자동으로 컴포넌트 찾기
        if (stunInvincible == null)
            stunInvincible = FindObjectOfType<PlayerStunInvincibleDie>();
        if (playerAnimation == null)
            playerAnimation = FindObjectOfType<PlayerAnimation>();
        if (playerInteraction == null)
            playerInteraction = FindObjectOfType<PlayerInteractionBase>();
        if (playerDeathHandler == null)
            playerDeathHandler = FindObjectOfType<PlayerDeathHandler>();
            
        Debug.Log("🧪 PlayerStateDebugger 초기화 완료!");
    }
    
    private void Update()
    {
        // 테스트 입력 처리만 수행
        HandleTestInput();
        
        // DisplayDebugInfo() 호출 제거 - OnGUI에서 처리
    }
    
    private void HandleTestInput()
    {
        // 1: 스턴 테스트
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestStun();
        }
        
        // 2: 무적 테스트
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TestInvincible();
        }
        
        // 3: 사망 테스트
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TestDeath();
        }
        
        // 4: 랜덤 넉백 테스트
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TestRandomKnockback();
        }
        
        // 0: 상태 리셋
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ResetStates();
        }
        
        // R: 상태 업데이트 강제 실행
        if (Input.GetKeyDown(KeyCode.R))
        {
            ForceStateUpdate();
        }
    }
    
    private void TestStun()
    {
        if (playerInteraction != null)
        {
            playerInteraction.ApplyStun(testStunDuration);
            Debug.Log($"🧪 스턴 테스트 실행! 지속시간: {testStunDuration}초");
        }
    }
    
    private void TestInvincible()
    {
        if (playerInteraction != null)
        {
            playerInteraction.SetInvincible(true, testInvincibleDuration);
            Debug.Log($"🧪 무적 테스트 실행! 지속시간: {testInvincibleDuration}초");
        }
    }
    
    private void TestDeath()
    {
        if (playerDeathHandler != null)
        {
            playerDeathHandler.Die();
            Debug.Log("🧪 사망 테스트 실행!");
        }
    }
    
    private void TestRandomKnockback()
    {
        if (playerInteraction != null)
        {
            ApplyRandomKnockback(testKnockbackForce);
            Debug.Log($"🧪 랜덤 넉백 테스트 실행! 힘: {testKnockbackForce}");
        }
    }
    
    // 🧪 랜덤 방향 넉백 테스트 (디버그용)
    private void ApplyRandomKnockback(float force = 10f, float stunDuration = 0.5f)
    {
        if (playerInteraction == null) return;
        
        // 랜덤 방향 생성 (360도)
        float randomAngle = Random.Range(0f, 360f);
        Vector2 randomDirection = new Vector2(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad),
            Mathf.Sin(randomAngle * Mathf.Deg2Rad)
        );
        
        // 랜덤한 힘 적용
        Vector2 knockbackForce = randomDirection * force;
        playerInteraction.ApplyKnockback(knockbackForce, stunDuration);
        
        Debug.Log($"[PlayerStateDebugger] 랜덤 넉백 테스트! 방향: {randomDirection}, 힘: {force}, 스턴시간: {stunDuration}초");
    }
    
    private void ResetStates()
    {
        if (playerInteraction != null)
        {
            // 무적 상태 해제 (테스트용)
            playerInteraction.SetInvincible(false, 0f);
            Debug.Log("🧪 상태 리셋 실행! (무적 상태 해제)");
        }
    }
    
    private void ForceStateUpdate()
    {
        if (stunInvincible != null)
        {
            Debug.Log("🧪 PlayerStunInvincibleDie는 자동 상태 업데이트를 하지 않습니다!");
        }
    }
    
    private void DisplayDebugInfo()
    {
        if (stunInvincible == null) return;
        
        // Spawned 상태 확인 (Networked 프로퍼티 접근 전)
        if (!stunInvincible.Object.IsValid)
        {
            return;
        }
        
        // Debug.Log 제거 - OnGUI에서만 표시
        // 이제 이 메서드는 사용되지 않음
    }
    
    // GUI로 화면에 표시 (옵션)
    private void OnGUI()
    {
        if (!showDebugInfo || stunInvincible == null) return;
        
        // Spawned 상태 확인
        if (!stunInvincible.Object.IsValid) 
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label("=== 플레이어 상태 디버그 ===");
            GUILayout.Label("플레이어가 아직 Spawned되지 않았습니다.");
            GUILayout.EndArea();
            return;
        }
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== 플레이어 상태 디버그 ===");
        GUILayout.Label($"PlayerInteraction: {(playerInteraction != null ? "찾음" : "없음")}");
        GUILayout.Label($"StateAuthority: {(playerInteraction != null ? playerInteraction.HasStateAuthority.ToString() : "N/A")}");
        GUILayout.Label($"Spawned: {stunInvincible.Object.IsValid}");
        
        GUILayout.Label($"스턴: {stunInvincible.IsStunned}");
        GUILayout.Label($"무적: {stunInvincible.IsInvincible}");
        GUILayout.Label($"사망: {(playerDeathHandler != null ? playerDeathHandler.IsDead.ToString() : "N/A")}");
        GUILayout.Label($"들림: {stunInvincible.IsHeld}");
        
        GUILayout.Space(10);
        GUILayout.Label("=== 테스트 키 ===");
        GUILayout.Label("1: 스턴, 2: 무적, 3: 사망, 4: 넉백, 0: 리셋, R: 업데이트");
        GUILayout.EndArea();
    }
} 