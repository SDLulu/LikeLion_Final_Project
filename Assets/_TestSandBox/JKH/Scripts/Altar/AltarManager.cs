using Fusion;
using UnityEngine;
using System.Collections.Generic; // List 사용을 위해 추가

public class AltarManager : NetworkBehaviour
{
    // 씬 어디서든 AltarManager에 쉽게 접근할 수 있도록 static 인스턴스를 만듭니다 (싱글톤 패턴).
    //public static AltarManager Instance { get; private set; }

    [Header("보상 아이템 설정")]
    [SerializeField] private List<NetworkPrefabRef> tier1RewardPrefabs; // 8점 보상 아이템 프리팹 목록
    [SerializeField] private NetworkPrefabRef kapalaPrefab; // 16점 보상 (카팔라) 프리팹
    [SerializeField] private List<NetworkPrefabRef> HealingRewardPrefabs; // 16점 보상 (카팔라) 프리팹

    // ⭐️ 팀 전체가 공유하는 호의 점수
    [Networked] public int SharedFavor { get; set; }
    // 호스트에서만 사용하는, 이전 틱의 호의 점수를 저장하는 변수
    private int _previousFavorOnHost;

    // 호의 점수를 추가하는 메서드 (호스트에서만 호출)
    public void AddFavor(int amount, Vector3 rewardSpawnPosition)
    {
        // ======================= 로그 추적 시작 =======================
        Debug.Log("--- STEP 4: AddFavor 메서드 진입 성공! ---");

        if (!HasStateAuthority)
        {
            Debug.LogError("--- FATAL ERROR: 권한(State Authority)이 없는 AltarManager에서 AddFavor가 호출되었습니다! ---");
            return;
        }

        Debug.Log("--- STEP 5: 권한 확인 통과. 점수 변경 시도... ---");
        int oldFavor = SharedFavor;
        SharedFavor += amount;

        Debug.Log($"--- STEP 6: SharedFavor 값 변경 완료. 이전: {oldFavor}, 현재: {SharedFavor} ---");

        // 보상 체크 로직은 그대로 둡니다.
        CheckAndGiveRewards(oldFavor, SharedFavor, rewardSpawnPosition);
        // ======================= 로그 추적 종료 =======================
    }

    // 보상을 체크하고 지급하는 메서드 (호스트에서만 실행)
    private void CheckAndGiveRewards(int oldFavor, int newFavor, Vector3 rewardSpawnPosition)
    {
        if (oldFavor < 24 && newFavor >= 24)
        {
            Debug.Log("24점 보상");
            SharedFavor -= 8;
            //회복 아이템 스폰
            if (HealingRewardPrefabs != null && HealingRewardPrefabs.Count > 0)
            {
                NetworkPrefabRef healingrewardPrefab = HealingRewardPrefabs[Random.Range(0, HealingRewardPrefabs.Count)];
                if (healingrewardPrefab.IsValid)
                {
                    Runner.Spawn(healingrewardPrefab, rewardSpawnPosition + (Vector3.up * 0.5f), Quaternion.identity);
                }
            }
        }
        // 카팔라 보상 (16점)
        if (oldFavor < 16 && newFavor >= 16)
        {
            Debug.Log("Host: Spawning Kapala as a reward!");
            if (kapalaPrefab.IsValid)
            {
                Runner.Spawn(kapalaPrefab, rewardSpawnPosition + (Vector3.up * 0.5f), Quaternion.identity);
            }
        }
        // 유용한 아이템 보상 (8점)
        else if (oldFavor < 8 && newFavor >= 8)
        {
            Debug.Log("Host: Spawning a useful item as a reward!");
            if (tier1RewardPrefabs != null && tier1RewardPrefabs.Count > 0)
            {
                // 보상 목록에서 무작위 아이템 하나를 선택
                NetworkPrefabRef rewardPrefab = tier1RewardPrefabs[Random.Range(0, tier1RewardPrefabs.Count)];
                if (rewardPrefab.IsValid)
                {
                    Runner.Spawn(rewardPrefab, rewardSpawnPosition + (Vector3.up * 0.5f), Quaternion.identity);
                }
            }
        }
    }
}