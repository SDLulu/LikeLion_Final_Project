using Fusion;
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
    private PlayerStunInvincibleDie stunInvincibleDie;
    
    // 📦 시각적 컴포넌트 참조들 (하위 오브젝트에서 찾기)
    private PlayerAnimation playerAnimation;
    private SpriteRenderer spriteRenderer;
    
    // 📦 Hand 컴포넌트 참조들 (하위 오브젝트에서 찾기)
    private PlayerObjectPickup itemPickup;
    private PlayerItemUsage itemUsage;
    private PlayerObjectThrower itemThrower;

    public override void Spawned()
    {
        // 물리/로직 컴포넌트들 (같은 오브젝트에서 찾기)
        groundCheck = GetComponentInChildren<PlayerGroundCheck>();
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
        climbing = GetComponent<PlayerClimbing>();
        stunInvincibleDie = GetComponent<PlayerStunInvincibleDie>();
        
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
    
    // 🎮 입력 수집 (매 프레임)
    public void BeforeUpdate()
    {
        // Dead, Stunned, Held, Thrown 상태면 모든 입력 무시 + 입력값 초기화
        if (stunInvincibleDie != null && (stunInvincibleDie.IsDead ||
         stunInvincibleDie.IsStunned ||
         stunInvincibleDie.IsHeld ||
         stunInvincibleDie.IsThrown))
        {
            // Held 상태에서는 점프 입력만 허용 (탈출용)
            if (stunInvincibleDie.IsHeld && Object.HasInputAuthority)
            {
                bool jumpPressed = Input.GetKey(KeyCode.Space);
                if (jumpPressed)
                {
                    // 점프로 탈출 - 던지기와 동일한 처리
                    EscapeFromBeingHeld();
                    
                    return;
                }
            }
            
            horizontalInput = 0f;
            verticalInput = 0f;
            mouseScrollWheel = 0f;
            jumpPressed = false;
            pickupPressed = false;
            useItemHeld = false;
            throwItemPressed = false;
            skillPressed = false;
            return;
        }

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
        if (Runner.TryGetInputForPlayer<SpelunkyPlayerInputData>(Object.InputAuthority, out var input))
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
            data.NetworkButtons.Set(SpelunkyInputButtons.PickupItem, pickupPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.UseItemHold, useItemHeld);
            data.NetworkButtons.Set(SpelunkyInputButtons.ThrowItem, throwItemPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.Skill, skillPressed);
        }
        
        return data;
    }
    
    // 🎮 상태 확인 헬퍼 프로퍼티들
    public bool IsDead => stunInvincibleDie?.IsDead ?? false;
    public bool IsStunned => stunInvincibleDie?.IsStunned ?? false;
    public bool IsInvincible => stunInvincibleDie?.IsInvincible ?? false;
    public bool IsHeld => stunInvincibleDie?.IsHeld ?? false;
    public bool IsThrown => stunInvincibleDie?.IsThrown ?? false;
    public bool IsNormal => !(stunInvincibleDie?.IsDead ?? false) && !(stunInvincibleDie?.IsStunned ?? false) && !(stunInvincibleDie?.IsHeld ?? false) && !(stunInvincibleDie?.IsThrown ?? false);
    
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
} 