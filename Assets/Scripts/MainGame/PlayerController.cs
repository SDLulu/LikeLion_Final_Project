using Fusion;
using Fusion.Addons.Physics;
using TMPro;
using UnityEngine;

// 🎮 플레이어의 핵심 컨트롤러 클래스
// NetworkBehaviour: Fusion에서 네트워크 오브젝트의 기본 클래스
// IBeforeUpdate: Fusion 업데이트 루프 전에 호출되는 인터페이스
public class PlayerController : NetworkBehaviour, IBeforeUpdate
{
    // 🎯 플레이어가 입력을 받을 수 있는 조건들을 체크
    // 살아있고 + 게임이 끝나지 않았고 + 채팅 중이 아닐 때만 입력 허용
    public bool AcceptAnyInput => PlayerIsAlive && !GameManager.MatchIsOver && !playerChatController.IsTyping;
    
    // 🔧 컴포넌트 참조들
    [SerializeField] private PlayerChatController playerChatController;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject cam; // 플레이어 전용 카메라
    [SerializeField] private float moveSpeed = 6;
    [SerializeField] private float jumpForce = 1000;

    [Header("Grounded Vars")] 
    [SerializeField] private LayerMask groundLayer; // 땅으로 인식할 레이어
    [SerializeField] private Transform groundDetectionObj; // 땅 감지용 오브젝트

    // 🌐 네트워크 동기화되는 변수들 [Networked]
    // 모든 클라이언트에서 같은 값을 유지해야 하는 중요한 데이터들
    [Networked] public TickTimer RespawnTimer { get; private set; } // 리스폰 타이머
    [Networked] public NetworkBool PlayerIsAlive { get; private set; } // 플레이어 생존 상태
    [Networked] private NetworkString<_8> playerName { get; set; } // 플레이어 이름 (최대 8자)
    [Networked] private NetworkButtons buttonsPrev { get; set; } // 이전 프레임 버튼 상태
    [Networked] private Vector2 serverNextSpawnPoint { get; set; } // 다음 스폰 위치
    [Networked] private NetworkBool isGrounded { get; set; } // 땅에 닿아있는지 여부
    [Networked] private TickTimer respawnToNewPointTimer { get; set; } // 새 위치 이동 타이머

    // 🎮 로컬 변수들 (네트워크 동기화 안됨)
    private float horizontal; // 좌우 이동 입력값
    private Rigidbody2D rigid; // 물리 컴포넌트
    private PlayerWeaponController playerWeaponController;
    private PlayerVisualController playerVisualController;
    private PlayerHealthController playerHealthController;
    private ChangeDetector _changeDetector; // 네트워크 상태 변화 감지기

    // 🎯 플레이어가 누를 수 있는 버튼들을 정의
    public enum PlayerInputButtons
    {
        None,
        Jump,  // 점프
        Shoot  // 사격
    }

    // 🚀 네트워크 오브젝트가 생성될 때 한 번만 호출 (생성자 같은 역할)
    public override void Spawned()
    {
        // 이 오브젝트가 물리 시뮬레이션에 참여하도록 설정
        Runner.SetIsSimulated(Object, true);
        
        // 🔧 필요한 컴포넌트들 가져오기
        rigid = GetComponent<Rigidbody2D>();
        playerWeaponController = GetComponent<PlayerWeaponController>();
        playerVisualController = GetComponent<PlayerVisualController>();
        playerHealthController = GetComponent<PlayerHealthController>();
        
        // 네트워크 상태 변화를 감지할 수 있는 도구 생성
        // 🌐 시뮬레이션 상태 = [Networked] 프로퍼티들
        // false = 특수 데이터 제외
        // 기본적인 [Networked] 프로퍼티만 감지// 특별한 메타데이터나 내부 상태는 제외// 성능 최적화 (불필요한 데이터 제외)
        // true = 특수 데이터 포함
        // 모든 네트워크 관련 데이터 감지// 메타데이터, 내부 상태 등도 포함// 더 상세한 감지 가능하지만 성능 부담
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState, false);
        
        // 📝 플레이어 스포너에 이 플레이어 등록
        GlobalManagers.Instance.PlayerSpawnerController.AddToEntry(base.Object.InputAuthority, base.Object);
        
        // 🎯 로컬 플레이어 전용 설정 (카메라, 닉네임 등)
        SetLocalObjects();
        PlayerIsAlive = true;
    }

    // 🎮 로컬 플레이어와 원격 플레이어를 구분해서 초기 설정
    private void SetLocalObjects()
    {
        // 내가 조종하는 플레이어인 경우
        if (Utils.IsLocalPlayer(Object))
        {
            // 카메라를 플레이어에서 분리하고 활성화 (따라다니도록)
            cam.transform.SetParent(null);
            cam.SetActive(true);

            // 닉네임을 서버에 전송
            var nickName = GlobalManagers.Instance.NetworkRunnerController.LocalPlayerNickname;
            RpcSetNickName(nickName);
        }
        else
        {
            // 다른 플레이어의 경우 (원격 플레이어)
            // 예측 시뮬레이션 대신 보간을 사용하여 성능 최적화
            // 렌더링 소스를 보간으로 설정하여 부드러운 움직임 보장
            base.Object.RenderSource = RenderSource.Interpolated;
            base.Object.ForceRemoteRenderTimeframe = true;
        }
    }

    // 📡 RPC (Remote Procedure Call): 네트워크를 통해 다른 클라이언트의 함수 호출
    // sources: 이 RPC를 호출할 수 있는 권한 (InputAuthority = 플레이어 본인만)
    // RpcTargets: 이 함수가 실행될 대상 (StateAuthority = 서버에서만 실행)
    [Rpc(sources: RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RpcSetNickName(NetworkString<_8> nickName)
    {
        // 서버에서 플레이어 이름 설정
        playerName = nickName;
    }

    // 🔄 플레이어 이름이 변경되었을 때 호출되는 콜백
    private void OnNicknameChanged( NetworkString<_8> nickName)
    {
        SetPlayerNickname(nickName);
    }

    // 🏷️ UI에 플레이어 이름 표시
    private void SetPlayerNickname(NetworkString<_8> nickName)
    {
        playerNameText.text = nickName + " " + Object.InputAuthority.PlayerId;
    }

    // 💀 플레이어 사망 처리
    public void KillPlayer()
    {
        const int RESPAWN_AMOUNT = 5; // 5초 후 리스폰
        
        // 서버에서만 실행되는 로직
        if (Runner.IsServer)
        {
            // 새로운 스폰 위치 결정
            serverNextSpawnPoint = GlobalManagers.Instance.PlayerSpawnerController.GetRandomSpawnPoint();
            // 스폰 위치 이동 타이머 설정 (4초 후)
            respawnToNewPointTimer = TickTimer.CreateFromSeconds(Runner, RESPAWN_AMOUNT - 1);
        }
        
        // 플레이어 상태 변경
        PlayerIsAlive = false;
        rigid.simulated = false; // 물리 시뮬레이션 중지
        playerVisualController.TriggerDieAnimation(); // 사망 애니메이션
        RespawnTimer = TickTimer.CreateFromSeconds(Runner, RESPAWN_AMOUNT); // 리스폰 타이머 시작
    }

    // ⚡ Fusion 업데이트 루프 전에 호출 (매 프레임)
    // 입력 수집 등 Fusion이 처리하기 전에 해야 할 작업들
    public void BeforeUpdate()
    {
        // 로컬 플레이어이고 입력을 받을 수 있는 상태일 때만
        if (Utils.IsLocalPlayer(Object) && AcceptAnyInput)
        {
            const string HORIZONTAL = "Horizontal";
            // Unity 입력 시스템에서 좌우 이동 값 가져오기 (-1, 0, 1)
            horizontal = Input.GetAxisRaw(HORIZONTAL);
        }
    }

    // 🔄 고정 업데이트 네트워크 (Fusion의 핵심 업데이트 루프)
    // 물리 시뮬레이션과 네트워크 동기화가 여기서 처리됨
    public override void FixedUpdateNetwork()
    {
        // 리스폰 타이머 체크
        CheckRespawnTimer();

        // 🎮 이 플레이어의 입력 데이터 가져오기
        // TryGetInputForPlayer: 해당 플레이어의 입력을 안전하게 가져오는 함수
        if (Runner.TryGetInputForPlayer<PlayerData>(Object.InputAuthority, out var input))
        {
            if (AcceptAnyInput)
            {
                // 🏃 좌우 이동 처리 (Y축 속도는 유지)
                rigid.linearVelocity = new Vector2(input.HorizontalInput * moveSpeed, rigid.linearVelocity.y);

                // 🦘 점프 입력 체크
                CheckJumpInput(input);

                // 이전 버튼 상태 저장 (다음 프레임에서 비교용)
                buttonsPrev = input.NetworkButtons;
            }
            else
            {
                // 입력을 받을 수 없는 상태면 정지
                rigid.linearVelocity = Vector2.zero;
            }
        }

        // 🎨 시각적 업데이트 (애니메이션, 방향 등)
        playerVisualController.UpdateScaleTransforms(rigid.linearVelocity);
    }

    // ⏰ 리스폰 타이머들을 체크하는 함수
    private void CheckRespawnTimer()
    {
        if (PlayerIsAlive) return; // 살아있으면 리스폰 필요 없음

        // 🌐 서버에서만 실행: 새로운 스폰 위치로 이동
        if (respawnToNewPointTimer.Expired(Runner))
        {
            // 네트워크 리지드바디를 사용해서 텔레포트 (네트워크 동기화됨)
            GetComponent<NetworkRigidbody2D>().Teleport(serverNextSpawnPoint);
            respawnToNewPointTimer = TickTimer.None; // 타이머 리셋
        }
        
        // 🔄 리스폰 타이머가 끝나면 플레이어 부활
        if (RespawnTimer.Expired(Runner))
        {
            RespawnTimer = TickTimer.None;
            RespawnPlayer();
        }
    }

    // 🌟 플레이어 리스폰 처리
    private void RespawnPlayer()
    {
        PlayerIsAlive = true;
        rigid.simulated = true; // 물리 시뮬레이션 재개
        playerVisualController.TriggerRespawnAnimation(); // 리스폰 애니메이션
        playerHealthController.ResetHealthAmountToMax(); // 체력 최대치로 회복
    }

    // 🎨 렌더링 업데이트 (시각적 요소들)
    // 네트워크 상태 변화 감지 및 시각적 업데이트 처리
    public override void Render()
    {
        // 🔍 네트워크 상태 변화 감지
        foreach (var change in _changeDetector.DetectChanges(this, out var prev, out var current))
        {
            switch (change)
            {
                case nameof(playerName):
                    // 플레이어 이름이 변경되었을 때
                    var reader = GetPropertyReader<NetworkString<_8>>(nameof(playerName));
                    var (oldName, currentName) = reader.Read(prev, current);
                    OnNicknameChanged(currentName);
                    break;
            }
        }
        
        // 🎭 시각적 렌더링 업데이트
        playerVisualController.RendererVisuals(rigid.linearVelocity, playerWeaponController.IsHoldingShootingKey);
    }

    // 🦘 점프 입력 처리
    private void CheckJumpInput(PlayerData input)
    {
        // 🔍 땅에 닿아있는지 물리 검사
        var transform1 = groundDetectionObj.transform;
        isGrounded = (bool)Runner.GetPhysicsScene2D().OverlapBox(transform1.position,
            transform1.localScale, 0, groundLayer);

        // 땅에 닿아있을 때만 점프 가능
        if (isGrounded)
        {
            // 🔘 버튼이 눌렸는지 체크 (이전 프레임과 비교)
            var pressed = input.NetworkButtons.GetPressed(buttonsPrev);
            if (pressed.WasPressed(buttonsPrev, PlayerInputButtons.Jump))
            {
                // 위쪽으로 힘을 가해서 점프
                rigid.AddForce(Vector2.up * jumpForce, ForceMode2D.Force);
            }
        }
    }

    // 🗑️ 네트워크 오브젝트가 제거될 때 호출 (소멸자 같은 역할)
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // 오브젝트 풀링 매니저에서 제거
        GlobalManagers.Instance.ObjectPoolingManager.RemoveNetworkObjectFromDic(Object);
        // 게임 오브젝트 완전 삭제
        Destroy(gameObject);
    }

    // 🎮 로컬 플레이어의 입력을 네트워크 데이터로 변환
    // LocalInputPoller에서 호출되어 서버로 전송될 입력 데이터 생성
    public PlayerData GetPlayerNetworkInput()
    {
        PlayerData data = new PlayerData();
        data.HorizontalInput = horizontal; // 좌우 이동
        data.GunPivotRotation = playerWeaponController.LocalQuaternionPivotRot; // 무기 회전
        data.NetworkButtons.Set(PlayerInputButtons.Jump, Input.GetKey(KeyCode.Space)); // 점프 키
        data.NetworkButtons.Set(PlayerInputButtons.Shoot, Input.GetButton("Fire1")); // 사격 키
        return data;
    }
}