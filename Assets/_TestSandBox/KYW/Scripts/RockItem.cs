using Fusion;
using UnityEngine;

// 🪨 스펠렁키 돌 아이템
// 던질 수 있는 기본적인 투사체 아이템
public class RockItem : BaseItem
{
    [Header("Rock Settings")]
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private float throwAngle = 30f;
    [SerializeField] private GameObject throwablePrefab; // 던진 후 생성될 프리팹
    
    // 마우스 방향으로 던지기 위한 변수
    private Vector2 customThrowDirection = Vector2.zero;
    private bool useCustomDirection = false;
    
    protected override void OnPickup(PlayerRef player)
    {
        Debug.Log($"🪨 {player}가 돌을 주웠습니다!");
    }
    
    protected override bool OnUse(SpelunkyPlayerController user)
    {
        if (user == null) return false;
        
        Debug.Log($"🪨 {user.Object.InputAuthority}가 돌을 던집니다!");
        
        // 던지는 방향 계산
        Vector2 throwDirection;
        
        if (useCustomDirection)
        {
            // 마우스 방향 사용
            throwDirection = customThrowDirection.normalized;
        }
        else
        {
            // 기본 방향 (플레이어가 바라보는 방향)
            throwDirection = user.IsFacingLeft ? Vector2.left : Vector2.right;
            
            // 각도 적용 (위쪽으로 던지기)
            float angleInRadians = throwAngle * Mathf.Deg2Rad;
            throwDirection = new Vector2(
                throwDirection.x * Mathf.Cos(angleInRadians),
                Mathf.Sin(angleInRadians)
            ).normalized;
        }
        
        // 방향 초기화
        useCustomDirection = false;
        
        // 투사체 생성 및 발사
        if (throwablePrefab != null)
        {
            CreateThrownRock(user.transform.position, throwDirection);
        }
        else
        {
            // 프리팹이 없으면 현재 오브젝트를 그대로 사용
            ThrowCurrentRock(user.transform.position, throwDirection);
        }
        
        return true; // 사용 성공
    }
    
    protected override void OnDrop()
    {
        Debug.Log("🪨 돌이 떨어졌습니다!");
    }
    
    protected override void OnDestroy()
    {
        Debug.Log("🪨 돌이 파괴되었습니다!");
    }
    
    // 🎯 마우스 방향으로 던지기 설정
    public void SetThrowDirection(Vector2 direction)
    {
        customThrowDirection = direction;
        useCustomDirection = true;
    }
    
    // 새로운 투사체 돌 생성
    private void CreateThrownRock(Vector3 throwPosition, Vector2 direction)
    {
        if (Runner != null && throwablePrefab != null)
        {
            // 네트워크 오브젝트로 투사체 생성
            var thrownRock = Runner.Spawn(throwablePrefab, throwPosition);
            var projectile = thrownRock.GetComponent<ThrownRockProjectile>();
            
            if (projectile != null)
            {
                projectile.Launch(direction * throwForce);
            }
        }
    }
    
    // 현재 돌을 투사체로 변환
    private void ThrowCurrentRock(Vector3 throwPosition, Vector2 direction)
    {
        // 현재 아이템을 던진 돌로 변환
        var projectile = gameObject.AddComponent<ThrownRockProjectile>();
        if (projectile != null)
        {
            projectile.Launch(direction * throwForce);
        }
        
        // 아이템 상태를 드롭으로 변경
        DropItemRpc(throwPosition);
    }
} 