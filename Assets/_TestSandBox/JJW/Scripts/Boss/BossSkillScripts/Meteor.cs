using UnityEngine;
using Fusion;
public class Meteor : BossSkillBState, IAnimationTriggerReceiver
{
    [Header("Animation")]
    [SerializeField] private string attackAnimStateName; // 메테오 시전 시 재생할 애니메이션

    [Header("Meteor Settings")]
    [SerializeField] private NetworkPrefabRef meteorPrefab; // 떨어질 메테오 프리팹
    [SerializeField] private int numberOfMeteors = 10; // 떨어질 총 메테오 개수 (n)
    [SerializeField] private float spawnInterval = 0.3f; // 메테오 생성 간격 (초)
    [SerializeField] private float spawnHeight = 15f; // 메테오가 생성될 높이

    [Header("Spawn Area")]
    [SerializeField] private Transform leftBoundary; // 왼쪽 생성 경계
    [SerializeField] private Transform rightBoundary; // 오른쪽 생성 경계

    // --- 네트워크 동기화 변수 ---
    [Networked] private int meteorsSpawned { get; set; }
    [Networked] private TickTimer spawnTimer { get; set; }

    [Networked] private bool startMeteor { get; set;}


    // 상태에 처음 진입했을 때
    protected override void OnEnterState()
    {
        UnityEngine.Debug.Log("보스 몬스터가 Meteor Spawn 상태에 진입했습니다!!!!!!!!!!!!!!!!!!!!");
        if (Object.HasStateAuthority)
        {
            meteorsSpawned = 0;
            startMeteor = false;
            // 첫 메테오가 바로 떨어지도록 타이머를 즉시 만료시킵니다.
            spawnTimer = TickTimer.CreateFromSeconds(Runner, 0);
        }
    }

    // 렌더링 측면에서 애니메이션 실행
    protected override void OnEnterStateRender()
    {
        anim.CrossFadeInFixedTime(attackAnimStateName, animTransitionLength);
    }
    public void OnAnimationEvent(string eventName)
    {
        if (!Object.HasStateAuthority) return;
        switch (eventName)
        {
            case "MeteorSpawn":
                startMeteor = true;
                anim.CrossFadeInFixedTime("Idle", animTransitionLength);
                break;
        }
    }
    // 물리 프레임마다 호출
    protected override void OnFixedUpdate()
    {
        if (!Object.HasStateAuthority || startMeteor == false) return;

        // 정해진 수의 메테오를 모두 소환했다면, 스킬을 종료하고 Idle 상태로 돌아갑니다.
        if (meteorsSpawned >= numberOfMeteors)
        {
            fsmRef.BossNetworkBehaviour.CurrentState = BossStateName.Idle;
            //fsmRef.StateMachine.ForceActivateState<BossIdleState>();
            return;
        }

        // 생성 간격 타이머가 만료되었다면
        if (spawnTimer.ExpiredOrNotRunning(Runner))
        {
            SpawnMeteor();
            meteorsSpawned++;
            // 다음 메테오를 위해 타이머를 재설정합니다.
            spawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
        }
    }

    private void SpawnMeteor()
    {
        // 왼쪽과 오른쪽 경계 사이에서 랜덤한 x좌표를 계산합니다.
        float randomX = Random.Range(leftBoundary.position.x, rightBoundary.position.x);
        Vector2 spawnPosition = new Vector2(randomX, boss.nrb.transform.position.y + spawnHeight);

        // 계산된 위치에 메테오 프리팹을 생성합니다.
        Runner.Spawn(meteorPrefab, spawnPosition, Quaternion.identity);
    }
}
