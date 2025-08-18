using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class SpeedCollisionSettingsProvider : MonoBehaviour
{
	[SerializeField] private SpeedCollisionSettingsSO settings;

	public static SpeedCollisionSettingsProvider Instance { get; private set; }

	public SpeedCollisionSettingsSO Settings
	{
		get { return settings; }
		set { settings = value; }
	}

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);

		if (settings == null)
		{
			// Resources/SpeedCollisionSettingsSO(또는 같은 이름)에서 자동 로드 시도
			var loaded = Resources.Load<SpeedCollisionSettingsSO>("SpeedCollisionSettingsSO");
			if (loaded != null)
			{
				settings = loaded;
			}
		}
	}
}


