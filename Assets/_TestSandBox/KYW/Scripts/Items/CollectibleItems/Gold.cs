using Fusion;
using UnityEngine;

public class Gold : NetworkBehaviour
{
    [Header("Gold Settings")]
    [SerializeField] private int goldAmount = 10; // 골드 수량
    
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
            // 플레이어 인벤토리 찾기
            var playerInventory = other.GetComponentInChildren<PlayerInventory>();
            var deathState = other.GetComponent<PlayerStunInvincibleDie>();
            if (playerInventory != null)
            {
                // 사망 중이면 획득 금지
                if (deathState != null && deathState.IsDead)
                {
                    return;
                }
                // 골드 추가 및 수집 완료
                CollectGold(playerInventory);
            }
        }
    }
    
    private void CollectGold(PlayerInventory playerInventory)
    {
        if (isCollected) return;
        
        // StateAuthority만 골드 추가 처리
        if (HasStateAuthority)
        {
            // 플레이어 인벤토리에 골드 추가
            playerInventory.CurrentMoney += goldAmount;
            
            // 수집 완료 표시
            isCollected = true;
            
            // 수집 효과 재생 (선택사항)
            if (collectEffect != null)
            {
                var effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
                Destroy(effect, 2f); // 2초 후 효과 제거
            }
            
            // 수집 소리와 이펙트 재생
            RPC_PlayCollectFeedback(transform.position);
            
            Debug.Log($"골드 {goldAmount}개를 수집했습니다! 현재 골드: {playerInventory.CurrentMoney}");
            var netObj = playerInventory.GetComponentInParent<NetworkObject>();
            if (netObj != null)
            {
                NetworkEventSystem.Inst.TriggerItemCollected(netObj.InputAuthority, goldAmount);
            }
            
            // 골드 오브젝트 제거
            Runner.Despawn(Object);
        }
    }

    // --- RPC 메서드들 ---
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayCollectFeedback(Vector3 pos)
    {
        AudioManager.Inst.PlaySound("돈먹기", pos);
        EffectManager.Inst.PlayEffect("돈이펙트", pos);
    }
}
