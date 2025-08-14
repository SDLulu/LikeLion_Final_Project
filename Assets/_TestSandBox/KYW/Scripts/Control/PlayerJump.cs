using Fusion;
using UnityEngine;
using System.Collections.Generic;

// 🦘 플레이어 점프 컴포넌트
// 점프, 중력, 속도 제한 담당
public class PlayerJump : NetworkBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpSpeed = 10f;         // 점프 상승 속도 (일정)
    [SerializeField] private float maxJumpTime = 0.3f;      // 최대 점프 지속 시간
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float maxFallSpeed = 50f;
    public float VelocityY { get; private set; }
    
    [Header("Passive Items - Jump")]
    [SerializeField] private float jumpShoesMultiplier = 1.2f; // 점프신발 배율
    [SerializeField] private int wingsJumpCount = 2; // 날개 점프 횟수
    
    [Header("Passive Items - Rocket")]
    [SerializeField] private float rocketThrust = 25f; // 로켓 추진력 (중력보다 강하게)
    [SerializeField] private float fuelConsumptionRate = 20f; // 연료 소모율 (초당)
    [SerializeField] private float maxRocketFuel = 100f; // 최대 연료량
    
    // 🦘 점프 상태 추적
    [Networked] public bool IsJumping { get; private set; }
    [Networked] public float JumpTime { get; private set; }
    [Networked] public int CurrentJumpCount { get; private set; } // 현재 점프 횟수
    [Networked] public float RocketFuel { get; private set; } // 로켓 연료량
    [Networked] public bool IsRocketThrusting { get; private set; } // 로켓 추진 중인지
    
    // 🦘 밑점프 상태 추적 (로컬에서만)
    private bool isDownJumping = false;
    private float downJumpTimer = 0f;
    [SerializeField] private float downJumpIgnoreTime = 0.5f;  // 플랫폼 무시 시간
    
    // 🦘 비활성화된 플랫폼 컴포넌트들 저장
    private List<BoxCollider2D> disabledBoxColliders = new List<BoxCollider2D>();
    
    // Fusion 2 공식 패턴: 이전 버튼 상태 추적 (GetPressed 사용)
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }
    
    // 참조 컴포넌트들
    private PlayerGroundCheck groundCheck;
    private PlayerMovement movement;
    private Rigidbody2D rb;
    private SpelunkyPlayerController playerController;
    private PlayerInventory playerInventory;
    
    public override void Spawned()
    {
        // 모든 컴포넌트 참조를 한 번에 설정
        rb = GetComponent<Rigidbody2D>();
        groundCheck = GetComponentInChildren<PlayerGroundCheck>();
        movement = GetComponent<PlayerMovement>();
        playerController = GetComponent<SpelunkyPlayerController>();
        playerInventory = GetComponentInChildren<PlayerInventory>();
        
        // 필수 컴포넌트 검증
        if (rb == null)
            Debug.LogError($"[{name}] Rigidbody2D 컴포넌트를 찾을 수 없습니다!");
        if (groundCheck == null)
            Debug.LogError($"[{name}] PlayerGroundCheck 컴포넌트를 찾을 수 없습니다!");
        if (movement == null)
            Debug.LogError($"[{name}] PlayerMovement 컴포넌트를 찾을 수 없습니다!");
        if (playerController == null)
            Debug.LogError($"[{name}] SpelunkyPlayerController 컴포넌트를 찾을 수 없습니다!");
        if (playerInventory == null)
            Debug.LogError($"[{name}] PlayerInventory 컴포넌트를 찾을 수 없습니다!");
        
        // 로켓 연료 초기화
        RocketFuel = maxRocketFuel;
    }
    
    // 점프 관련 모든 처리를 통합한 메서드
    public void ProcessInput(SpelunkyPlayerInputData input)
    {
        // 상태 확인 - 점프 불가능한 상태면 처리하지 않음
        if (playerController.IsDead || playerController.IsStunned || playerController.IsHeld || playerController.IsThrown)
        {
            return;
        }
        
        HandleJump(input);
        HandleRocketThrust(input); // 로켓 추진 처리
        UpdateDownJump();  // 밑점프 타이머 처리
        ApplyGravity();
        ClampVelocity();
        
        // 비주얼 효과 업데이트는 PlayerPassiveItemVisual에서 처리
        
        // 수직 속도 업데이트
        VelocityY = rb.linearVelocity.y;
    }

    // 사다리에서 강제 점프 진입용 (Climbing에서 호출)
    public void SetJumpFromClimb()
    {
        if (!IsJumping)
        {
            IsJumping = true;
            JumpTime = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
            Debug.Log("🦘 사다리에서 점프!");
        }
    }
    
    private void HandleJump(SpelunkyPlayerInputData input)
    {
        // Fusion 2 공식 패턴: GetPressed로 점프 버튼 눌림 감지
        var pressed = input.NetworkButtons.GetPressed(ButtonsPrevious);
        bool jumpHeld = input.NetworkButtons.IsSet(SpelunkyInputButtons.Jump);
        bool downJumpPressed = input.NetworkButtons.IsSet(SpelunkyInputButtons.DownJump);
        
        // 이전 상태 업데이트 (공식 패턴)
        ButtonsPrevious = input.NetworkButtons;
        
        // 🎮 밑점프 시작 (플랫폼 위에서 앉은 상태일 때)
        if (pressed.IsSet(SpelunkyInputButtons.DownJump) && groundCheck.IsGrounded && 
            movement != null && movement.IsDucking && !movement.IsLookingUp && !isDownJumping)
        {
            // 기존 점프 상태 초기화
            if (IsJumping)
            {
                IsJumping = false;
                JumpTime = 0f;
            }
            
            StartDownJump();
            return; // 밑점프 실행 시 일반 점프 건너뛰기
        }
        
        // 🚫 밑점프 입력이 있으면 일반 점프 로직 수행하지 않음
        if (downJumpPressed)
        {
            return;
        }
        
        // 🎮 일반 점프 시작 (땅에 있을 때만, 한 번만 감지)
        if (pressed.IsSet(SpelunkyInputButtons.Jump) && groundCheck.IsGrounded)
        {
            IsJumping = true;
            JumpTime = 0f;
            CurrentJumpCount = 1; // 첫 번째 점프
        }
        
        // 🎮 공중 점프 (날개가 있을 때)
        if (pressed.IsSet(SpelunkyInputButtons.Jump) && !groundCheck.IsGrounded && 
            playerInventory.hasWings && CurrentJumpCount < GetMaxJumpCount())
        {
            IsJumping = true;
            JumpTime = 0f;
            CurrentJumpCount++;
            Debug.Log($"[{name}] 공중 점프! ({CurrentJumpCount}/{GetMaxJumpCount()})");
            
            // 날개 펄럭임 애니메이션은 PlayerPassiveItemVisual에서 처리
        }
        
        // 일반 점프 중일 때 처리
        if (IsJumping)
        {
            // 점프 시간 업데이트
            JumpTime += Runner.DeltaTime;
            
            // 점프키를 누르고 있고, 최대 시간을 넘지 않았으면 일정한 속도로 상승
            if (jumpHeld && JumpTime < maxJumpTime)
            {
                // 패시브 아이템 효과가 적용된 점프력으로 상승
                float currentJumpForce = GetJumpForce();
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);
            }
            else
            {
                // 점프키를 떼거나 최대 시간 도달시 점프 종료
                IsJumping = false;
            }
        }
        
        // 땅에 닿으면 점프 상태 초기화 및 연료 충전
        if (groundCheck.IsGrounded && rb.linearVelocity.y <= 0)
        {
            IsJumping = false;
            JumpTime = 0f;
            CurrentJumpCount = 0; // 점프 횟수 초기화
            
            // 로켓 연료 충전
            if (playerInventory.hasRocket && RocketFuel < maxRocketFuel)
            {
                RocketFuel = maxRocketFuel;
                Debug.Log($"[{name}] 로켓 연료 충전됨: {RocketFuel}");
            }
        }
    }
    
    private void ApplyGravity()
    {
        if (!groundCheck.IsGrounded)
        {
            rb.linearVelocity += Vector2.down * gravity * Runner.DeltaTime;
        }
    }
    
    private void ClampVelocity()
    {
        // 최대 낙하 속도 제한
        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }
    
    // 🚀 로켓 추진 처리
    private void HandleRocketThrust(SpelunkyPlayerInputData input)
    {
        // 로켓이 없거나 연료가 없으면 처리하지 않음
        if (!playerInventory.hasRocket || RocketFuel <= 0)
        {
            IsRocketThrusting = false;
            return;
        }
        
        // 점프 중이 아니고 점프키를 누르고 있고, 땅에 있지 않으면 로켓 추진
        bool jumpHeld = input.NetworkButtons.IsSet(SpelunkyInputButtons.Jump);
        if (!IsJumping && !groundCheck.IsGrounded && jumpHeld)
        {
            // 고정 추진력 적용 (linearVelocity로 직접 속도 설정)
            float currentVelocityY = rb.linearVelocity.y;
            float newVelocityY = Mathf.Max(currentVelocityY, rocketThrust);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, newVelocityY);
            
            // 연료 소모
            RocketFuel -= fuelConsumptionRate * Runner.DeltaTime;
            RocketFuel = Mathf.Max(0, RocketFuel);
            
            // 로켓 추진 상태 설정
            IsRocketThrusting = true;
            
            // 연료 소진 시 로그
            if (RocketFuel <= 0)
            {
                Debug.Log($"[{name}] 로켓 연료 소진!");
                IsRocketThrusting = false;
            }
        }
        else
        {
            // 로켓 추진 중이 아니면 상태 해제
            IsRocketThrusting = false;
        }
    }
    
    // 🦘 최대 점프 횟수 계산
    private int GetMaxJumpCount()
    {
        if (playerInventory.hasWings)
            return wingsJumpCount;
        return 1; // 기본 점프 횟수
    }
    
    // 🦘 점프력 계산
    private float GetJumpForce()
    {
        float jumpForce = jumpSpeed;
        if (playerInventory.hasJumpShoes)
            jumpForce *= jumpShoesMultiplier;
        return jumpForce;
    }
    
    // 🦘 밑점프 시작
    private void StartDownJump()
    {
        isDownJumping = true;
        downJumpTimer = downJumpIgnoreTime;
        
        // 이 플레이어만 플랫폼과 충돌 무시
        IgnorePlatformCollisions();
        
        // 다른 클라이언트들에게도 밑점프 상태 알림
        if (Object.HasInputAuthority)
        {
            RPC_StartDownJump();
        }
    }
    
    // 🌐 밑점프 시작 RPC (다른 클라이언트들에게 알림)
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_StartDownJump()
    {
        if (!Object.HasInputAuthority)
        {
            isDownJumping = true;
            downJumpTimer = downJumpIgnoreTime;
            IgnorePlatformCollisions();
        }
    }
    
    // 🦘 이 플레이어만 플랫폼과 충돌 무시
    private void IgnorePlatformCollisions()
    {
        // 이 플레이어의 Collider2D들 가져오기
        Collider2D[] playerColliders = GetComponents<Collider2D>();
        int platformLayer = LayerMask.NameToLayer("Platform");
        
        // PlayerGroundCheck의 실제 위치와 감지 영역을 사용
        Vector3 groundCheckPosition = groundCheck.transform.position;
        Vector2 groundCheckSize = groundCheck.GroundCheckSize;
        LayerMask groundLayer = groundCheck.GroundLayer;
        
        // 밑점프할 때는 감지 영역을 더 넓게 설정 (가로로 5배, 세로로 2배)
        Vector2 downJumpCheckSize = new Vector2(groundCheckSize.x * 5f, groundCheckSize.y * 2f);
        
        // PlayerGroundCheck의 실제 위치에서 감지 (밑점프용 넓은 영역)
        Collider2D[] groundedColliders = Physics2D.OverlapBoxAll(
            groundCheckPosition, 
            downJumpCheckSize,  // 밑점프용 넓은 영역 사용
            0f, 
            groundLayer
        );
        
        foreach (var platformCollider in groundedColliders)
        {
            // Platform 레이어인지 확인
            if (platformCollider.gameObject.layer == platformLayer)
            {
                // 이미 무시된 플랫폼인지 확인 (중복 처리 방지)
                var boxCollider = platformCollider.GetComponent<BoxCollider2D>();
                if (boxCollider != null && !disabledBoxColliders.Contains(boxCollider))
                {
                    // 이 플레이어의 모든 Collider2D와 이 플랫폼 간의 충돌 무시
                    foreach (var playerCollider in playerColliders)
                    {
                        Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
                    }
                    
                    // 나중에 복구하기 위해 저장
                    disabledBoxColliders.Add(boxCollider);
                }
            }
        }
    }
    
    // 🦘 밑점프 타이머 처리
    private void UpdateDownJump()
    {
        if (isDownJumping)
        {
            downJumpTimer -= Runner.DeltaTime;
            if (downJumpTimer <= 0f)
            {
                EndDownJump();
            }
        }
    }
    
    // 🦘 밑점프 종료 (플랫폼 충돌 복구)
    private void EndDownJump()
    {
        isDownJumping = false;
        downJumpTimer = 0f;
        
        // 이 플레이어의 플랫폼 충돌 무시 복구
        RestorePlatformCollisions();
        
        // 다른 클라이언트들에게도 밑점프 종료 알림
        if (Object.HasInputAuthority)
        {
            RPC_EndDownJump();
        }
    }
    
    // 🌐 밑점프 종료 RPC (다른 클라이언트들에게 알림)
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_EndDownJump()
    {
        if (!Object.HasInputAuthority)
        {
            isDownJumping = false;
            downJumpTimer = 0f;
            RestorePlatformCollisions();
        }
    }
    
    // 🦘 이 플레이어의 플랫폼 충돌 무시 복구
    private void RestorePlatformCollisions()
    {
        // 이 플레이어의 Collider2D들 가져오기
        Collider2D[] playerColliders = GetComponents<Collider2D>();
        
        // 저장된 플랫폼 컴포넌트들을 통해 충돌 복구
        for (int i = 0; i < disabledBoxColliders.Count; i++)
        {
            var boxCollider = disabledBoxColliders[i];
            if (boxCollider != null)
            {
                // 이 플레이어의 모든 Collider2D와 이 플랫폼 간의 충돌 복구
                foreach (var playerCollider in playerColliders)
                {
                    Physics2D.IgnoreCollision(playerCollider, boxCollider, false);
                }
            }
        }
        
        // 저장된 컴포넌트들 초기화
        disabledBoxColliders.Clear();
    }
    
    // 디버그 GUI는 PlayerJumpDebugGUI 컴포넌트에서 처리
} 