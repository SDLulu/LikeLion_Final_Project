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
    
    // 🎮 로컬 변수들
    private float horizontalInput;
    private float verticalInput;
    private Vector2 mouseWorldPosition;
    private bool jumpPressed;
    private bool pickupPressed;
    private bool useItemPressed;
    private bool equipItemPressed;

    
    // 📦 물리/로직 컴포넌트 참조들 (같은 오브젝트에서 찾기)
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private PlayerJump jump;
    
    // 📦 시각적 컴포넌트 참조들 (하위 오브젝트에서 찾기)
    private PlayerAnimation playerAnimation;
    private SpriteRenderer spriteRenderer;
    
    // 📦 Hand 컴포넌트 참조들 (하위 오브젝트에서 찾기)
    private PlayerItemPickup itemPickup;
    private PlayerItemUsage itemUsage;
    
    public override void Spawned()
    {
        // 물리/로직 컴포넌트들 (같은 오브젝트에서 찾기)
        groundCheck = GetComponentInChildren<PlayerGroundCheck>();
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
        
        // 하위 오브젝트들 설정 (Visual, Hand)
        SetupChildObjects();
        
        // 네트워크 물리 설정
        Runner.SetIsSimulated(Object, true);
        
        // 로컬 플레이어 설정
        if (Object.HasInputAuthority)
        {
            // 카메라 등 로컬 전용 설정
        }
        else
        {
            // 원격 플레이어 설정
            Object.RenderSource = RenderSource.Interpolated;
        }
        
        IsAlive = true;
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
        
        if (itemPickup == null)
        {
            Debug.LogWarning($"[{name}] Hand 오브젝트에 PlayerItemPickup 컴포넌트가 없습니다.");
        }
        if (itemUsage == null)
        {
            Debug.LogWarning($"[{name}] Hand 오브젝트에 PlayerItemUsage 컴포넌트가 없습니다.");
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
            
            // 아이템 관련 입력 (Fusion 2 공식 권장: GetKey/GetMouseButton 사용)
            pickupPressed = Input.GetKey(KeyCode.Space) && IsDucking;
            useItemPressed = Input.GetMouseButton(0);
            equipItemPressed = Input.GetMouseButton(1);
        }
    }
    
    // 🌐 네트워크 고정 업데이트
    public override void FixedUpdateNetwork()
    {
        if (!IsAlive) return;
        
        // 입력 데이터 가져오기
        if (Runner.TryGetInputForPlayer<SpelunkyPlayerData>(Object.InputAuthority, out var input))
        {
            // 각 컴포넌트를 일관성 있게 ProcessInput 메서드로 처리
            movement?.ProcessInput(input);
            jump?.ProcessInput(input);
            
            // Hand 컴포넌트들 처리
            ProcessHandInput(input);
        }
    }
    
    // 🎨 렌더링 업데이트 (애니메이션 + 스프라이트 뒤집기)
    public override void Render()
    {
        // 시각적 업데이트들 (저장된 참조 사용)
        playerAnimation?.UpdateAnimations();
        
        // 스프라이트 뒤집기 처리
        if (movement != null && spriteRenderer != null)
        {
            spriteRenderer.flipX = movement.IsFacingLeft;
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
            
            // 버튼 입력 설정 (모든 입력을 일관성 있게 변수로 처리)
            data.NetworkButtons.Set(SpelunkyInputButtons.Jump, jumpPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.PickupItem, pickupPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.UseItem, useItemPressed);
            data.NetworkButtons.Set(SpelunkyInputButtons.EquipItem, equipItemPressed);
        }
        
        return data;
    }
    
    #region 📊 상태 접근 프로퍼티들
    
    // 🏃 이동 관련 프로퍼티들
    public bool IsGrounded => groundCheck?.IsGrounded ?? false;
    public bool IsDucking => movement?.IsDucking ?? false;
    public bool IsFacingLeft => movement?.IsFacingLeft ?? false;
    public float CurrentSpeed => movement?.CurrentSpeed ?? 0f;
    
    // 🦘 점프 관련 프로퍼티들
    public Vector2 Velocity => jump?.Velocity ?? Vector2.zero;
    public bool IsJumping => jump?.IsCurrentlyJumping ?? false;
    public float JumpTime => jump?.CurrentJumpTime ?? 0f;
    
    // 🎒 아이템 관련 프로퍼티들
    public bool HasItem => itemPickup?.HasItem ?? false;
    public string CurrentItemName => itemPickup?.CurrentItemName ?? "없음";
    public int NearbyItemsCount => itemPickup?.NearbyItemsCount ?? 0;
    
    // ⚔️ 무기 관련 프로퍼티들
    public bool HasWeapon => false; // PlayerHandController 제거됨
    
    // 📍 Transform 접근 프로퍼티들
    public Transform VisualRoot => visualRoot;
    public Transform HandRoot => handRoot;
    
    #endregion
    
    #region 🔧 헬퍼 메서드들
    
    // 🤲 Hand 컴포넌트들 입력 처리
    private void ProcessHandInput(SpelunkyPlayerData input)
    {
        // 저장된 참조 사용 (매번 GetComponent 하지 않음)
        itemPickup?.ProcessInput(input);
        itemUsage?.ProcessInput(input);
    }
    
    // 🎒 PlayerItemPickup 컴포넌트 접근
    private PlayerItemPickup GetItemPickup()
    {
        return itemPickup;
    }
    
    // 🎮 PlayerItemUsage 컴포넌트 접근
    private PlayerItemUsage GetItemUsage()
    {
        return itemUsage;
    }
    
    #endregion
} 