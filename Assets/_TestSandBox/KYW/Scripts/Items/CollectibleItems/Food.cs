using Fusion;
using UnityEngine;

public class Food : NetworkBehaviour
{
    [Header("Food Settings")]
    [SerializeField] private int healAmount = 5; // 회복할 체력량
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject collectEffect; // 수집 효과 (선택사항)
    
    private bool isCollected = false; // 수집 완료 여부
    
    public override void Spawned()
    {
        // 네트워크 시뮬레이션 활성화
        Runner.SetIsSimulated(Object, true);
    }
    
    // 트리거 충돌 시 자동 수집
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        if (!HasStateAuthority) return;
        
        // 플레이어 태그 확인
        if (other.CompareTag("Player"))
        {
            // 플레이어 체력 컴포넌트 찾기
            var playerHealth = other.GetComponentInChildren<PlayerHealth>();
            if (playerHealth != null)
            {
                // 체력 회복 및 수집 완료
                CollectFood(playerHealth);
            }
        }
    }
    
    private void CollectFood(PlayerHealth playerHealth)
    {
        if (isCollected) return;
        
        // StateAuthority만 체력 회복 처리
        if (HasStateAuthority)
        {
            // 체력이 이미 최대인지 확인
            if (playerHealth.Health >= playerHealth.MaxHealth)
            {
                Debug.Log("[Food] 체력이 이미 최대입니다!");
                return;
            }
            
            // 체력 회복
            playerHealth.Heal(healAmount);
            
            // 수집 완료 표시
            isCollected = true;
            
            // 수집 효과 재생 (선택사항)
            if (collectEffect != null)
            {
                var effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
                Destroy(effect, 2f); // 2초 후 효과 제거
            }
            
            Debug.Log($"[Food] 체력 {healAmount} 회복! 현재 체력: {playerHealth.Health}/{playerHealth.MaxHealth}");
            var netObj = playerHealth.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                NetworkEventSystem.Inst.TriggerItemCollected(netObj.InputAuthority, 1);
            }
            
            // 음식 오브젝트 제거
            Runner.Despawn(Object);
        }
    }
}
