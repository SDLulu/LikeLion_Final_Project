using Fusion;
using UnityEngine;

// ExampleEnemy, EnemyInteractionBase(추상클래스), IEnemyInteraction(인터페이스) 순서로 안전하게 처리하는 예시
public class EnemyInteractionTester : NetworkBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float knockbackForce = 3f;
    [SerializeField] private float stunDuration = 3f;
    [SerializeField] private LayerMask enemyLayerMask = -1;
    [SerializeField] private float interactionRange = 2f;

    private void Update()
    {
        if (!HasStateAuthority) return;
        TestEnemyInteractions();
    }

    private void TestEnemyInteractions()
    {
        if (Input.GetKeyDown(KeyCode.J)) AttackNearbyEnemies();
        if (Input.GetKeyDown(KeyCode.K)) StunNearbyEnemies();
        if (Input.GetKeyDown(KeyCode.L)) MakeEnemiesInvincible();
        if (Input.GetKeyDown(KeyCode.Space)) TryPickupEnemies();
    }

    // 1. 공격: 데미지 + 넉백
    private void AttackNearbyEnemies()
    {
        var colliders = Physics2D.OverlapCircleAll(transform.position, interactionRange, enemyLayerMask);
        foreach (var col in colliders)
        {
            // 1순위: ExampleEnemy (구체 클래스)
            if (col.TryGetComponent<ExampleEnemy>(out var exampleEnemy))
            {
                Vector3 knockbackDir = (col.transform.position - transform.position).normalized;
                exampleEnemy.Hit((int)attackDamage, knockbackDir * knockbackForce);
                continue;
            }
            // 2순위: EnemyInteractionBase (추상클래스)
            if (col.TryGetComponent<EnemyInteractionBase>(out var enemyBase))
            {
                Vector3 knockbackDir = (col.transform.position - transform.position).normalized;
                enemyBase.TakeDamage((int)attackDamage);
                enemyBase.ApplyKnockback(knockbackDir * knockbackForce);
                continue;
            }
            // 3순위: IEnemyInteraction (인터페이스)
            if (col.TryGetComponent<IEnemyInteraction>(out var enemyInteraction))
            {
                Vector3 knockbackDir = (col.transform.position - transform.position).normalized;
                enemyInteraction.TakeDamage((int)attackDamage);
                enemyInteraction.ApplyKnockback(knockbackDir * knockbackForce);
            }
        }
    }

    // 2. 스턴
    private void StunNearbyEnemies()
    {
        var colliders = Physics2D.OverlapCircleAll(transform.position, interactionRange, enemyLayerMask);
        foreach (var col in colliders)
        {
            if (col.TryGetComponent<ExampleEnemy>(out var exampleEnemy))
            {
                exampleEnemy.GetStunned();
                continue;
            }
            if (col.TryGetComponent<EnemyInteractionBase>(out var enemyBase))
            {
                enemyBase.ApplyStun(stunDuration);
                continue;
            }
            if (col.TryGetComponent<IEnemyInteraction>(out var enemyInteraction))
            {
                enemyInteraction.ApplyStun(stunDuration);
            }
        }
    }

    // 3. 무적 상태 만들기
    private void MakeEnemiesInvincible()
    {
        var colliders = Physics2D.OverlapCircleAll(transform.position, interactionRange, enemyLayerMask);
        foreach (var col in colliders)
        {
            if (col.TryGetComponent<ExampleEnemy>(out var exampleEnemy))
            {
                exampleEnemy.BecomeInvincible(5f);
                continue;
            }
            if (col.TryGetComponent<EnemyInteractionBase>(out var enemyBase))
            {
                enemyBase.SetInvincible(true, 5f);
                continue;
            }
            if (col.TryGetComponent<IEnemyInteraction>(out var enemyInteraction))
            {
                enemyInteraction.SetInvincible(true, 5f);
            }
        }
    }

    // 4. 들기 시도
    private void TryPickupEnemies()
    {
        var colliders = Physics2D.OverlapCircleAll(transform.position, interactionRange, enemyLayerMask);
        foreach (var col in colliders)
        {
            // 들기/놓기 관련은 인터페이스만 처리 (추상클래스에 구현 안 했으므로)
            if (col.TryGetComponent<IEnemyInteraction>(out var enemyInteraction))
            {
                if (enemyInteraction.IsHoldable)
                {
                    enemyInteraction.OnPickedUp(transform);
                    Debug.Log($"[{name}] {col.name}을(를) 들었습니다!");
                }
                else
                {
                    Debug.Log($"[{name}] {col.name}은(는) 지금 들 수 없습니다!");
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
} 