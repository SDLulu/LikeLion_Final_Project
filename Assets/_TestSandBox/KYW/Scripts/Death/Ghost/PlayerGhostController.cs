using Fusion;
using UnityEngine;

// 👻 유령 플레이어 컨트롤러
// 플레이어가 죽은 후 유령으로 조작하는 시스템
public class PlayerGhostController : NetworkBehaviour, IBeforeUpdate
{
    [Header("유령 이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("입김 설정")]
    [SerializeField] private GameObject breathPrefab; // 입김 프리팹
    [SerializeField] private float breathSpeed = 2f; // 입김 속도
    [SerializeField] private float breathDuration = 0.5f; // 입김 지속시간
    [SerializeField] private float breathCooldown = 0.5f; // 입김 쿨다운
    [SerializeField] private Transform breathSpawnPoint; // 입김 스폰 위치 (유령 입 부분)
    
    [Header("대쉬 설정")]
    [SerializeField] private float dashDistance = 3f; // 대쉬 거리
    [SerializeField] private float dashDuration = 0.2f; // 대쉬 지속시간
    [SerializeField] private float dashCooldown = 1f; // 대쉬 쿨다운
    
    // 네트워크 상태
    [Networked] private Vector3 moveDirection { get; set; }
    [Networked] private float rotationAngle { get; set; }
    [Networked] private TickTimer breathCooldownTimer { get; set; } // 입김 쿨다운 타이머
    [Networked] private TickTimer dashCooldownTimer { get; set; } // 대쉬 쿨다운 타이머
    [Networked] private TickTimer dashTimer { get; set; } // 대쉬 진행 타이머
    [Networked] private Vector2 dashDirection { get; set; } // 대쉬 방향
    [Networked] private bool isDashing { get; set; } // 대쉬 중인지
    
    // 참조
    private NetworkObject originalPlayer;
    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    
    
    // 입력 변수들
    private float horizontalInput;
    private float verticalInput;
    private Vector2 mouseWorldPosition;
    private bool leftClickPressed;
    private bool spacePressed;
    
    public override void Spawned()
    {
        // 스프라이트 렌더러 찾기
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        if (spriteRenderer == null)
        {
            Debug.LogError($"[{name}] SpriteRenderer를 찾을 수 없습니다!");
            return;
        }
        
        // 초기 위치 저장
        startPosition = transform.position;
        
        
        Debug.Log($"[{name}] 유령 플레이어 컨트롤러 초기화 완료!");
    }
    
    // 🎮 입력 수집 (매 프레임)
    public void BeforeUpdate()
    {
        if (!Object.HasInputAuthority) return;
        
        // 방향키 입력
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        
        // 마우스 월드 위치 계산
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        
        // 클릭 입력 (Press 방식으로 변경)
        leftClickPressed = Input.GetMouseButton(0);
        
        // 스페이스 입력 (대쉬)
        spacePressed = Input.GetKeyDown(KeyCode.Space);
    }
    
    public override void FixedUpdateNetwork()
    {
        // 권한 확인 (호스트/서버에서만 실행)
        if (!HasStateAuthority) return;
        
        // 입력이 필요한 것들 (InputAuthority에서만)
        if (Runner.TryGetInputForPlayer<GhostInputData>(Object.InputAuthority, out var input))
        {
            ProcessDashing(input);
            ProcessMovement(input);
            ProcessRotation(input);
            ProcessBreathing(input);
        }
    }
    
    // 🚀 대쉬 처리 (스페이스)
    private void ProcessDashing(GhostInputData input)
    {
        // 대쉬 중일 때 처리
        if (isDashing)
        {
            if (dashTimer.Expired(Runner))
            {
                // 대쉬 종료
                isDashing = false;
                Debug.Log($"[{name}] 대쉬 종료");
            }
            else
            {
                // 대쉬 이동
                float dashSpeed = dashDistance / dashDuration;
                Vector3 dashMove = (Vector3)dashDirection * dashSpeed * Runner.DeltaTime;
                transform.position += dashMove;
            }
            return;
        }
        
        // 대쉬 시작 체크
        if (input.NetworkButtons.IsSet(GhostInputButtons.Space) && dashCooldownTimer.ExpiredOrNotRunning(Runner))
        {
            StartDash(input.MouseWorldPosition);
        }
    }
    
    // 🚀 대쉬 시작
    private void StartDash(Vector2 mouseWorldPosition)
    {
        // 대쉬 방향 계산 (마우스 방향)
        Vector2 dashDir = (mouseWorldPosition - (Vector2)transform.position).normalized;
        
        // 만약 마우스가 너무 가까우면 현재 바라보는 방향으로 대쉬
        if (Vector2.Distance(mouseWorldPosition, transform.position) < 0.5f)
        {
            float currentAngle = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
            dashDir = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle));
        }
        
        // 대쉬 시작
        dashDirection = dashDir;
        isDashing = true;
        dashTimer = TickTimer.CreateFromSeconds(Runner, dashDuration);
        dashCooldownTimer = TickTimer.CreateFromSeconds(Runner, dashCooldown);
        
        Debug.Log($"[{name}] 대쉬 시작: 방향={dashDirection}, 거리={dashDistance}");
    }
    
    // 🏃 유령 이동 (공중 부유)
    private void ProcessMovement(GhostInputData input)
    {
        // 대쉬 중에는 일반 이동 금지
        if (isDashing) return;
        
        Vector3 direction = new Vector3(input.HorizontalInput, input.VerticalInput, 0);
        Vector3 newPosition = transform.position + direction * moveSpeed * Runner.DeltaTime;
        
        // 물체 통과 가능 (Collider2D 없음)
        transform.position = newPosition;
    }
    
    // 🔄 회전 (마우스 방향)
    private void ProcessRotation(GhostInputData input)
    {
        Vector2 direction = input.MouseWorldPosition - (Vector2)transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    // 💨 입김 (좌클릭)
    private void ProcessBreathing(GhostInputData input)
    {
        // 쿨다운 체크
        if (!breathCooldownTimer.ExpiredOrNotRunning(Runner)) return;
        
        if (input.NetworkButtons.IsSet(GhostInputButtons.LeftClick))
        {
            // 입김 프리팹 스폰
            SpawnBreathEffect();
            
            // 쿨다운 설정
            breathCooldownTimer = TickTimer.CreateFromSeconds(Runner, breathCooldown);
            
            Debug.Log($"[{name}] 유령 입김!");
        }
    }
    
    // 💨 입김 이펙트 생성
    private void SpawnBreathEffect()
    {
        if (!HasStateAuthority || breathPrefab == null) return;
        
        // 마우스 방향 계산 (월드 좌표 기준)
        Vector2 worldDir = (mouseWorldPosition - (Vector2)transform.position).normalized;
        
        // 입김 스폰 위치 결정 (입 부분이 있으면 사용, 없으면 유령 위치)
        Vector3 spawnPosition = breathSpawnPoint != null ? breathSpawnPoint.position : transform.position;
        
        // 입김 방향에 맞는 회전 계산 (월드 좌표 기준)
        float angle = Mathf.Atan2(worldDir.y, worldDir.x) * Mathf.Rad2Deg;
        Quaternion breathRotation = Quaternion.Euler(0, 0, angle);
        
        // 입김 프리팹을 독립적으로 스폰 (부모 없이)
        var breath = Runner.Spawn(breathPrefab, spawnPosition, breathRotation, Object.InputAuthority);
        
        // 입김 컨트롤러 설정 (월드 방향 전달)
        var breathController = breath.GetComponent<GhostBreathController>();
        if (breathController != null)
        {
            breathController.InitializeBreath(worldDir, breathSpeed, breathDuration);
        }
        
        Debug.Log($"[{name}] 입김 프리팹 스폰: {breath.name} (위치: {spawnPosition}, 회전: {angle:F1}도, 방향: {worldDir})");
    }
    
    // 🔗 원래 플레이어 설정
    public void SetOriginalPlayer(NetworkObject player)
    {
        originalPlayer = player;
        Debug.Log($"[{name}] 원래 플레이어 설정: {player?.name ?? "null"}");
    }
    
    // 🔄 부활 시 유령 제거
    public void DespawnGhost()
    {
        if (HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }
    
    // 📡 입력 데이터 생성 (GhostLocalInputPoller에서 호출)
    public GhostInputData GetNetworkInputData()
    {
        GhostInputData data = new GhostInputData();
        
        if (Object.HasInputAuthority)
        {
            data.HorizontalInput = horizontalInput;
            data.VerticalInput = verticalInput;
            data.MouseWorldPosition = mouseWorldPosition;
            
            // 🔘 버튼 입력 설정
            data.NetworkButtons.Set(GhostInputButtons.LeftClick, leftClickPressed);
            data.NetworkButtons.Set(GhostInputButtons.Space, spacePressed);
        }
        
        return data;
    }
    
} 