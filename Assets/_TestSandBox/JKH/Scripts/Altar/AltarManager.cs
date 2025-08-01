using Fusion;
using UnityEngine;
using System.Collections.Generic; // List 사용을 위해 추가

public class AltarManager : NetworkBehaviour
{
    // 씬 어디서든 AltarManager에 쉽게 접근할 수 있도록 static 인스턴스를 만듭니다 (싱글톤 패턴).
    public static AltarManager Instance { get; private set; }

    [Header("보상 아이템 설정")]
    [SerializeField] private List<NetworkPrefabRef> tier1RewardPrefabs; // 8점 보상 아이템 프리팹 목록
    [SerializeField] private NetworkPrefabRef kapalaPrefab; // 16점 보상 (카팔라) 프리팹

    // ⭐️ 팀 전체가 공유하는 호의 점수
    [Networked] public int SharedFavor { get; private set; }
    // 호스트에서만 사용하는, 이전 틱의 호의 점수를 저장하는 변수
    private int _previousFavorOnHost;

    private void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // 이미 인스턴스가 있다면 이 오브젝트는 파괴
            Destroy(gameObject);
        }
    }

    public override void Spawned()
    {
        // 호스트에서 이 변수를 초기화합니다.
        if (HasStateAuthority)
        {
            _previousFavorOnHost = SharedFavor;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // ⭐️ 모든 게임 로직은 호스트의 FixedUpdateNetwork에서 처리합니다.
        if (HasStateAuthority)
        {
            // 1. 현재 점수와 이전 점수를 비교하여 변화를 감지합니다.
            if (SharedFavor != _previousFavorOnHost)
            {
                // 2. 점수가 변경되었다면 보상 지급 로직을 실행합니다.
                //    보상 아이템은 AddFavor가 호출된 위치(제단 위치)에 스폰해야 하므로,
                //    이 방식보다는 AddFavor에서 직접 처리하는 것이 더 좋습니다. (아래 AddFavor 메서드 참조)

                // 3. 변화 감지가 끝났으므로 이전 점수 변수를 현재 점수로 업데이트합니다.
                _previousFavorOnHost = SharedFavor;
            }
        }
    }

    // 호의 점수를 추가하는 메서드 (호스트에서만 호출)
    public void AddFavor(int amount, Vector3 rewardSpawnPosition)
    {
        if (!HasStateAuthority) return;

        int oldFavor = SharedFavor;
        SharedFavor += amount;

        // ⭐️ OnChanged 콜백 대신, 점수를 변경하는 이 메서드에서 직접 보상 로직을 호출합니다.
        // 이것이 '상태 변경'과 '그로 인한 로직 실행'을 호스트의 Tick 안에서 처리하는 가장 명확한 방법입니다.
        CheckAndGiveRewards(oldFavor, SharedFavor, rewardSpawnPosition);
    }

    // 보상을 체크하고 지급하는 메서드 (호스트에서만 실행)
    private void CheckAndGiveRewards(int oldFavor, int newFavor, Vector3 rewardSpawnPosition)
    {
        // 카팔라 보상 (16점)
        if (oldFavor < 16 && newFavor >= 16)
        {
            Debug.Log("Host: Spawning Kapala as a reward!");
            if (kapalaPrefab.IsValid)
            {
                Runner.Spawn(kapalaPrefab, rewardSpawnPosition + Vector3.up, Quaternion.identity);
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
                    Runner.Spawn(rewardPrefab, rewardSpawnPosition + Vector3.up, Quaternion.identity);
                }
            }
        }
    }
}