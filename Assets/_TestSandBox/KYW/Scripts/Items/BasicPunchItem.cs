using UnityEngine;
using System.Collections;

// 기본 주먹 아이템 (콜라이더로 판정)
public class BasicPunchItem : UsableItemBase
{
    public float punchDamage = 1f;
    public float punchCooldown = 0.3f;
    public LayerMask targetLayers = -1;

    private float lastPunchTime;
    private Collider2D punchCollider;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Transform cachedTransform;

    private void Awake()
    {
        punchCollider = GetComponent<Collider2D>();
        if (punchCollider != null) punchCollider.enabled = false;
        cachedTransform = transform;
        originalLocalPosition = cachedTransform.localPosition;
        originalLocalRotation = cachedTransform.localRotation;
        gameObject.SetActive(false); // 기본적으로 비활성화
    }

    public override void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        base.OnUsePress(mouseWorldPosition, playerPosition);
    }

    protected override void StartUse(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        cachedTransform.localPosition = originalLocalPosition;
        cachedTransform.localRotation = originalLocalRotation;
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(PunchRoutine(mouseWorldPosition, playerPosition));
    }

    private IEnumerator PunchRoutine(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        Vector2 dir = (mouseWorldPosition - playerPosition).normalized;
        float punchDistance = 1.2f;
        float punchSpeed = 0.08f;
        float returnSpeed = 0.07f;
        Vector3 punchTarget = originalLocalPosition + (Vector3)(dir * punchDistance);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / punchSpeed;
            cachedTransform.localPosition = Vector3.Lerp(originalLocalPosition, punchTarget, t);
            yield return null;
        }
        cachedTransform.localPosition = punchTarget;

        if (punchCollider != null)
        {
            punchCollider.enabled = true;
            yield return new WaitForSeconds(0.05f);
            punchCollider.enabled = false;
        }

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / returnSpeed;
            cachedTransform.localPosition = Vector3.Lerp(punchTarget, originalLocalPosition, t);
            yield return null;
        }
        cachedTransform.localPosition = originalLocalPosition;
        cachedTransform.localRotation = originalLocalRotation;

        gameObject.SetActive(false);
        EndUse(mouseWorldPosition, playerPosition);
    }

    // OnTriggerEnter2D에서 피격 처리
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;
        var damageable = other.GetComponent<IHitReaction>();
        if (damageable != null)
        {
            Vector2 dir = (other.transform.position - transform.position).normalized;
            damageable.ApplyHit(dir * 5f, 0.1f, 0.1f, 0.3f);
        }
    }

    public override void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
    public override void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }
} 