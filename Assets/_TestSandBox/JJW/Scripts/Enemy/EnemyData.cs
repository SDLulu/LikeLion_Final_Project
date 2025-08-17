using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Info")]
    public string enemyName; //몬스터 이름
    public string description; //몬스터 설명

    [Header("Stats")]
    public int maxHp; //최대 체력
    public float moveSpeed; //이동속도
    public float attackDamage; //공격력
    
    //public float attackRange; //공격 사거리
    //public float searchDistance; //플레이어 탐지 범위
    public float attackCooldown; //공격 쿨타임

    public LayerMask PlayerLayer;
    public LayerMask PlayerHitBoxLayer;
    public LayerMask PlatformLayer;

}
