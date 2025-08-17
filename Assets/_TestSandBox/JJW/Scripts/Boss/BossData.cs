using UnityEngine;

[CreateAssetMenu(fileName = "New Boss Data", menuName = "Boss/Boss Data")]
public class BossData : ScriptableObject
{
    [Header("Info")]
    public string bossName; //몬스터 이름
    public int maxHp; //최대 체력

    [Header("Layers")]
    public LayerMask PlayerLayer;
    public LayerMask PlayerHitBoxLayer;
    public LayerMask PlatformLayer;
}
