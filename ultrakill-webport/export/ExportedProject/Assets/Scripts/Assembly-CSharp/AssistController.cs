using UnityEngine;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class AssistController : MonoSingleton<AssistController>
{
	[HideInInspector]
	public bool majorEnabled;

	[HideInInspector]
	public bool cheatsEnabled;

	[HideInInspector]
	public float gameSpeed;

	[HideInInspector]
	public float damageTaken;

	[HideInInspector]
	public bool infiniteStamina;

	[HideInInspector]
	public bool disableHardDamage;

	private StatsManager sman;

	private void Start()
	{
		if (PlayerPrefs.GetInt("MajAss", 0) == 1)
		{
			majorEnabled = true;
		}
		else
		{
			majorEnabled = false;
		}
		gameSpeed = PlayerPrefs.GetFloat("GamSpe", 100f) / 100f;
		damageTaken = PlayerPrefs.GetFloat("DamTak", 100f) / 100f;
		if (PlayerPrefs.GetInt("InfSta", 0) == 1)
		{
			infiniteStamina = true;
		}
		else
		{
			infiniteStamina = false;
		}
		if (PlayerPrefs.GetInt("DisHarDam", 0) == 1)
		{
			disableHardDamage = true;
		}
		else
		{
			disableHardDamage = false;
		}
	}

	public void MajorEnabled()
	{
		majorEnabled = true;
		if (sman == null)
		{
			sman = MonoSingleton<StatsManager>.Instance;
		}
		sman.MajorUsed();
	}
}
