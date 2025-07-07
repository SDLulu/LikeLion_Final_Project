using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ❤️ 플레이어의 체력 시스템을 관리하는 컨트롤러
// 📡 NetworkBehaviour: 네트워크 동기화가 필요한 컴포넌트
public class PlayerHealthController : NetworkBehaviour
{
    // ⚠️ 데미지를 주는 지형 레이어 (용암, 가시 등)
    [SerializeField] private LayerMask deathGroundLayerMask;
    
    // 🩸 피격 시 화면 효과들
    [SerializeField] private Animator bloodScreenHitAnimator; // 피격 화면 효과
    [SerializeField] private PlayerCameraController playerCameraController; // 카메라 흔들림
    
    // 💊 체력 UI 요소들
    [SerializeField] private Image fillAmountImg; // 체력바 게이지
    [SerializeField] private TextMeshProUGUI healthAmountText; // 체력 숫자 텍스트

    // ❤️ 현재 체력 (네트워크 동기화)
    // 모든 플레이어가 같은 체력 값을 공유해야 함
    //
    // 🔑 중요한 Fusion 개념:
    // 📡 [Networked] 프로퍼티는 setter 콜백이 불가능!
    //    - 일반 프로퍼티: set { 로직 실행 } ✅ 가능
    //    - 네트워크 프로퍼티: set { 로직 실행 } ❌ 불가능 (Fusion이 내부 관리)
    //    - 해결 방법: ChangeDetector 사용 → 변화 감지 시 로직 실행
    //
    // ⚡ ChangeDetector의 역할:
    //    - 매 프레임 체크 ❌ (성능 낭비)
    //    - 변화 있을 때만 감지 ✅ (효율적)
    //    - 네트워크 프로퍼티의 "가상 콜백" 역할
    [Networked] 
    private int currentHealthAmount { get; set; }

    // ⚖️ 최대 체력 상수
    private const int MAX_HEALTH_AMOUNT = 100;
    
    // 🔗 컴포넌트 참조들
    private PlayerController playerController;
    private Collider2D coll;
    private ChangeDetector _changeDetector; // 네트워크 상태 변화 감지기
    
    // 🎬 네트워크 오브젝트가 생성될 때 한 번 호출
    public override void Spawned()
    {
        // 🔧 네트워크 상태 변화 감지기 초기화
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        
        // 🔍 필요한 컴포넌트들 가져오기
        coll = GetComponent<Collider2D>();
        playerController = GetComponent<PlayerController>();
        
        // ❤️ 체력을 최대치로 초기화
        currentHealthAmount = MAX_HEALTH_AMOUNT;
    }

    // 📡 네트워크 물리 업데이트 (고정된 프레임레이트로 실행)
    public override void FixedUpdateNetwork()
    {
        // 🖥️ 서버에서만 데미지 체크 (치팅 방지)
        if (Runner.IsServer && playerController.PlayerIsAlive)
        {
            // ⚠️ 데미지 지형과 충돌했는지 물리 검사
            var didHitCollider = Runner.GetPhysicsScene2D()
                .OverlapBox(transform.position, coll.bounds.size, 0, deathGroundLayerMask);
                
            // 💀 데미지 지형에 닿았으면 즉사 데미지
            if (didHitCollider != default)
            {
                Rpc_ReducePlayerHealth(MAX_HEALTH_AMOUNT); // 최대 데미지 = 즉사
            }
        }
    }

    // 💥 플레이어 체력을 감소시키는 RPC
    // 🖥️ sources: StateAuthority (서버만 호출 가능)
    // 🖥️ targets: StateAuthority (서버에서만 실행)
    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void Rpc_ReducePlayerHealth(int damage)
    {
        // ❤️ 현재 체력에서 데미지만큼 차감
        currentHealthAmount -= damage;
    }

    // 🎨 렌더링 업데이트 (시각적 요소 처리)
    public override void Render()
    {
        // 🔍 네트워크 상태 변화 감지
        // 💡 ChangeDetector = 네트워크 프로퍼티의 "스마트 콜백 시스템"
        //    - currentHealthAmount가 변했을 때만 실행 (효율적!)
        //    - 매 프레임 무의미한 체크 방지
        foreach (var change in _changeDetector.DetectChanges(this, out var prev, out var current))
        {
            switch (change)
            {
                // ❤️ 체력이 변경되었을 때
                case nameof(currentHealthAmount):
                    // 📖 이전 체력과 현재 체력 읽기
                    var reader = GetPropertyReader<int>(nameof(currentHealthAmount));
                    var (oldHealth, currentHealth) = reader.Read(prev, current);
                    
                    // ⚡ 체력 변화 처리
                    HealthAmountChanged(oldHealth, currentHealth);
                    break;
            }
        }
    }

    // 🔄 체력이 변경되었을 때 처리하는 함수
    private void HealthAmountChanged(int oldHealth, int currentHealth)
    {
        // ✅ 실제로 체력이 변했는지 확인 (중복 처리 방지)
        if (currentHealth != oldHealth)
        {
            // 🎨 UI 업데이트 (체력바, 숫자)
            UpdateVisuals(currentHealth);

            // 💊 리스폰이나 초기 스폰이 아닌 경우 (실제 피격)
            if (currentHealth != MAX_HEALTH_AMOUNT)
            {
               // 🩸 피격 효과 처리
               PlayerGotHit(currentHealth);
            }
        }
    }

    // 🎨 체력 UI를 업데이트하는 함수
    private void UpdateVisuals(int healthAmount)
    {
        // 📊 체력 비율 계산 (0.0 ~ 1.0)
        var num = (float)healthAmount / MAX_HEALTH_AMOUNT;
        
        // 📈 체력바 게이지 업데이트
        fillAmountImg.fillAmount = num;
        
        // 📝 체력 텍스트 업데이트 (예: "75/100")
        healthAmountText.text = $"{healthAmount}/{MAX_HEALTH_AMOUNT}";
    }

    // 🩸 플레이어가 피격당했을 때 처리하는 함수
    private void PlayerGotHit(int healthAmount)
    {
        // 🎮 로컬 플레이어인 경우에만 화면 효과 실행
        if (Utils.IsLocalPlayer(Object))
        {
            Debug.Log("LOCAL PLAYER GOT HIT!");

            // 🩸 피격 화면 효과 재생
            const string BLOOD_HIT_CLIP_NAME = "BloodScreenHit";
            bloodScreenHitAnimator.Play(BLOOD_HIT_CLIP_NAME);

            // 📱 카메라 흔들림 효과
            var shakeAmount = new Vector3(0.2f, 0.1f);
            playerCameraController.ShakeCamera(shakeAmount);
        }

        // 💀 체력이 0 이하면 플레이어 사망
        if (healthAmount <= 0)
        {
            playerController.KillPlayer();
            Debug.Log("Player is DEAD!");
        }
    }

    // 💊 체력을 최대치로 회복하는 함수 (리스폰 시 사용)
    public void ResetHealthAmountToMax()
    {
        currentHealthAmount = MAX_HEALTH_AMOUNT; 
    }
}
