using Fusion;
using UnityEngine;

// 🎮 스펠렁키 플레이어 메인 컨트롤러
// 컴포넌트들을 조합하고 네트워크 동기화 담당
// 
// 📁 권장 구조:
// Player (이 오브젝트) - 물리/로직 컴포넌트들
// ├── Visual - 시각적 컴포넌트들 (SpriteRenderer, Animator, PlayerAnimation)
// └── Hand - 아이템 시스템 (PlayerItemPickup, PlayerItemUsage, CircleCollider2D)
//
// 🔧 설정 방법:
// 1. Visual 하위 오브젝트 생성 후 SpriteRenderer, Animator, PlayerAnimation 이동
// 2. Hand 하위 오브젝트 생성 후 PlayerItemPickup, PlayerItemUsage 이동
// 3. Hand에 CircleCollider2D 추가 (IsTrigger = true, 아이템 감지용)
// 4. Inspector에서 Visual Root, Hand Root 필드에 각각 할당
public class SpelunkyPlayerController : NetworkBehaviour, IBeforeUpdate
{
    [Header("Player State")]
    // 🎮 상태 패턴 관련 필드들
    [Networked] public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
    [Networked] public PlayerAction CurrentActions { get; private set; } = PlayerAction.None;
    
    [Header("Visual References")]
    [SerializeField] private Transform visualRoot; // Visual 하위 오브젝트 참조
    
    [Header("Hand References")]
    [SerializeField] private Transform handRoot; // Hand 하위 오브젝트 참조
    
    // 🎮 입력 변수들 (Fusion 2 공식 방식 - NetworkButtons로 통합)
    private float horizontalInput;
    private float verticalInput;
    private Vector2 mouseWorldPosition;
    private float mouseScrollWheel;     // 마우스 휠 스크롤 값 (아이템 스왑용)
    private bool jumpPressed;       // Space + !IsDucking
    private bool pickupPressed;     // Space + IsDucking  
    
    // 🔨 아이템 사용 입력
    private bool useItemHeld;           // 현재 클릭 유지 중
    private bool throwItemPressed;      // 우클릭 (MouseButton 1)
    private bool skillPressed;          // 쉬프트키 (스킬 사용)

    
    // 📦 물리/로직 컴포넌트 참조들 (같은 오브젝트에서 찾기)
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerClimbing climbing;
    
    // 📦 시각적 컴포넌트 참조들 (하위 오브젝트에서 찾기)
    private PlayerAnimation playerAnimation;
    private SpriteRenderer spriteRenderer;
    
    // 📦 Hand 컴포넌트 참조들 (하위 오브젝트에서 찾기)
    private PlayerItemPickup itemPickup;
    private PlayerItemUsage itemUsage;
    private PlayerItemThrower itemThrower;

    public override void Spawned()
    {
        // 물리/로직 컴포넌트들 (같은 오브젝트에서 찾기)
        groundCheck = GetComponentInChildren<PlayerGroundCheck>();
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
        climbing = GetComponent<PlayerClimbing>();
        
        // 하위 오브젝트들 설정 (Visual, Hand)
        SetupChildObjects();
        
        // 네트워크 설정 분리
        SpelunkyNetworkInitializer.InitializeNetworkSettings(this);
        SpelunkyNetworkInitializer.InitializePlayerType(this);
        
        Debug.Log($"🎮 플레이어 소환 완료! InputAuthority: {Object.HasInputAuthority}, " +
                 $"IsLocalPlayer: {Object.InputAuthority == Runner.LocalPlayer}");
    }
    
    // 📦 하위 오브젝트들 설정 (Visual, Hand)
    private void SetupChildObjects()
    {
        SetupVisualObject();
        SetupHandObject();
    }
    
    // 🎭 Visual 오브젝트 설정
    private void SetupVisualObject()
    {
        // visualRoot가 설정되지 않은 경우 자동으로 찾기
        if (visualRoot == null)
        {
            visualRoot = transform.Find("Visual");
            
            // Visual 오브젝트가 없으면 경고만 출력 (자동 생성 제거)
            if (visualRoot == null)
            {
                Debug.LogWarning($"[{name}] Visual 하위 오브젝트를 찾을 수 없습니다. " +
                               "Inspector에서 Visual Root를 수동으로 설정해주세요.");
            }
        }
    }
    
    // 🤲 Hand 오브젝트 설정
    private void SetupHandObject()
    {
        // handRoot가 설정되지 않은 경우 자동으로 찾기
        if (handRoot == null)
        {
            handRoot = transform.Find("Hand");
            
            // Hand 오브젝트가 없으면 경고만 출력 (자동 생성 제거)
            if (handRoot == null)
            {
                Debug.LogWarning($"[{name}] Hand 하위 오브젝트를 찾을 수 없습니다. " +
                               "Inspector에서 Hand Root를 수동으로 설정해주세요.");
            }
        }
        
        // Hand 컴포넌트들 참조 설정
        itemPickup = handRoot?.GetComponent<PlayerItemPickup>();
        itemUsage = handRoot?.GetComponent<PlayerItemUsage>();
        itemThrower = handRoot?.GetComponent<PlayerItemThrower>();
        
        if (itemPickup == null)
        {
            Debug.LogWarning($"[{name}] Hand 오브젝트에 PlayerItemPickup 컴포넌트가 없습니다.");
        }
        if (itemUsage == null)
        {
            Debug.LogWarning($"[{name}] Hand 오브젝트에 PlayerItemUsage 컴포넌트가 없습니다.");
        }
        if (itemThrower == null)
        {
            Debug.LogWarning($"[{name}] Hand 오브젝트에 PlayerItemThrower 컴포넌트가 없습니다.");
        }
    }
    
    // 🎮 입력 수집 (매 프레임)
    public void BeforeUpdate()
    {
        if (Object.HasInputAuthority)
        {
            // 방향키 입력
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
            
            // 마우스 월드 위치 계산
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            
            // 마우스 휠 스크롤 입력
            mouseScrollWheel = Input.GetAxis("Mouse ScrollWheel");
            
            // 점프 입력 (일관성을 위해 변수로 저장)
            jumpPressed = Input.GetKey(KeyCode.Space) && !(movement?.IsDucking ?? false);
            
            // 🔘 아이템 관련 입력
            pickupPressed = Input.GetKey(KeyCode.Space) && (movement?.IsDucking ?? false);
            
            useItemHeld = Input.GetMouseButton(0);       // 마우스 좌클릭
            throwItemPressed = Input.GetMouseButton(1);  // 마우스 우클릭
            skillPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);  // 쉬프트키
        }
    }
    
    // 🌐 네트워크 고정 업데이트
    public override void FixedUpdateNetwork()
    {
        // 입력이 필요한 것들 (InputAuthority에서만)
        if (Runner.TryGetInputForPlayer<SpelunkyPlayerData>(Object.InputAuthority, out var input))
        {
            // 기존 컴포넌트들 처리 (그대로 유지)
            movement?.ProcessInput(input);
            jump?.ProcessInput(input);
            climbing?.ProcessInput(input);
            itemPickup?.ProcessInput(input);
            itemUsage?.ProcessInput(input);
            itemThrower?.ProcessInput(input);
        }
        
        // 1번 키 입력 체크 (InputAuthority에서만)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Object.HasInputAuthority && Input.GetKeyDown(KeyCode.Alpha1))
        {
            var inventory = GetComponentInChildren<PlayerInventory>();
            if (inventory != null)
            {
                var held = inventory.CurrentHeldObject;
                string heldName = held != null ? held.name : "없음";
                Debug.Log($"[SpelunkyPlayerController] 현재 손에 든 오브젝트: {heldName}");
            }
            else
            {
                Debug.Log("[SpelunkyPlayerController] PlayerInventory 컴포넌트를 찾을 수 없습니다.");
            }
        }
#endif
        // 🎮 상태 업데이트 (새로 추가)
        UpdateCurrentState();
        UpdateCurrentActions();
    }

    // 📡 입력 데이터 생성 (LocalInputPoller에서 호출)
    public SpelunkyPlayerData GetNetworkInputData()
    {
        SpelunkyPlayerData data = new SpelunkyPlayerData();
        
        if (Object.HasInputAuthority)
        {
            data.HorizontalInput = horizontalInput;
            data.VerticalInput = verticalInput;
            data.MouseWorldPosition = mouseWorldPosition;
            data.MouseScrollWheel = mouseScrollWheel;
            
            // 🔘 버튼 입력 설정
            data.NetworkButtons.Set(SpelunkyInputButtons.Jump, jumpPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.PickupItem, pickupPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.UseItemHold, useItemHeld);
            data.NetworkButtons.Set(SpelunkyInputButtons.ThrowItem, throwItemPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.Skill, skillPressed);
        }
        
        return data;
    }
    
    // 🎮 현재 상태 업데이트 (기존 컴포넌트들의 상태를 읽어서 결정)
    private void UpdateCurrentState()
    {
        // StateAuthority에서만 상태 변경
        if (!Object.HasStateAuthority) return;
        
        PlayerState newState = DetermineCurrentState();
        
        // 상태가 변경된 경우에만 업데이트
        if (CurrentState != newState)
        {
            CurrentState = newState;
            Debug.Log($"🎮 상태 변경: {CurrentState}");
        }
    }
    
    // 🎮 현재 액션 업데이트
    private void UpdateCurrentActions()
    {
        // StateAuthority에서만 액션 변경
        if (!Object.HasStateAuthority) return;
        
        PlayerAction newActions = PlayerAction.None;
        
        // 아이템 사용 중인지 확인 (간단하게 입력으로 판단)
        if (useItemHeld)
        {
            newActions |= PlayerAction.UsingItem;
        }
        
        // 스킬 사용 중인지 확인 (PlayerShiftSkill이 있다면)
        var shiftSkill = GetComponent<PlayerShiftSkill>();
        if (shiftSkill != null && shiftSkill.IsSkillActive)
        {
            newActions |= PlayerAction.UsingSkill;
        }
        
        // 액션이 변경된 경우에만 업데이트
        if (CurrentActions != newActions)
        {
            CurrentActions = newActions;
        }
    }
    
    // 🎮 현재 상태 결정 (기존 컴포넌트들의 상태를 기반으로)
    private PlayerState DetermineCurrentState()
    {
        // 사망 상태 체크
        if (CurrentState == PlayerState.Dead) return PlayerState.Dead;
        
        // 기존 컴포넌트들의 상태를 읽어서 결정
        bool isGrounded = groundCheck?.IsGrounded ?? false;
        bool isClimbing = climbing?.IsClimbing ?? false;
        bool isJumping = jump?.IsJumping ?? false;
        bool isDucking = movement?.IsDucking ?? false;
        
        // 🎯 순환 의존성 방지: 현재 Ducking 상태일 때는 조건을 더 엄격하게 체크
        if (CurrentState == PlayerState.Ducking)
        {
            // Ducking 상태에서 벗어나는 조건들
            if (!isGrounded) return PlayerState.Falling;
            if (isClimbing) return PlayerState.Climbing;
            if (isJumping) return PlayerState.Jumping;
            if (!isDucking) return PlayerState.Idle; // 웅크리기 해제 시 Idle로
            return PlayerState.Ducking; // 계속 웅크리기 중
        }
        
        // 일반적인 우선순위에 따라 상태 결정
        if (isClimbing) return PlayerState.Climbing;
        if (isJumping) return PlayerState.Jumping;
        if (isGrounded)
        {
            if (isDucking) return PlayerState.Ducking;
            if (movement?.NormalizedSpeed > 0.1f) return PlayerState.Walking;
            return PlayerState.Idle;
        }
        else
        {
            return PlayerState.Falling;
        }
    }
} 