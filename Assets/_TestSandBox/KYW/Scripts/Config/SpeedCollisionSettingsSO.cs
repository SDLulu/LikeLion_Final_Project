using UnityEngine;

[CreateAssetMenu(menuName = "KYW/Combat/Speed Collision Settings", fileName = "SpeedCollisionSettingsSO")]
public class SpeedCollisionSettingsSO : ScriptableObject
{
	[Header("Speed Thresholds")]
	[SerializeField] private float attackSpeedThreshold = 3f;   // 공격 모드로 전환 속도 임계값
	[SerializeField] private float normalSpeedThreshold = 1f;   // 일반 모드로 복귀 속도 임계값

	public float AttackSpeedThreshold { get { return attackSpeedThreshold; } set { attackSpeedThreshold = value; } }
	public float NormalSpeedThreshold { get { return normalSpeedThreshold; } set { normalSpeedThreshold = value; } }
}


