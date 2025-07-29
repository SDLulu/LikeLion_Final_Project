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
    [Networked] public bool IsAlive { get; private set; } = true;
    
    [Header("Visual References")]
    [SerializeField] private Transform visualRoot; // Visual 하위 오브젝트 참조
    
    [Header("Hand References")]
    [SerializeField] private Transform handRoot; // Hand 하위 오브젝트 참조
    
    // 🎮 입력 변수들 (Fusion 2 공식 방식 - NetworkButtons로 통합)
    private float horizontalInput;
    private float verticalInput;
    private Vector2 mouseWorldPosition;
    private bool jumpPressed;       // Space + !IsDucking
    private bool pickupPressed;     // Space + IsDucking  
    private bool pickitem;
    private bool buyitem;
    private bool dropitem;
    
    // 🔨 아이템 사용 입력
    private bool useItemHeld;           // 현재 클릭 유지 중
    private bool throwItemPressed;      // 우클릭 (MouseButton 1)
    private bool skillPressed;          // 쉬프트키 (스킬 사용)

    
    // 📦 물리/로직 컴포넌트 참조들 (같은 오브젝트에서 찾기)
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerClimbing climbing;
    private PlayerInventory inventory;

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
        inventory = GetComponent<PlayerInventory>();

        // 하위 오브젝트들 설정 (Visual, Hand)
        SetupChildObjects();
        
        // 네트워크 설정 분리
        SpelunkyNetworkInitializer.InitializeNetworkSettings(this);
        SpelunkyNetworkInitializer.InitializePlayerType(this);
        
        IsAlive = true;
        
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
            
            // Visual 오브젝트가 없으면 생성
            if (visualRoot == null)
            {
                GameObject visualObj = new GameObject("Visual");
                visualObj.transform.SetParent(transform);
                visualObj.transform.localPosition = Vector3.zero;
                visualRoot = visualObj.transform;
                
                Debug.LogWarning($"[{name}] Visual 하위 오브젝트가 없어서 자동 생성했습니다. " +
                               "SpriteRenderer와 Animator를 Visual 오브젝트로 이동해주세요.");
            }
        }
        
        // Visual 컴포넌트들 참조 설정
        playerAnimation = visualRoot.GetComponent<PlayerAnimation>();
        spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
        
        if (playerAnimation == null)
        {
            Debug.LogWarning($"[{name}] Visual 오브젝트에 PlayerAnimation 컴포넌트가 없습니다.");
        }
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[{name}] Visual 오브젝트에 SpriteRenderer 컴포넌트가 없습니다.");
        }
    }
    
    // 🤲 Hand 오브젝트 설정
    private void SetupHandObject()
    {
        // handRoot가 설정되지 않은 경우 자동으로 찾기
        if (handRoot == null)
        {
            handRoot = transform.Find("Hand");
            
            // Hand 오브젝트가 없으면 생성
            if (handRoot == null)
            {
                GameObject handObj = new GameObject("Hand");
                handObj.transform.SetParent(transform);
                handObj.transform.localPosition = Vector3.zero;
                handRoot = handObj.transform;
                
                Debug.LogWarning($"[{name}] Hand 하위 오브젝트가 없어서 자동 생성했습니다. " +
                               "PlayerItemPickup과 PlayerItemUsage를 Hand 오브젝트로 이동해주세요.");
            }
        }
        
        // Hand 컴포넌트들 참조 설정
        itemPickup = handRoot.GetComponent<PlayerItemPickup>();
        itemUsage = handRoot.GetComponent<PlayerItemUsage>();
        itemThrower = handRoot.GetComponent<PlayerItemThrower>();
        
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
        if (Object.HasInputAuthority && IsAlive)
        {
            // 방향키 입력
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
            
            // 마우스 월드 위치 계산
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            
            // 점프 입력 (일관성을 위해 변수로 저장)
            jumpPressed = Input.GetKey(KeyCode.Space) && !IsDucking;
            
            // 🔘 아이템 관련 입력
            pickupPressed = Input.GetKey(KeyCode.Space) && IsDucking;
            useItemHeld = Input.GetMouseButton(0);       // 마우스 좌클릭
            throwItemPressed = Input.GetMouseButton(1);  // 마우스 우클릭
            skillPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);  // 쉬프트키
            pickitem = Input.GetKey(KeyCode.C);
            buyitem = Input.GetKey(KeyCode.X);
            dropitem = Input.GetKey(KeyCode.V);
        }
    }
    
    // 🌐 네트워크 고정 업데이트
    public override void FixedUpdateNetwork()
    {
        if (!IsAlive) return;
        
        // 입력이 필요한 것들 (InputAuthority에서만)
        if (Runner.TryGetInputForPlayer<SpelunkyPlayerData>(Object.InputAuthority, out var input))
        {
            movement?.ProcessInput(input);
            jump?.ProcessInput(input);
            climbing?.ProcessInput(input);
            itemPickup?.ProcessInput(input);
            itemUsage?.ProcessInput(input);
            itemThrower?.ProcessInput(input);
            inventory?.ProcessInput(input);

        }
        
    }

    // 📡 입력 데이터 생성 (LocalInputPoller에서 호출)
    public SpelunkyPlayerData GetNetworkInputData()
    {
        SpelunkyPlayerData data = new SpelunkyPlayerData();
        
        if (IsAlive)
        {
            data.HorizontalInput = horizontalInput;
            data.VerticalInput = verticalInput;
            data.MouseWorldPosition = mouseWorldPosition;
            
            // 🔘 버튼 입력 설정
            data.NetworkButtons.Set(SpelunkyInputButtons.Jump, jumpPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.PickupItem, pickupPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.UseItemHold, useItemHeld);
            data.NetworkButtons.Set(SpelunkyInputButtons.ThrowItem, throwItemPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.Skill, skillPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.pick, pickitem);
            data.NetworkButtons.Set(SpelunkyInputButtons.buy, buyitem);
            data.NetworkButtons.Set(SpelunkyInputButtons.drop, dropitem);
            
        }
        
        return data;
    }
    
    // 필수 프로퍼티 (BeforeUpdate에서 사용)
    private bool IsDucking { get { return movement?.IsDucking ?? false; } }
} 