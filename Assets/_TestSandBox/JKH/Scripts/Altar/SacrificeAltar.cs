using Fusion;
using UnityEngine;

public class SacrificeAltar : NetworkBehaviour
{
    [Header("제단 설정")]
    [SerializeField] private float sacrificeDelay = 1f; // 제물로 바쳐지기까지 필요한 시간
    [SerializeField] private float cooldownDuration = 2.0f; // 제물 바친 후 재사용 대기시간
    [SerializeField] private GameObject sacrificeEffectPrefab; // 제물 바칠 때 나타날 이펙트

    // --- 네트워크 상태 변수들 ---
    [Networked] private TickTimer CooldownTimer { get; set; } // 제단 재사용 쿨다운 타이머
    [Networked] private TickTimer SacrificeDelayTimer { get; set; } // 제물화 지연 타이머
    [Networked] private NetworkObject PotentialSacrifice { get; set; } // 제물 후보 네트워크 오브젝트

    

    public override void FixedUpdateNetwork()
    {
        // 호스트에서만 실행하며, 제물 후보가 있고 지연 타이머가 끝났는지 확인합니다.
        if (HasStateAuthority && PotentialSacrifice != null && SacrificeDelayTimer.Expired(Runner))
        {
            // 타이머가 만료되었으므로 제물 바치기를 시도합니다.
            TryToPerformSacrifice();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!HasStateAuthority || !CooldownTimer.ExpiredOrNotRunning(Runner)) return;

        var targetComponent = other.GetComponentInParent<PlayerStunInvincibleDie>();

        // 스턴 상태인 유효한 대상이 들어왔을 때
        if (targetComponent != null && targetComponent.IsStunned && !targetComponent.IsDead)
        {
            Debug.Log($"Host: 제물 후보 [{targetComponent.name}]가 제단에 올라왔습니다. 1초 카운트다운을 시작합니다.");
            // 제물 후보로 설정하고, 지연 타이머를 시작합니다.
            PotentialSacrifice = targetComponent.Object;
            SacrificeDelayTimer = TickTimer.CreateFromSeconds(Runner, sacrificeDelay);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!HasStateAuthority) return;

        var targetObject = other.GetComponentInParent<NetworkObject>();

        // 제물 후보가 카운트다운 중에 제단 밖으로 나가면 취소합니다.
        if (targetObject != null && targetObject == PotentialSacrifice)
        {
            Debug.Log($"Host: 제물 후보 [{targetObject.name}]가 제단을 벗어나 제물화가 취소되었습니다.");
            // 모든 상태를 초기화합니다.
            PotentialSacrifice = null;
            SacrificeDelayTimer = TickTimer.None;
        }
    }

    private void TryToPerformSacrifice()
    {
        // PotentialSacrifice가 여전히 유효한지 다시 확인합니다.
        if (PotentialSacrifice == null || !PotentialSacrifice.IsValid)
        {
            ResetAltarState();
            return;
        }

        var targetComponent = PotentialSacrifice.GetComponent<PlayerStunInvincibleDie>();

        // 1초가 지난 지금도 여전히 스턴 상태인지 최종 확인합니다.
        if (targetComponent != null && targetComponent.IsStunned)
        {
            if (targetComponent.IsStunned || targetComponent.IsDead)
                PerformSacrifice(targetComponent);
        }
        else
        {
            Debug.Log($"Host: [{targetComponent?.name}]이(가) 제물로 바쳐지기 전에 스턴에서 풀려났습니다.");
        }

        // 시도가 끝났으므로 상태를 초기화합니다.
        ResetAltarState();
    }

    private void PerformSacrifice(PlayerStunInvincibleDie target)
    {
        Debug.Log($"Host: [{target.name}]을(를) 제물로 바칩니다!");
        CooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldownDuration);

        if (sacrificeEffectPrefab != null)
        {
            Instantiate(sacrificeEffectPrefab, target.transform.position, Quaternion.identity);
        }
        if (target.IsStunned)
        {
            //살아 있는 점수
            AltarManager.Instance.AddFavor(8, target.transform.position);
        }else if (target.IsDead)
        {
            //죽은 점수
            AltarManager.Instance.AddFavor(6, target.transform.position);
        }

        if (target.Object != null && target.Object.IsValid)
        {
            Runner.Despawn(target.Object);
        }

        // TODO: 여기에 보상 시스템을 구현합니다.
        // AltarManager Ddol -> 제단 호의 점수 정보 저장. 및 일정 호의 점수 도달 시 아이템 생성
    }

    private void ResetAltarState()
    {
        PotentialSacrifice = null;
        SacrificeDelayTimer = TickTimer.None;
    }
}