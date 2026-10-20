using UnityEngine;
using UnityEngine.UI;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class RailcannonMeter : MonoSingleton<RailcannonMeter>
{
	public Image meterBackground;

	public Image[] meters;

	private Image self;

	public GameObject[] altHudPanels;

	private float flashAmount;

	public GameObject miniVersion;

	private bool hasFlashed;

	private void Start()
	{
		CheckStatus();
	}

	private void OnEnable()
	{
		CheckStatus();
	}

	private void Update()
	{
		if (self.enabled || miniVersion.activeSelf)
		{
			for (int i = 0; i < meters.Length; i++)
			{
				if (self.enabled || i != 0)
				{
					meters[i].enabled = true;
				}
				else
				{
					meters[i].enabled = false;
				}
			}
			if (MonoSingleton<WeaponCharges>.Instance.raicharge > 4f)
			{
				if (!hasFlashed && Time.timeScale > 0f)
				{
					flashAmount = 1f;
				}
				hasFlashed = true;
				Color color = MonoSingleton<ColorBlindSettings>.Instance.GetHudColor(HudColorType.railcannonFull);
				if (flashAmount > 0f)
				{
					color = Color.Lerp(color, Color.white, flashAmount);
					flashAmount = Mathf.MoveTowards(flashAmount, 0f, Time.deltaTime);
				}
				Image[] array = meters;
				foreach (Image obj in array)
				{
					obj.fillAmount = 1f;
					obj.color = color;
				}
			}
			else
			{
				flashAmount = 0f;
				hasFlashed = false;
				Image[] array = meters;
				foreach (Image obj2 in array)
				{
					obj2.color = MonoSingleton<ColorBlindSettings>.Instance.GetHudColor(HudColorType.railcannonCharging);
					obj2.fillAmount = MonoSingleton<WeaponCharges>.Instance.raicharge / 4f;
				}
			}
			if (MonoSingleton<WeaponCharges>.Instance.raicharge > 4f || !self.enabled)
			{
				meterBackground.enabled = false;
			}
			else
			{
				meterBackground.enabled = true;
			}
		}
		else
		{
			flashAmount = 0f;
			meterBackground.enabled = false;
			hasFlashed = false;
			Image[] array = meters;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].enabled = false;
			}
		}
	}

	public void CheckStatus()
	{
		if (!self)
		{
			self = GetComponent<Image>();
		}
		if (MonoSingleton<HUDOptions>.Instance.railcannonMeter.isOn && RailcannonStatus())
		{
			if (MonoSingleton<HUDOptions>.Instance.weaponIcon.isOn)
			{
				self.enabled = true;
				miniVersion.SetActive(value: false);
			}
			else
			{
				self.enabled = false;
				miniVersion.SetActive(value: true);
			}
			GameObject[] array = altHudPanels;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: true);
			}
		}
		else
		{
			self.enabled = false;
			miniVersion.SetActive(value: false);
			GameObject[] array = altHudPanels;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: false);
			}
		}
	}

	private bool RailcannonStatus()
	{
		for (int i = 0; i < 4; i++)
		{
			string text = "rai" + i;
			if (GameProgressSaver.CheckGear(text) == 1 && PlayerPrefs.GetInt(text, 1) == 1 && !MonoSingleton<GunControl>.Instance.noWeapons)
			{
				return true;
			}
		}
		return false;
	}
}
