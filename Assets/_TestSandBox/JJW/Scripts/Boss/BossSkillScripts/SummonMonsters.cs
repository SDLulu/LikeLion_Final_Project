using UnityEngine;
using System.Collections.Generic;
using Fusion;
using System.Linq;

public class SummonMonsters : BossSkillCState
{
    [Header("Animation")]
    [SerializeField]
    private string summonAnimStateName = "Attack"; // 소환 시전 시 재생할 애니메이션

    [Header("Summon Configuration")]
    [Tooltip("소환할 몬스터 프리팹 목록입니다. NetworkPrefabRef 타입을 가집니다.")]
    [SerializeField]
    private List<NetworkPrefabRef> monsterPrefabs;

    [Tooltip("몬스터가 소환될 위치 목록입니다. Transform 타입을 가집니다.")]
    [SerializeField]
    private List<Transform> spawnPoints;

    [Tooltip("소환을 마친 후 Idle 상태로 돌아가기까지의 짧은 지연 시간입니다.")]
    [SerializeField]
    private float exitDelay = 1.5f;

    // 상태에 처음 진입했을 때
    protected override void OnEnterState()
    {
        // base.OnEnterState()는 사용하지 않습니다. stateDuration 대신 exitDelay를 사용합니다.
        if (Object.HasStateAuthority)
        {
            CleanUpDeadMonsters();
            
            SummonMissingMonsters();

            // 소환 애니메이션이 자연스럽게 보일 시간을 번 후, Idle 상태로 돌아가도록 타이머를 설정합니다.
            fsmRef.BossNetworkBehaviour.StateTimer = TickTimer.CreateFromSeconds(Runner, exitDelay);
        }
    }

    // 렌더링 측면에서 애니메이션 실행
    protected override void OnEnterStateRender()
    {
        anim.CrossFadeInFixedTime(summonAnimStateName, animTransitionLength);
    }

    // 물리 프레임마다 호출
    protected override void OnFixedUpdate()
    {
        if (!Object.HasStateAuthority) return;

        // exitDelay 타이머가 만료되었다면 Idle 상태로 돌아갑니다.
        if (fsmRef.BossNetworkBehaviour.StateTimer.ExpiredOrNotRunning(Runner))
        {
            fsmRef.BossNetworkBehaviour.CurrentState = BossStateName.Idle;
            // fsmRef.StateMachine.ForceActivateState<BossIdleState>();
        }
    }

    /// <summary>
    /// 등록된 모든 위치에 몬스터를 소환하는 메서드입니다.
    /// </summary>
    // private void SummonAllMonsters()
    // {
    //     // 몬스터 목록과 스폰 위치 목록의 개수가 다를 경우를 대비한 안전장치
    //     int summonCount = Mathf.Min(monsterPrefabs.Count, spawnPoints.Count);
    //     if (summonCount == 0)
    //     {
    //         Debug.LogWarning("소환할 몬스터나 스폰 위치가 지정되지 않았습니다.");
    //         return;
    //     }

    //     // 지정된 개수만큼 몬스터를 소환합니다.
    //     for (int i = 0; i < summonCount; i++)
    //     {
    //         NetworkPrefabRef monsterToSpawn = monsterPrefabs[i];
    //         Transform spawnPoint = spawnPoints[i];

    //         // 해당 위치에 몬스터를 네트워크 객체로 생성합니다.
    //         Runner.Spawn(monsterToSpawn, spawnPoint.position, spawnPoint.rotation);
    //     }
    // }

    /// <summary>
    /// 보스의 기억(Dictionary)에서 이미 파괴된(null) 몬스터들을 제거합니다.
    /// </summary>
    private void CleanUpDeadMonsters()
    {
        // Dictionary에서 Value(NetworkObject)가 null인 Key(Transform)들을 찾습니다.
        var deadMonsterKeys = boss.SummonedMonsterMap
            .Where(pair => pair.Value == null || (pair.Value.TryGetComponent(out EnemyBase enemy) && enemy.IsDead))
            .Select(pair => pair.Key)
            .ToList();

        // 찾은 Key들을 순회하며 Dictionary에서 제거합니다.
        foreach (var key in deadMonsterKeys)
        {
            boss.SummonedMonsterMap.Remove(key);
        }
    }

    /// <summary>
    /// 비어있는 소환 위치에만 몬스터를 소환하는 메서드입니다.
    /// </summary>
    private void SummonMissingMonsters()
    {
        // 지정된 스폰 위치들을 하나씩 확인합니다.
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Transform spawnPoint = spawnPoints[i];

            // 1. 이 스폰 위치가 보스의 기억에 있는지, 그리고 몬스터가 아직 살아있는지 확인합니다.
            if (boss.SummonedMonsterMap.ContainsKey(spawnPoint) && boss.SummonedMonsterMap[spawnPoint] != null)
            {
                // 이미 몬스터가 살아있다면, 아무것도 하지 않고 다음 위치로 넘어갑니다.
                Debug.Log($"{spawnPoint.name} 위치에는 몬스터가 이미 살아있어 소환을 건너뜁니다.");
                continue;
            }

            // 2. 소환할 몬스터가 목록에 있는지 확인합니다.
            if (i >= monsterPrefabs.Count)
            {
                Debug.LogWarning($"SpawnPoint는 있지만, {i}번째 MonsterPrefab이 지정되지 않았습니다.");
                continue;
            }

            // 3. 몬스터를 소환하고, 보스의 기억(Dictionary)에 새로 등록합니다.
            NetworkPrefabRef monsterToSpawn = monsterPrefabs[i];
             NetworkObject newMonster = Runner.Spawn(monsterToSpawn, 
                                                spawnPoint.position, 
                                                spawnPoint.rotation,
                                                null,
                                                (runner, obj) => { obj.transform.SetParent(boss.tileRogic.parentTrans);}); // boss의 NetworkObject를 부모로 설정


            if (newMonster != null)
            {
                // 새로 소환된 몬스터를 '어디에서 소환되었는지'와 함께 기억합니다.
                boss.SummonedMonsterMap[spawnPoint] = newMonster;
            }
        }
    }
}
