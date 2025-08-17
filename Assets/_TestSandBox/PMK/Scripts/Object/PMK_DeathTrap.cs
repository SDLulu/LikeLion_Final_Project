using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class PMK_DeathTrap : NetworkBehaviour
{
    [Header("가시 설정")]
    [SerializeField] private int damageAmount = 99; // 가시 데미지
    [SerializeField] private float fallSpeedThreshold = 2f; // 위에서 떨어지는 속도 임계값
    [SerializeField] private float slowFallGravity = 0.01f; // 천천히 떨어지는 중력 값

    // 시체 오브젝트들을 추적하는 리스트
    private List<Rigidbody2D> attachedCorpses = new List<Rigidbody2D>();

    public override void Spawned()
    {
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 캐릭터의 Rigidbody2D 가져오기
        Rigidbody2D characterRb = other.GetComponent<Rigidbody2D>();
        if (characterRb == null) return;

        // IPlayerInteraction 컴포넌트 찾기
        var characterInteraction = other.GetComponent<IPlayerInteraction>();
        if (characterInteraction != null)
        {
            // 캐릭터가 위에서 떨어지고 있는지 확인 (y 속도가 음수이고 임계값보다 빠름)
            if (characterRb.linearVelocity.y < -fallSpeedThreshold)
            {
                // 데미지 적용 (StateAuthority가 있는 경우에만)
                if (HasStateAuthority)
                {
                    characterInteraction.TakeDamage(damageAmount);
                    Debug.Log($"💀 PMK_DeathTrap: 캐릭터가 가시에 찔려 {damageAmount} 데미지를 받았습니다!");
                    
                }
            }
        }
        else
        {
            // IPlayerInteraction이 없지만 Corpse 태그라면 중력만 적용
            if (other.CompareTag("Corpse"))
            {
                Debug.Log($"💀 PMK_DeathTrap: Corpse 태그 오브젝트가 가시에 찔려 중력 효과만 적용됩니다.");
                ApplySlowFallEffect(characterRb);
                
                // 시체 리스트에 추가
                if (!attachedCorpses.Contains(characterRb))
                {
                    attachedCorpses.Add(characterRb);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Corpse 태그 오브젝트가 가시에서 벗어나면 중력 복원
        if (other.CompareTag("Corpse"))
        {
            var characterRb = other.GetComponent<Rigidbody2D>();
            if (characterRb != null)
            {
                RestoreGravity(characterRb);
                
                // 시체 리스트에서 제거
                attachedCorpses.Remove(characterRb);
            }
        }
    }

    private void Update()
    {
        // 붙어있는 시체들의 중력을 지속적으로 느리게 유지
        for (int i = attachedCorpses.Count - 1; i >= 0; i--)
        {
            if (attachedCorpses[i] == null)
            {
                attachedCorpses.RemoveAt(i);
                continue;
            }
            
            // 시체가 가시에 붙어있으면 중력을 느리게 유지
            if (attachedCorpses[i].gravityScale > slowFallGravity)
            {
                attachedCorpses[i].gravityScale = slowFallGravity;
            }
        }
    }

    /// <summary>
    /// 캐릭터에게 천천히 떨어지는 효과를 적용합니다.
    /// </summary>
    private void ApplySlowFallEffect(Rigidbody2D characterRb)
    {
        if (characterRb == null) return;
        
        // 중력 스케일을 조정하여 천천히 떨어지도록 함
        characterRb.gravityScale = slowFallGravity; // 기본 중력의 1%로 설정
    }

    /// <summary>
    /// 캐릭터의 중력을 원래대로 복원합니다.
    /// </summary>
    private void RestoreGravity(Rigidbody2D characterRb)
    {
        if (characterRb == null) return;
        
        // 중력을 원래대로 복원
        characterRb.gravityScale = 1f;
        Debug.Log($"💀 PMK_DeathTrap: 시체의 중력이 원래대로 복원되었습니다.");
    }
}