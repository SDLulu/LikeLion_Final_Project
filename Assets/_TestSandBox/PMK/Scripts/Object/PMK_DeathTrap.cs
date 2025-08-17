using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class PMK_DeathTrap : NetworkBehaviour
{
    [Header("가시 설정")]
    [SerializeField] private int damageAmount = 99; // 가시 데미지
    [SerializeField] private float fallSpeedThreshold = 2f; // 위에서 떨어지는 속도 임계값
    [SerializeField] private float slowFallGravity = 0.01f; // 천천히 떨어지는 중력 값

    private Rigidbody2D rb;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        // 캐릭터의 Rigidbody2D 가져오기
        Rigidbody2D characterRb = other.GetComponent<Rigidbody2D>();
        if (characterRb == null) return;

        // 캐릭터가 위에서 떨어지고 있는지 확인 (y 속도가 음수이고 임계값보다 빠름)
        if (characterRb.linearVelocity.y < -fallSpeedThreshold)
        {
            // IPlayerInteraction 컴포넌트 찾기
            var characterInteraction = other.GetComponent<IPlayerInteraction>();
            if (characterInteraction != null)
            {
                // 데미지 적용 (StateAuthority가 있는 경우에만)
                if (HasStateAuthority)
                {
                    characterInteraction.TakeDamage(damageAmount);
                    Debug.Log($"💀 PMK_DeathTrap: 캐릭터가 가시에 찔려 {damageAmount} 데미지를 받았습니다!");
                    
                    // 천천히 떨어지도록 중력 적용
                    ApplySlowFallEffect(characterRb);
                }
            }
        }
    }

    /// <summary>
    /// 캐릭터에게 천천히 떨어지는 효과를 적용합니다.
    /// </summary>
    private void ApplySlowFallEffect(Rigidbody2D characterRb)
    {
        if (characterRb == null) return;

        // 현재 속도를 유지하되 y 속도만 천천히 떨어지도록 조정
        Vector2 currentVelocity = characterRb.linearVelocity;
        
        // 중력 스케일을 조정하여 천천히 떨어지도록 함
        characterRb.gravityScale = slowFallGravity; // 기본 중력의 1%로 설정
    }

}