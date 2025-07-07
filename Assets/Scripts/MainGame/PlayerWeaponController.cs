using Fusion;
using UnityEngine;

// 🔫 플레이어의 무기 시스템을 관리하는 컨트롤러
// 📡 NetworkBehaviour: 네트워크 동기화가 필요한 컴포넌트
// ⚡ IBeforeUpdate: Unity Update 전에 실행되는 Fusion 인터페이스 (로컬 입력 처리용)
public class PlayerWeaponController : NetworkBehaviour, IBeforeUpdate
{
    // 🎯 로컬 플레이어의 무기 회전값 (다른 스크립트에서 읽기 전용 접근 가능)
    public Quaternion LocalQuaternionPivotRot { get; private set; }
    
    // 🧩 네트워크 프리팹 참조 - 생성할 총알 프리팹
    [SerializeField] private NetworkPrefabRef bulletPrefab = NetworkPrefabRef.Empty;
    
    // 📍 총알이 발사될 위치
    [SerializeField] private Transform firePointPos;
    
    // ⏱️ 연속 사격 간 딜레이 시간 (초)
    [SerializeField] private float delayBetweenShots = 0.18f;
    
    // ✨ 총구 화염 파티클 효과
    [SerializeField] private ParticleSystem muzzleEffect;
    
    // 📷 로컬 플레이어의 카메라 (마우스 위치 계산용)
    [SerializeField] private Camera localCam;
    
    // 🔄 회전시킬 무기 피벗 (총구 방향 조정용)
    [SerializeField] private Transform pivotToRotate;

    // 📡 네트워크 동기화 변수들
    // 🎮 현재 사격 키를 누르고 있는지 여부 (다른 플레이어들이 볼 수 있음)
    [Networked, HideInInspector] public NetworkBool IsHoldingShootingKey { get; private set; }
    
    // 💥 총구 화염 효과를 재생할지 여부 (네트워크 동기화)
    [Networked] private NetworkBool playMuzzleEffect { get; set; }
    
    // 🎯 현재 플레이어 무기의 회전값 (모든 클라이언트에서 동기화)
    [Networked] private Quaternion currentPlayerPivotRotation { get; set; }
    
    // 🔘 이전 프레임의 버튼 상태 (버튼 눌림/떼짐 감지용)
    [Networked] private NetworkButtons buttonsPrev { get; set; }
    
    // ⏳ 사격 쿨다운 타이머 (연사 제한용)
    [Networked] private TickTimer shootCoolDown { get; set; }

    // 🔗 컴포넌트 참조들
    private PlayerController playerController;
    
    // 👁️ 네트워크 상태 변화 감지기 (효과 재생용)
    private ChangeDetector _changeDetector;

    // 🎬 네트워크 오브젝트가 생성될 때 한 번 호출
    public override void Spawned()
    {
        // ⚙️ 이 오브젝트의 시뮬레이션을 활성화 (물리 계산 등)
        Runner.SetIsSimulated(Object, true);
        
        // 🔍 같은 GameObject의 PlayerController 컴포넌트 가져오기
        playerController = GetComponent<PlayerController>();
        
        // 🔧 네트워크 상태 변화를 감지하기 위한 ChangeDetector 초기화
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    // ⚡ Unity Update 전에 실행 (로컬 입력 처리용)
    // 🚀 네트워크 지연 없이 즉시 반응해야 하는 로컬 작업들을 처리
    public void BeforeUpdate()
    {
        // ✅ 로컬 플레이어이고 입력을 받을 수 있는 상태인지 확인
        if (Utils.IsLocalPlayer(Object) && playerController.AcceptAnyInput)
        {
            // 🖱️ 마우스 위치를 월드 좌표로 변환하고 플레이어 위치와의 방향 벡터 계산
            var direction = localCam.ScreenToWorldPoint(Input.mousePosition) - transform.position;

            // 📐 방향 벡터를 각도로 변환 (2D에서 마우스 방향 계산)
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 🔄 계산된 각도를 Quaternion으로 변환하여 저장
            LocalQuaternionPivotRot = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    // 📡 네트워크 물리 업데이트 (고정된 프레임레이트로 실행)
    // 🎯 모든 네트워크 로직은 여기서 처리
    public override void FixedUpdateNetwork()
    {
        // 🎮 이 오브젝트의 입력 권한을 가진 플레이어의 입력 데이터 가져오기
        if (Runner.TryGetInputForPlayer<PlayerData>(Object.InputAuthority, out var input))
        {
            // ✅ 입력을 받을 수 있는 상태인지 확인
            if (playerController.AcceptAnyInput)
            {
                // 🔫 사격 입력 처리
                CheckShootInput(input);
                
                // 📡 네트워크를 통해 받은 무기 회전값 적용
                currentPlayerPivotRotation = input.GunPivotRotation;

                // 💾 다음 프레임에서 버튼 상태 비교를 위해 현재 상태 저장
                buttonsPrev = input.NetworkButtons;
            }
            else
            {
                // 🚫 입력을 받을 수 없는 상태일 때 모든 값 초기화
                IsHoldingShootingKey = false;
                playMuzzleEffect = false;
                buttonsPrev = default;
            }
        }

        // 🔄 모든 클라이언트에서 무기 회전 동기화
        pivotToRotate.rotation = currentPlayerPivotRotation;
    }

    // 🎯 사격 입력을 확인하고 처리하는 함수
    private void CheckShootInput(PlayerData input)
    {
        // 🔍 이전 프레임과 비교하여 새로 눌린 버튼들 감지
        var currentBtns = input.NetworkButtons.GetPressed(buttonsPrev);

        // 🔘 사격 키가 떼어졌는지 확인 (버튼을 누르고 떼는 순간에 발사)
        IsHoldingShootingKey = currentBtns.WasReleased(buttonsPrev, PlayerController.PlayerInputButtons.Shoot);
        
        // ✅ 사격 키가 떼어지고 쿨다운이 끝났는지 확인
        if (currentBtns.WasReleased(buttonsPrev, PlayerController.PlayerInputButtons.Shoot) && shootCoolDown.ExpiredOrNotRunning(Runner))
        {
            // 💥 총구 화염 효과 재생 플래그 설정
            playMuzzleEffect = true;
            
            // ⏳ 다음 사격까지의 쿨다운 타이머 설정
            shootCoolDown = TickTimer.CreateFromSeconds(Runner, delayBetweenShots);

            // 🖥️ 서버에서만 총알 생성 (네트워크 오브젝트는 서버에서 생성)
            if (Runner.IsServer)
            {
                // 🚀 총알 프리팹을 발사 위치와 회전으로 생성
                // 👤 InputAuthority를 전달하여 총알의 소유권 설정
                Runner.Spawn(bulletPrefab, firePointPos.position, firePointPos.rotation, Object.InputAuthority);
            }
        }
        else
        {
            // 🚫 사격하지 않을 때는 총구 화염 효과 중지
            playMuzzleEffect = false;
        }
    }

    // 🎨 렌더링 업데이트 (클라이언트별로 다른 프레임레이트로 실행)
    // 👁️ 주로 시각적 효과나 UI 업데이트 처리
    public override void Render()
    {
        // 🔍 네트워크 상태 변화 감지 및 처리
        foreach (var change in _changeDetector.DetectChanges(this, out var prev, out var current))
        {
            switch (change)
            {
                // 💥 총구 화염 효과 상태가 변경되었을 때
                case nameof(playMuzzleEffect):
                    // 📖 이전 상태와 현재 상태를 읽어옴
                    var reader = GetPropertyReader<NetworkBool>(nameof(playMuzzleEffect));
                    var (oldState, currentState) = reader.Read(prev, current);
                    
                    // ⚡ 상태에 따라 총구 화염 효과 재생/중지
                    PlayOrStopMuzzleEffect(currentState);
                    break;
            }
        }
    }
    
    // ✨ 총구 화염 효과를 재생하거나 중지하는 함수
    private void PlayOrStopMuzzleEffect(bool play)
    {
        if (play)
        {
            // 🎆 파티클 효과 재생
            muzzleEffect.Play();
        }
        else
        {
            // ⏹️ 파티클 효과 중지
            muzzleEffect.Stop();
        }
    }
    
    
    
    
    
    
    
    
    
    
    
}















