using UnityEngine;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.FSM;
using Fusion.Addons.Physics;

public enum BossStateName
{
    Idle,
    Dead,
    SkillA,
    SkillB,
    SkillC
}
public class BossBase : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnCurrentHealthChanged))] public int CurrentHealth { get; private set; } //몬스터 Hp의 변경이 감지되면 OnHpChanged 호출, 현재 hp
    [Networked] public BossStateName CurrentState { get; set; } //현재 스테이트 (EnemyFsm과 동기화)
    [Networked] public BossStateName LastUsedSkill { get; set; } // 마지막으로 사용한 스킬을 저장                    
    [Networked] protected bool IsDead { get; set; }
    [Networked] public TickTimer StateTimer { get; set; } //상태 시간(스킬 상태 지속시간)을 저장할 타이머
    [Networked] public TickTimer DespawnTimer { get; private set; } // 죽음 후 소멸까지의 시간을 잼


    //컴포넌트들
    public BossFSM fsm; //시각적 상태를 제어하는 fsm
    public BossData bossData; //ScriptableObject를 사용, 드래그앤드롭으로 적 기본 스탯 설정
    [HideInInspector]public Collider2D coll;
    [HideInInspector]public NetworkRigidbody2D nrb;

    // 소환된 몬스터를 관리하기 위한 Dictionary.
    // Key: 몬스터가 소환된 위치(Transform), Value: 소환된 몬스터의 NetworkObject
    public Dictionary<Transform, NetworkObject> SummonedMonsterMap { get; private set; } = new Dictionary<Transform, NetworkObject>();

    public override void Spawned() //네트워크 객체가 생성될 때 호출
    {
        coll = GetComponent<Collider2D>();
        nrb = GetComponent<NetworkRigidbody2D>();
        IsDead = false;

        if (Object.HasStateAuthority) //호스트(서버)에서 초기스탯 설정
        {
            CurrentHealth = bossData.maxHp;
            CurrentState = BossStateName.Idle;
            LastUsedSkill = BossStateName.Idle; // 초기값은 Idle로 설정
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return; //호스트가 아니면, 실행 X

        if (IsDead) return;

        //UpdateIdleState의 알고리즘에서 스킬 상태를 결정하고 CurrentState에 알맞는 상태를 부여함
        switch (CurrentState)
        {
            case BossStateName.Idle:
                fsm.StateMachine.ForceActivateState<BossIdleState>();
                UpdateIdleState();
                break;
        }
    }
    protected virtual void UpdateIdleState()
    {
        //Idle State (보스 기본 상태) 에서는 가장 최신에서 사용한 스킬을 제외한 나머지 스킬들 중에서 랜덤으로 스킬 상태에 돌입함.
        //각각의 보스가 가진 공통된 로직은 여기서 구현함
        //각각의 보스가 대기상태에서 다른 움직임 로직(드래곤은 날라다니고, 스켈레톤은 걸어다니는 등)을 가진다면, 이 함수를 상속받아서 따로 구현해야함

        // 대기 시간이 끝나지 않았으면 아무것도 하지 않음
        if (StateTimer.ExpiredOrNotRunning(Runner) == false) return;
        
        // 사용 가능한 모든 스킬 목록 생성
        List<BossStateName> availableSkills = new List<BossStateName>
        {
            BossStateName.SkillA,
            BossStateName.SkillB,
            BossStateName.SkillC
            // BossStateName.SkillD
        };

        // 마지막에 사용한 스킬이 있다면 목록에서 제외
        if (LastUsedSkill != BossStateName.Idle)
        {
            availableSkills.Remove(LastUsedSkill);
        }

        if (availableSkills.Count == 0)
        {
            CurrentState = BossStateName.Idle;
            LastUsedSkill = BossStateName.Idle;
            return;
        }

        // 남은 스킬 중에서 다음 스킬을 랜덤으로 선택  
        BossStateName nextSkill = availableSkills[UnityEngine.Random.Range(0, availableSkills.Count)];

        // 보스의 현재 상태와 마지막 사용 스킬을 업데이트
        CurrentState = nextSkill;
        LastUsedSkill = nextSkill;

        // 선택된 스킬에 따라 상태 머신 전환
        switch (nextSkill)
        {
            case BossStateName.SkillA:
                fsm.StateMachine.ForceActivateState<BossSkillAState>();
                break;
            case BossStateName.SkillB:
                fsm.StateMachine.ForceActivateState<BossSkillBState>();
                break;
            case BossStateName.SkillC:
                fsm.StateMachine.ForceActivateState<BossSkillCState>();
                break;
        }
    }

    //데미지를 받는 함수
    public void TakeDamage(int damage) //데미지를 받는 함수
    {
        //죽었다면 데미지 못받게 return
        if (IsDead) return;

        CurrentHealth -= damage;
        UnityEngine.Debug.Log($"보스 체력 : {CurrentHealth}");

        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            IsDead = true;
            CurrentState = BossStateName.Dead;
            DespawnTimer = TickTimer.CreateFromSeconds(Runner, 5.0f);
            fsm.StateMachine.ForceActivateState<BossDeadState>();
            // 적 처치 트리거 - 살아있는 플레이어들 대상
            foreach (var player in PlayerManager.Inst.GetAlivePlayers())
            {
                NetworkEventSystem.Inst.TriggerEnemyKilled(player.InputAuthority, 20);
            }
        }
    }
    public void OnCurrentHealthChanged()
    {
        //필요하다면 보스 체력 UI 업데아트가 실행될 곳 
    }
}
