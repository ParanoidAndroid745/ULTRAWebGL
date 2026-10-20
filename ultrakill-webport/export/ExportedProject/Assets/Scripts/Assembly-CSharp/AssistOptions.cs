using UnityEngine;
using UnityEngine.UI;

public class AssistOptions : MonoBehaviour
{
	public Toggle majorEnable;

	public Slider gameSpeed;

	public Slider damageTaken;

	public Toggle infiniteStamina;

	public Toggle disableHardDamage;

	public Toggle autoAim;

	public Slider autoAimSlider;

	public GameObject majorPopup;

	public GameObject majorBlocker;

	private AssistController asscon;

	private NewMovement nmov;

	public static float autoAimAmount;

	private void CheckAssistController()
	{
		if (nmov == null)
		{
			nmov = MonoSingleton<NewMovement>.Instance;
		}
		if (asscon == null)
		{
			asscon = MonoSingleton<AssistController>.Instance;
		}
	}

	private void Start()
	{
		CheckAssistController();
		if (PlayerPrefs.GetInt("MajAss", 0) == 1)
		{
			majorEnable.isOn = true;
			majorBlocker.SetActive(value: false);
		}
		else
		{
			majorEnable.isOn = false;
			majorBlocker.SetActive(value: true);
		}
		gameSpeed.value = PlayerPrefs.GetFloat("GamSpe", 100f);
		damageTaken.value = PlayerPrefs.GetFloat("DamTak", 100f);
		if (PlayerPrefs.GetInt("InfSta", 0) == 1)
		{
			infiniteStamina.isOn = true;
		}
		else
		{
			infiniteStamina.isOn = false;
		}
		if (PlayerPrefs.GetInt("DisHarDam", 0) == 1)
		{
			disableHardDamage.isOn = true;
		}
		else
		{
			disableHardDamage.isOn = false;
		}
		if (PlayerPrefs.GetInt("AutoAim", 0) == 1)
		{
			autoAim.isOn = true;
		}
		else
		{
			autoAim.isOn = false;
		}
		autoAimSlider.value = PlayerPrefs.GetFloat("AutoAimAmount", 0.2f) * 100f;
		MonoSingleton<CameraFrustumTargeter>.Instance.maxHorAim = autoAimSlider.value / 100f;
	}

	public void MajorCheck()
	{
		CheckAssistController();
		if (PlayerPrefs.GetInt("MajAss", 0) == 0)
		{
			majorPopup.SetActive(value: true);
			return;
		}
		PlayerPrefs.SetInt("MajAss", 0);
		asscon.majorEnabled = false;
		majorEnable.isOn = false;
		majorBlocker.SetActive(value: true);
	}

	public void MajorEnable()
	{
		CheckAssistController();
		PlayerPrefs.SetInt("MajAss", 1);
		asscon.MajorEnabled();
		majorEnable.isOn = true;
		majorBlocker.SetActive(value: false);
	}

	public void GameSpeed(float stuff)
	{
		CheckAssistController();
		PlayerPrefs.SetFloat("GamSpe", stuff);
		asscon.gameSpeed = stuff / 100f;
	}

	public void DamageTaken(float stuff)
	{
		CheckAssistController();
		PlayerPrefs.SetFloat("DamTak", stuff);
		asscon.damageTaken = stuff / 100f;
	}

	public void InfiniteStamina(bool stuff)
	{
		CheckAssistController();
		if (stuff)
		{
			PlayerPrefs.SetInt("InfSta", 1);
		}
		else
		{
			PlayerPrefs.SetInt("InfSta", 0);
		}
		asscon.infiniteStamina = stuff;
	}

	public void DisableHardDamage(bool stuff)
	{
		CheckAssistController();
		if (stuff)
		{
			PlayerPrefs.SetInt("DisHarDam", 1);
		}
		else
		{
			PlayerPrefs.SetInt("DisHarDam", 0);
		}
		asscon.disableHardDamage = stuff;
	}

	public void AutoAim(bool stuff)
	{
		CameraFrustumTargeter.IsEnabled = stuff;
		autoAimSlider.transform.parent.gameObject.SetActive(stuff);
	}

	public void AutoAimAmount(float stuff)
	{
		if (CameraFrustumTargeter.IsEnabled && (bool)MonoSingleton<CameraFrustumTargeter>.Instance)
		{
			MonoSingleton<CameraFrustumTargeter>.Instance.maxHorAim = stuff / 100f;
		}
		PlayerPrefs.SetFloat("AutoAimAmount", stuff / 100f);
	}
}
