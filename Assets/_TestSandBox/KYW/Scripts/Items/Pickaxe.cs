using Fusion;
using UnityEngine;

// 네트워크 동기화 기반 곡괭이
public class Pickaxe : NetworkBehaviour, IUsableItem
{
    [Header("Pickaxe Settings")]
    [SerializeField] private float swingAngle = 90f;
    [SerializeField] private float swingDuration = 0.18f;
    [SerializeField] private Collider2D bladeCollider;
    [SerializeField] private LayerMask tileLayer;

    [Networked] private float PickaxeTimer { get; set; }
    [Networked] private float PickaxeAngle { get; set; }
    [Networked] private NetworkBool IsSwinging { get; set; }
    [Networked] private NetworkBool HasHitTile { get; set; }

    private Quaternion originalRotation;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (bladeCollider != null) bladeCollider.enabled = false;
        originalRotation = transform.localRotation;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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
        if (!HasStateAuthority || IsSwinging) return;
        IsSwinging = true;
        PickaxeTimer = swingDuration;
        HasHitTile = false;
    }

    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsSwinging || !HasStateAuthority || HasHitTile) return;
        if (((1 << other.gameObject.layer) & tileLayer.value) != 0)
        {
            var tileLogic = other.GetComponent<PMK_TileRogic>();
            if (tileLogic != null)
            {
                // tileLogic.DestoryTile(other.transform.position);
                HasHitTile = true;
                if (bladeCollider != null) bladeCollider.enabled = false;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (IsSwinging)
        {
            PickaxeTimer -= Runner.DeltaTime;
            float t = Mathf.Clamp01(1f - (PickaxeTimer / swingDuration));
            // flipY가 true면 왼쪽, false면 오른쪽
            int facing = (spriteRenderer != null && spriteRenderer.flipY) ? -1 : 1;
            float angle;
            if (t < 0.5f)
            {
                angle = Mathf.Lerp(0f, -swingAngle * facing, t * 2f);
                if (bladeCollider != null) bladeCollider.enabled = false;
            }
            else
            {
                angle = Mathf.Lerp(-swingAngle * facing, swingAngle * facing, (t - 0.5f) * 2f);
                if (bladeCollider != null) bladeCollider.enabled = true;
            }
            PickaxeAngle = angle;
            if (transform.parent != null)
                transform.localRotation = originalRotation * Quaternion.Euler(0, 0, angle);
            // 부모가 없으면(local) localRotation을 건드리지 않음

            if (PickaxeTimer <= 0f)
            {
                IsSwinging = false;
                HasHitTile = false;
                if (bladeCollider != null) bladeCollider.enabled = false;
                if (transform.parent != null)
                    transform.localRotation = originalRotation;
            }
        }
        else
        {
            if (bladeCollider != null) bladeCollider.enabled = false;
            if (transform.parent != null)
                transform.localRotation = originalRotation;
        }
    }
} 