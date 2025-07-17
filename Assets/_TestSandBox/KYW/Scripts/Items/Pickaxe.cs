using UnityEngine;

// 곡괭이 - 블록 파괴와 적 공격이 가능한 도구
public class Pickaxe : MonoBehaviour, IUsableItem
{
    [Header("Attack Settings")]
    [SerializeField] private float damage = 2f;           // 공격력
    [SerializeField] private float attackDistance = 1.5f; // 공격 거리
    [SerializeField] private float attackSpeed = 0.3f;    // 공격 속도(초)
    [SerializeField] private float swingDuration = 0.15f; // 휘두르는 시간

    [Header("Target Settings")]
    [SerializeField] private LayerMask targetLayers = -1; // 타겟 레이어 (적, 블록 등)

    private SpriteRenderer spriteRenderer;
    private Transform cachedTransform;
    private Vector3 originalLocalPosition;
    private Vector3 originalLocalRotation;
    private bool isSwinging = false;
    private float swingTimer = 0f;
    private float lastSwingTime = 0f;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        cachedTransform = transform;
        originalLocalPosition = cachedTransform.localPosition;
        originalLocalRotation = cachedTransform.localRotation.eulerAngles;
    }

    private void Update()
    {
        if (!isSwinging) return;

        swingTimer += Time.deltaTime;
        float normalizedTime = swingTimer / swingDuration;

        if (normalizedTime <= 1f)
        {
            // 곡괭이 휘두르기 모션 (회전)
            float swingAngle = Mathf.Lerp(0f, -120f, normalizedTime);
            cachedTransform.localRotation = Quaternion.Euler(originalLocalRotation + new Vector3(0, 0, swingAngle));
        }
        else
        {
            // 휘두르기 종료
            FinishSwing();
        }
    }

    public void OnUsePress(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        TrySwing(mouseWorldPosition, playerPosition);
    }

    public void OnUseHold(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        TrySwing(mouseWorldPosition, playerPosition);
    }

    public void OnUseRelease(Vector2 mouseWorldPosition, Vector2 playerPosition) { }

    private void TrySwing(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 공격 속도 체크
        if (Time.time - lastSwingTime < attackSpeed) return;
        if (isSwinging) return;

        StartSwing(mouseWorldPosition, playerPosition);
        CheckHit(mouseWorldPosition, playerPosition);
        lastSwingTime = Time.time;
    }

    private void StartSwing(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        isSwinging = true;
        swingTimer = 0f;
    }

    private void FinishSwing()
    {
        isSwinging = false;
        cachedTransform.localRotation = Quaternion.Euler(originalLocalRotation);
    }

    private void CheckHit(Vector2 mouseWorldPosition, Vector2 playerPosition)
    {
        // 공격 방향
        Vector2 direction = (mouseWorldPosition - playerPosition).normalized;
        
        // 부채꼴 모양으로 히트 체크
        float hitAngle = 60f; // 120도의 절반
        Vector2 hitStartDirection = RotateVector2(direction, -hitAngle);
        Vector2 hitEndDirection = RotateVector2(direction, hitAngle);
        
        // 범위 내 모든 타겟 감지
        Collider2D[] hits = Physics2D.OverlapAreaAll(
            playerPosition + hitStartDirection * attackDistance,
            playerPosition + hitEndDirection * attackDistance,
            targetLayers
        );

        foreach (var hit in hits)
        {
            // 블록 파괴 처리 - 임시로 주석 처리
            /*
            var destructible = hit.GetComponent<IDestructible>();
            if (destructible != null)
            {
                destructible.OnDestroy();
                continue;
            }
            */
            
            // 디버그용 로그 추가
            Debug.Log($"곡괭이가 {hit.gameObject.name}에 히트!");

            // 적 데미지 처리
            var damageable = hit.GetComponent<IHitReaction>();
            if (damageable != null)
            {
                Vector2 hitDirection = (hit.transform.position - transform.position).normalized;
                damageable.ApplyHit(hitDirection * 3f, damage, 0.1f, 0.2f);
            }
        }
    }

    private Vector2 RotateVector2(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    private void OnDrawGizmos()
    {
        // 공격 범위 시각화
        if (!Application.isPlaying) return;
        
        Vector2 position = transform.position;
        Vector2 direction = ((Vector2)transform.right).normalized;
        float hitAngle = 60f;
        
        Gizmos.color = Color.red;
        Vector2 hitStart = position + RotateVector2(direction, -hitAngle) * attackDistance;
        Vector2 hitEnd = position + RotateVector2(direction, hitAngle) * attackDistance;
        
        Gizmos.DrawLine(position, hitStart);
        Gizmos.DrawLine(position, hitEnd);
        Gizmos.DrawLine(hitStart, hitEnd);
    }
} 