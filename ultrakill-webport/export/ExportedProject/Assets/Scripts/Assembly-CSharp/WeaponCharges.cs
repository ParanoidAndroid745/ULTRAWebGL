using System.Collections.Generic;
using UnityEngine;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class WeaponCharges : MonoSingleton<WeaponCharges>
{
	private GunControl gc;

	public float rev0charge = 100f;

	public bool rev0alt;

	public float rev1charge = 400f;

	[HideInInspector]
	public bool nai0set;

	public float nai0charge = 2f;

	public float naiheatUp;

	public float naiAmmo = 100f;

	public bool naiAmmoDontCharge;

	[HideInInspector]
	public List<Magnet> magnets = new List<Magnet>();

	public float naiMagnetCharge = 3f;

	public float raicharge = 5f;

	public GameObject railCannonFullChargeSound;

	public bool railChargePlayed;

	[HideInInspector]
	public bool rocketset;

	public float rocketcharge;

	public float revaltpickupcharge;

	private void Update()
	{
		if (rev0charge < 100f)
		{
			if (rev0alt)
			{
				rev0charge = Mathf.MoveTowards(rev0charge, 100f, 20f * Time.deltaTime);
			}
			else
			{
				rev0charge = Mathf.MoveTowards(rev0charge, 100f, 40f * Time.deltaTime);
			}
		}
		if (rev1charge < 400f)
		{
			rev1charge = Mathf.MoveTowards(rev1charge, 400f, 25f * Time.deltaTime);
		}
		if (nai0charge < 3f)
		{
			nai0charge = Mathf.MoveTowards(nai0charge, 2f, Time.deltaTime * 0.075f);
		}
		if (naiheatUp > 0f)
		{
			naiheatUp = Mathf.MoveTowards(naiheatUp, 0f, Time.deltaTime * 0.3f);
		}
		if (raicharge < 5f)
		{
			raicharge = Mathf.MoveTowards(raicharge, 5f, Time.deltaTime * 0.25f);
			if (raicharge >= 4f && (bool)railCannonFullChargeSound)
			{
				raicharge = 5f;
				PlayRailCharge();
			}
		}
		if (rocketcharge > 0f)
		{
			rocketcharge = Mathf.MoveTowards(rocketcharge, 0f, Time.deltaTime);
		}
		if (revaltpickupcharge > 0f)
		{
			revaltpickupcharge = Mathf.MoveTowards(revaltpickupcharge, 0f, Time.deltaTime);
		}
		if (!naiAmmoDontCharge)
		{
			naiAmmo = Mathf.MoveTowards(naiAmmo, 100f, Time.deltaTime * 3.5f);
		}
		if (magnets.Count > 0)
		{
			for (int num = magnets.Count - 1; num >= 0; num--)
			{
				if (magnets[num] == null)
				{
					magnets.RemoveAt(num);
				}
			}
			if (magnets.Count < 3 && naiMagnetCharge < (float)(3 - magnets.Count))
			{
				naiMagnetCharge = Mathf.MoveTowards(naiMagnetCharge, 3 - magnets.Count, Time.deltaTime * 3f);
			}
		}
		else if (naiMagnetCharge < 3f)
		{
			naiMagnetCharge = Mathf.MoveTowards(naiMagnetCharge, 3f, Time.deltaTime * 3f);
		}
	}

	public void MaxCharges()
	{
		rev0charge = 100f;
		rev1charge = 400f;
		nai0charge = 2f;
		naiheatUp = 0f;
		rocketcharge = 0f;
		revaltpickupcharge = 0f;
		naiAmmo = 100f;
		magnets.Clear();
		naiMagnetCharge = 3f;
		if (raicharge < 5f)
		{
			raicharge = 4.9f;
			PlayRailCharge();
		}
		if (!gc)
		{
			gc = GetComponent<GunControl>();
		}
		if ((bool)gc && (bool)gc.currentWeapon)
		{
			gc.currentWeapon.SendMessage("MaxCharge", SendMessageOptions.DontRequireReceiver);
		}
	}

	public void PlayRailCharge()
	{
		railChargePlayed = true;
		Object.Instantiate(railCannonFullChargeSound);
	}
}
