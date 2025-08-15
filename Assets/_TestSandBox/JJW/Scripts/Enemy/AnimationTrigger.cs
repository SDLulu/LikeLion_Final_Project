using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationTrigger : MonoBehaviour
{
    private EnemyBase enemy;
    void Awake()
    {
        enemy = GetComponentInParent<EnemyBase>();
    }

    public void TriggerDealDamage()
    {
        if (enemy != null)
        {
            enemy.DealDamage();
        }
        else
        {
            Debug.Log("부모에서 EnemyBase를 찾을 수 없습니다.");
        }
    }
}
