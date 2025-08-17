using Fusion;
using UnityEngine;

public class BasicPunchItem : NetworkBehaviour, IItemInteraction
{
    [Header("Punch Settings")]
    [SerializeField] private float punchDistance = 1.2f;
    [SerializeField] private float punchDuration = 0.15f;

    [Header("Attack Collision Handler")]
    [SerializeField] private AttackCollisionHandler AttackCollisionHandler;

    private SpriteRenderer spriteRenderer;
    private Collider2D attackCollider;
    private Vector3 originalPosition;

    [Networked] private TickTimer PunchTimer { get; set; }
    [Networked] private Vector3 PunchDirection { get; set; }
    [Networked] private NetworkBool IsHeld { get; set; }

    private bool isPunchActive { get { return PunchTimer.IsRunning; } }
    
    public bool IsPunchActive => isPunchActive;
    bool IItemInteraction.IsHeld => true;  // 기본 펀치는 항상 들려있음

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (AttackCollisionHandler != null)
        {
            attackCollider = AttackCollisionHandler.AttackCollider;
        }
        
        originalPosition = transform.localPosition;
        
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (attackCollider != null) attackCollider.enabled = false;
    }

    public override void Spawned()
    {
        if(!HasInputAuthority)
        {
            Runner.SetIsSimulated(Object, true);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (PunchTimer.IsRunning)
        {
            if (PunchTimer.Expired(Runner))
            {
                PunchTimer = TickTimer.None;
                
                if (attackCollider != null)
                {
                    attackCollider.enabled = false;
                }
            }
            else
            {
                float remaining = PunchTimer.RemainingTime(Runner) ?? 0f;
                float progress = 1f - (remaining / punchDuration);
                Vector3 punchOffset = PunchDirection * (punchDistance * progress);
                transform.localPosition = originalPosition + punchOffset;

                if (attackCollider != null) 
                {
                    attackCollider.enabled = true;
                }
            }
        }
        else
        {
            transform.localPosition = originalPosition;
            if (attackCollider != null) attackCollider.enabled = false;
        }
    }

    public override void Render()
    {
        if (isPunchActive)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = true;
        }
        else
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
        }
    }

    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        if (isPunchActive) return;

        Vector2 worldDir = ((Vector2)mouseWorldPosition - (Vector2)transform.position).normalized;
        Vector2 localDir = (Vector2)transform.parent.InverseTransformDirection(worldDir);
        PunchDirection = localDir;

        PunchTimer = TickTimer.CreateFromSeconds(Runner, punchDuration);
        
        if (attackCollider != null)
        {
            attackCollider.enabled = true;
        }
        
        // 펀치 소리 재생
        RPC_PlayPunchSound();
    }

    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

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
    
    // --- RPC 메서드들 ---
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PlayPunchSound()
    {
        // 펀치 소리 재생
        AudioManager.Inst.PlaySound("펀치", transform.position);
    }
} 