using Fusion;
using UnityEngine;

// 드릴 - 연속 회전 공격 무기
public class Drill : NetworkBehaviour, IItemInteraction
{
    [Header("Drill Settings")]
    [SerializeField] private float rotationSpeed = 3600f; // 회전 속도 (도/초)
    [SerializeField] private float attackInterval = 0.1f; // 연속 공격 간격 (초)
    
    [Header("References")]
    [SerializeField] private AttackCollisionHandler attackHandler; // 공용 핸들러 사용
    [SerializeField] private Collider2D attackCollider;            // 드릴 공격 콜라이더
    [SerializeField] private Transform drillSprite; // 드릴 스프라이트 Transform (별도 오브젝트)
    
    [Networked] private TickTimer AttackTimer { get; set; }
    [Networked] private NetworkBool IsHeld { get; set; }
    [Networked] private NetworkBool IsDrilling { get; set; }
    
    bool IItemInteraction.IsHeld => IsHeld;
    
    private Quaternion originalSpriteRotation;
    private float currentRotation = 0f; // 현재 회전 각도 (로컬 기준)
    
    private bool canAttack { get { return AttackTimer.ExpiredOrNotRunning(Runner); } }
    
    private void Awake()
    {
        // 스프라이트 참조가 없으면 자동으로 찾기
        if (drillSprite == null)
        {
            drillSprite = GetComponentInChildren<Transform>();
        }
        
        if (drillSprite != null)
        {
            originalSpriteRotation = drillSprite.localRotation;
            currentRotation = 0f; // 로컬 기준 초기 회전값
        }
    }
    
    public override void Spawned()
    {
        if (drillSprite != null)
        {
            originalSpriteRotation = drillSprite.localRotation;
            currentRotation = 0f; // 로컬 기준 초기 회전값
        }
        
        if (!HasInputAuthority)
        {
            Runner.SetIsSimulated(Object, true);
            base.Object.RenderSource = RenderSource.Interpolated;
            base.Object.ForceRemoteRenderTimeframe = true;
        }

        // 공용 핸들러에 콜라이더 주입 및 초기 비활성화
        if (attackHandler != null && attackCollider != null)
        {
            attackHandler.AttackCollider = attackCollider;
            attackCollider.enabled = false;
        }
    }
    
    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        StartDrilling();
    }
    
    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 마우스를 누르고 있는 동안 계속 드릴링
        if (!IsDrilling)
        {
            StartDrilling();
        }
    }
    
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        StopDrilling();
    }
    
    private void StartDrilling()
    {
        if (IsDrilling) return;
        
        AttackTimer = TickTimer.CreateFromSeconds(Runner, attackInterval);
        IsDrilling = true;
        
        // 콜라이더 활성화
        if (attackCollider != null)
            attackCollider.enabled = true;
    }
    
    private void StopDrilling()
    {
        IsDrilling = false;
        
        // 콜라이더 비활성화
        if (attackCollider != null)
            attackCollider.enabled = false;
        
        // 스프라이트 회전 초기화
        if (drillSprite != null)
        {
            drillSprite.localRotation = originalSpriteRotation;
            currentRotation = 0f;
        }
    }
    
    public override void FixedUpdateNetwork()
    {
        if (IsDrilling)
        {
            // 드릴 스프라이트 회전 처리 (로컬 좌표계 기준)
            if (drillSprite != null)
            {
                float deltaTime = Runner.DeltaTime;
                float rotationAmount = rotationSpeed * deltaTime;
                
                // 로컬 좌표계 기준으로 회전 (곡괭이와 같은 방식)
                currentRotation += rotationAmount;
                drillSprite.localRotation = originalSpriteRotation * Quaternion.Euler(currentRotation, 0, 0);
            }
            
            // 연속 공격 처리 - 콜라이더 껐다 켜기
            if (canAttack)
            {
                // 콜라이더를 껐다 켜서 새로운 트리거 체크 발생
                if (attackCollider != null)
                {
                    attackCollider.enabled = false;
                    attackCollider.enabled = true;
                }
                
                AttackTimer = TickTimer.CreateFromSeconds(Runner, attackInterval);
            }
        }
        else
        {
            // 콜라이더 비활성화
            if (attackCollider != null)
                attackCollider.enabled = false;
            
            // 스프라이트 회전 초기화
            if (drillSprite != null)
            {
                drillSprite.localRotation = originalSpriteRotation;
                currentRotation = 0f;
            }
        }
    }
    
    public void OnPickedUp()
    {
        if (!HasStateAuthority) return;
        IsHeld = true;
    }
    
    public void OnReleased()
    {
        if (!HasStateAuthority) return;
        IsHeld = false;
        StopDrilling();
    }
    
    public void ApplyKnockback(Vector2 force, float duration = 0f)
    {
        if (!HasStateAuthority) return;
        
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }
} 