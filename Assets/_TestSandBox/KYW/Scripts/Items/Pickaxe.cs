using Fusion;
using UnityEngine;

// 네트워크 동기화 기반 곡괭이
public class Pickaxe : NetworkBehaviour, IItemInteraction
{
    [Header("Pickaxe Settings")]
    [SerializeField] private float swingAngle = 90f;
    [SerializeField] private float swingDuration = 0.18f;
    
    [Header("Attack Collision Handler")]
    [SerializeField] private AttackCollisionHandler attackCollisionHandler;
    

    [Networked] private TickTimer PickaxeTimer { get; set; }

    [Networked] private NetworkBool IsHeld { get; set; }

    private Quaternion originalRotation;
    private SpriteRenderer spriteRenderer;
    private Collider2D attackCollider;

    private bool isPickaxeActive { get { return PickaxeTimer.IsRunning; } }
    
    bool IItemInteraction.IsHeld => IsHeld;  // 인터페이스 구현 (명시적 구현)

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (attackCollisionHandler != null)
        {
            attackCollider = attackCollisionHandler.AttackCollider;
        }
        
        originalRotation = transform.localRotation;
        
        if (attackCollider != null) attackCollider.enabled = false;
    }

    public override void Spawned()
    {
        originalRotation = transform.localRotation;
        if (!HasInputAuthority)
        {
            Runner.SetIsSimulated(Object, true);
            base.Object.RenderSource = RenderSource.Interpolated;
            base.Object.ForceRemoteRenderTimeframe = true;
        }
    }

    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (isPickaxeActive) return;

        PickaxeTimer = TickTimer.CreateFromSeconds(Runner, swingDuration);
        
        if (attackCollider != null)
        {
            attackCollider.enabled = true;
        }
    }

    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    public override void FixedUpdateNetwork()
    {
        if (PickaxeTimer.IsRunning)
        {
            if (PickaxeTimer.Expired(Runner))
            {
                PickaxeTimer = TickTimer.None;
                
                if (attackCollider != null)
                {
                    attackCollider.enabled = false;
                }
                
                if (transform.parent != null)
                    transform.localRotation = originalRotation;
            }
            else
            {
                float remaining = PickaxeTimer.RemainingTime(Runner) ?? 0f;
                float progress = 1f - (remaining / swingDuration);
                
                // flipY가 true면 왼쪽, false면 오른쪽
                int facing = (spriteRenderer != null && spriteRenderer.flipY) ? -1 : 1;
                float angle;
                if (progress < 0.5f)
                {
                    angle = Mathf.Lerp(0f, -swingAngle * facing, progress * 2f);
                    if (attackCollider != null) attackCollider.enabled = false;
                }
                else
                {
                    angle = Mathf.Lerp(-swingAngle * facing, swingAngle * facing, (progress - 0.5f) * 2f);
                    if (attackCollider != null) attackCollider.enabled = true;
                }
                if (transform.parent != null)
                    transform.localRotation = originalRotation * Quaternion.Euler(0, 0, angle);
            }
        }
        else
        {
            if (attackCollider != null) attackCollider.enabled = false;
            if (transform.parent != null)
                transform.localRotation = originalRotation;
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