using Fusion;
using LMCore;
using UnityEngine;

// 🎮 스펠렁키 플레이어 메인 컨트롤러
// 컴포넌트들을 조합하고 네트워크 동기화 담당
// 
// 📁 권장 구조:
// Player (이 오브젝트) - 물리/로직 컴포넌트들 + PlayerStateManager
// ├── Visual - 시각적 컴포넌트들 (SpriteRenderer, Animator, PlayerAnimation)
// └── Hand - 아이템 시스템 (PlayerItemPickup, PlayerItemUsage, CircleCollider2D)
//
// 🔧 설정 방법:
// 1. Visual 하위 오브젝트 생성 후 SpriteRenderer, Animator, PlayerAnimation 이동
// 2. Hand 하위 오브젝트 생성 후 PlayerItemPickup, PlayerItemUsage 이동
// 3. Hand에 CircleCollider2D 추가 (IsTrigger = true, 아이템 감지용)
// 4. Inspector에서 Visual Root, Hand Root 필드에 각각 할당
// 5. PlayerStateManager는 자동으로 추가됨 (상태 관리 전용)
public class SpelunkyPlayerController : NetworkBehaviour, IBeforeUpdate
{
    [Header("Player State")]
    // 🎮 상태 관리는 PlayerStunInvincibleDie에서 처리됨
    
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
    private bool downJumpPressed;   // Space + IsDucking (밑점프)
    private bool pickupPressed;     // Space + IsDucking  
    private bool pickitem;
    private bool buyitem;
    private bool dropitem;
    
    // 🔨 아이템 관련 입력
    private bool useItemHeld;           // 마우스 좌클릭
    private bool pickupItemPressed;     // 앉기 + Space (아이템 픽업)
    private bool throwItemPressed;      // 우클릭 (아이템 던지기)
    private bool interactPressed;       // F키 (상호작용)
    private bool skillPressed;          // 쉬프트키 (스킬 사용)
    private bool deathPressed;          // K키 (테스트용 죽음 트리거)
    
    // 📦 물리/로직 컴포넌트 참조들 (같은 오브젝트에서 찾기)
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerClimbing climbing;
    
    private PlayerStunInvincibleDie stunInvincibleDie;
    private PlayerDeathHandler playerDeathHandler;
    
    // 📦 시각적 컴포넌트 참조들 (하위 오브젝트에서 찾기)
    private PlayerAnimation playerAnimation;
    private SpriteRenderer spriteRenderer;
    
    // 📦 Hand 컴포넌트 참조들 (하위 오브젝트에서 찾기)
    private PlayerObjectPickup itemPickup;
    private PlayerItemUsage itemUsage;
    
    
    private PlayerObjectThrower itemThrower;
    
    // 🎯 상호작용 컴포넌트 참조
    private PlayerInteraction interaction;
    


    public override void Spawned()
    {
        // 물리/로직 컴포넌트들 (같은 오브젝트에서 찾기)
        groundCheck = GetComponentInChildren<PlayerGroundCheck>();
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
        climbing = GetComponent<PlayerClimbing>();

        stunInvincibleDie = GetComponent<PlayerStunInvincibleDie>();
        playerDeathHandler = GetComponent<PlayerDeathHandler>();
        interaction = GetComponent<PlayerInteraction>();
        
        // 🎮 상태 관리는 PlayerStunInvincibleDie에서 처리됨

        
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
        // Hand 컴포넌트들 참조 설정
        itemPickup = handRoot?.GetComponent<PlayerObjectPickup>();
        itemUsage = handRoot?.GetComponent<PlayerItemUsage>();
        itemThrower = handRoot?.GetComponent<PlayerObjectThrower>();
        
        if (itemPickup == null)
        {
            Debug.LogWarning($"[{name}] Hand 오브젝트에 PlayerObjectPickup 컴포넌트가 없습니다.");
        }
        if (itemUsage == null)
        {
            Debug.LogWarning($"[{name}] Hand 오브젝트에 PlayerItemUsage 컴포넌트가 없습니다.");
        }
        if (itemThrower == null)
        {
            Debug.LogWarning($"[{name}] Hand 오브젝트에 PlayerObjectThrower 컴포넌트가 없습니다.");
        }
    }

    
    private InputBlocker _inputBlocker;
    public InputBlocker InputBlocker => _inputBlocker ??= this.GetOrAddComponent<InputBlocker>();
    
    // 🎮 입력 수집 (매 프레임)
    public void BeforeUpdate()
    {
        // 채팅창이 활성화된 경우 모든 입력 차단
        if (UI_Chating.IsFocusChat)
        {
            ResetInput();
            return;
        }


        // 🎮 상태 확인 - 입력 불가능한 상태면 입력 무시
        if (IsDead || IsStunned || IsHeld || IsThrown)
        {
            // 입력 변수들 초기화
            horizontalInput = 0f;
            verticalInput = 0f;
            mouseScrollWheel = 0f;
            jumpPressed = false;
            downJumpPressed = false;
            pickupItemPressed = false;
            throwItemPressed = false;
            useItemHeld = false;
            interactPressed = false;
            skillPressed = false;
            deathPressed = false;
            return;
        }



        if (Object.HasInputAuthority)
        {
            bool shouldBlockKeyboard = InputBlocker.ShouldBlockKeyboardInput();
            bool shouldBlockMouse = InputBlocker.ShouldBlockMouseInput();
            
            // 키보드 입력 UI와 관계없이 처리
            if (shouldBlockKeyboard == false)
            {
                horizontalInput = Input.GetAxisRaw("Horizontal");
                verticalInput = Input.GetAxisRaw("Vertical");
                
                // 점프 입력 (일관성을 위해 변수로 저장)
                jumpPressed = Input.GetKey(KeyCode.Space) && !(movement?.IsDucking ?? false);
                
                // 밑점프 입력 (앉은 상태에서 스페이스)
                downJumpPressed = Input.GetKey(KeyCode.Space) && (movement?.IsDucking ?? false);
                
                // 키보드 기반 아이템 관련 입력
                pickupItemPressed = Input.GetKey(KeyCode.Space) && (movement?.IsDucking ?? false);
                interactPressed = Input.GetKey(KeyCode.F);
                skillPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                deathPressed = Input.GetKey(KeyCode.K);
                pickitem = Input.GetKey(KeyCode.C);
                buyitem = Input.GetKey(KeyCode.X);
                dropitem = Input.GetKey(KeyCode.V);
            }
            
            // 마우스 입력 - UI 위에 있을 때만 차단
            if (shouldBlockMouse == false)
            {
                // 마우스 월드 위치 계산
                Vector3 mouseScreenPos = Input.mousePosition;
                mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPos);
                
                // 마우스 휠 스크롤 입력
                mouseScrollWheel = Input.GetAxis("Mouse ScrollWheel");
                
                // 마우스 클릭 관련 입력
                throwItemPressed = Input.GetMouseButton(1);
                useItemHeld = Input.GetMouseButton(0);
            }
        }
    }
    
    // 🌐 네트워크 고정 업데이트
    public override void FixedUpdateNetwork()
    {
        // 입력이 필요한 것들 (InputAuthority에서만)
        if (Runner.TryGetInputForPlayer<SpelunkyPlayerInputData>(Object.InputAuthority, out var input))
        {
            // 기존 컴포넌트들 처리 (그대로 유지)
            movement?.ProcessInput(input);
            jump?.ProcessInput(input);
            climbing?.ProcessInput(input);
            itemPickup?.ProcessInput(input);
            itemUsage?.ProcessInput(input);
            itemThrower?.ProcessInput(input);
            interaction?.ProcessInput(input);
        }
        
        // 🎮 상태 관리는 PlayerStunInvincibleDie에서 처리됨
    }

    // 📡 입력 데이터 생성 (LocalInputPoller에서 호출)
    public SpelunkyPlayerInputData GetNetworkInputData()
    {
        SpelunkyPlayerInputData data = new SpelunkyPlayerInputData();
        
        if (Object.HasInputAuthority)
        {
            data.HorizontalInput = horizontalInput;
            data.VerticalInput = verticalInput;
            data.MouseWorldPosition = mouseWorldPosition;
            data.MouseScrollWheel = mouseScrollWheel;
            
            // 🔘 버튼 입력 설정
            data.NetworkButtons.Set(SpelunkyInputButtons.Jump, jumpPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.DownJump, downJumpPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.PickupItem, pickupItemPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.UseItemHold, useItemHeld);
            data.NetworkButtons.Set(SpelunkyInputButtons.ThrowItem, throwItemPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.Interact, interactPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.Skill, skillPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.Death, deathPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.pick, pickitem);
            data.NetworkButtons.Set(SpelunkyInputButtons.buy, buyitem);
            data.NetworkButtons.Set(SpelunkyInputButtons.drop, dropitem);
            
        }
        
        return data;
    }
    
    // 🎮 상태 확인 헬퍼 프로퍼티들
    public bool IsDead => playerDeathHandler?.IsDead ?? false;
    public bool IsStunned => stunInvincibleDie?.IsStunned ?? false;
    public bool IsInvincible => stunInvincibleDie?.IsInvincible ?? false;
    public bool IsHeld => stunInvincibleDie?.IsHeld ?? false;
    public bool IsThrown => stunInvincibleDie?.IsThrown ?? false;
    public bool IsNormal => !(playerDeathHandler?.IsDead ?? false) && !(stunInvincibleDie?.IsStunned ?? false) && !(stunInvincibleDie?.IsHeld ?? false) && !(stunInvincibleDie?.IsThrown ?? false);
    
    // 🎯 들린 상태에서 탈출 처리 (던지기와 동일한 로직)
    private void EscapeFromBeingHeld()
    {
        if (!Object.HasStateAuthority) return;
        
        // 현재 들고 있는 오브젝트 찾기
        var inventory = GetComponentInChildren<PlayerInventory>();
        if (inventory?.CurrentHeldObject == null) return;
        
        var obj = inventory.CurrentHeldObject;
        
        // PlayerObjectThrower의 공통 해제 로직 사용 (힘 없이)
        if (itemThrower != null)
        {
            itemThrower.ReleaseObject(obj, false);
        }
        
        // 점프 힘 적용 (사다리에서 점프하는 것과 동일)
        if (jump != null)
        {
            jump.SetJumpFromClimb();
        }
        
        Debug.Log($"[{name}] 점프로 들린 상태에서 탈출!");
    }

    public void ResetInput()
    {
        horizontalInput = 0f;
        verticalInput = 0f;
        mouseScrollWheel = 0f;
        jumpPressed = false;
        downJumpPressed = false;
        pickupItemPressed = false;
        throwItemPressed = false;
        useItemHeld = false;
        interactPressed = false;
        skillPressed = false;
        deathPressed = false;
    }

} 