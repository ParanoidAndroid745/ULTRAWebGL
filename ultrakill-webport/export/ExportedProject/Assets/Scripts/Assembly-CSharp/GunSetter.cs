using System.Collections.Generic;
using UnityEngine;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class GunSetter : MonoSingleton<GunSetter>
{
	[HideInInspector]
	public GunControl gunc;

	[HideInInspector]
	public ForcedLoadout forcedLoadout;

	[Header("Revolver")]
	public GameObject[] revolverPierce;

	public GameObject[] revolverRicochet;

	public GameObject[] revolverBerserker;

	public GameObject[] revolverGold;

	[Header("Shotgun")]
	public GameObject[] shotgunGrenade;

	public GameObject[] shotgunPump;

	public GameObject[] shotgunRed;

	public GameObject[] shotgunGold;

	[Header("Nailgun")]
	public GameObject[] nailMagnet;

	public GameObject[] nailOverheat;

	public GameObject[] nailRed;

	public GameObject[] nailGold;

	[Header("Railcannon")]
	public GameObject[] railCannon;

	public GameObject[] railHarpoon;

	public GameObject[] railMalicious;

	public GameObject[] railGold;

	[Header("Rocket Launcher")]
	public GameObject[] rocketBlue;

	public GameObject[] rocketGreen;

	public GameObject[] rocketRed;

	public GameObject[] rocketGold;

	private void Start()
	{
		gunc = GetComponent<GunControl>();
		if (base.enabled)
		{
			ResetWeapons(firstTime: true);
		}
	}

	public void ResetWeapons(bool firstTime = false)
	{
		if (gunc == null)
		{
			gunc = GetComponent<GunControl>();
		}
		foreach (List<GameObject> slot in gunc.slots)
		{
			foreach (GameObject item in slot)
			{
				Object.Destroy(item);
			}
			slot.Clear();
		}
		List<int> list = CheckWeaponOrder("rev");
		for (int i = 1; i < 5; i++)
		{
			switch (list.IndexOf(i))
			{
			case 0:
				CheckWeapon("rev0", gunc.slot1, revolverPierce);
				break;
			case 1:
				CheckWeapon("rev1", gunc.slot1, revolverBerserker);
				break;
			case 2:
				CheckWeapon("rev2", gunc.slot1, revolverRicochet);
				break;
			case 3:
				CheckWeapon("rev3", gunc.slot1, revolverGold);
				break;
			}
		}
		list = CheckWeaponOrder("sho");
		for (int j = 1; j < 5; j++)
		{
			switch (list.IndexOf(j))
			{
			case 0:
				CheckWeapon("sho0", gunc.slot2, shotgunGrenade);
				break;
			case 1:
				CheckWeapon("sho1", gunc.slot2, shotgunPump);
				break;
			case 2:
				CheckWeapon("sho2", gunc.slot2, shotgunRed);
				break;
			case 3:
				CheckWeapon("sho3", gunc.slot2, shotgunGold);
				break;
			}
		}
		list = CheckWeaponOrder("nai");
		for (int k = 1; k < 5; k++)
		{
			switch (list.IndexOf(k))
			{
			case 0:
				CheckWeapon("nai0", gunc.slot3, nailMagnet);
				break;
			case 1:
				CheckWeapon("nai1", gunc.slot3, nailOverheat);
				break;
			case 2:
				CheckWeapon("nai2", gunc.slot3, nailRed);
				break;
			case 3:
				CheckWeapon("nai3", gunc.slot3, nailGold);
				break;
			}
		}
		list = CheckWeaponOrder("rai");
		for (int l = 1; l < 5; l++)
		{
			switch (list.IndexOf(l))
			{
			case 0:
				CheckWeapon("rai0", gunc.slot4, railCannon);
				break;
			case 1:
				CheckWeapon("rai1", gunc.slot4, railHarpoon);
				break;
			case 2:
				CheckWeapon("rai2", gunc.slot4, railMalicious);
				break;
			case 3:
				CheckWeapon("rai3", gunc.slot4, railGold);
				break;
			}
		}
		list = CheckWeaponOrder("rock");
		for (int m = 1; m < 5; m++)
		{
			switch (list.IndexOf(m))
			{
			case 0:
				CheckWeapon("rock0", gunc.slot5, rocketBlue);
				break;
			case 1:
				CheckWeapon("rock1", gunc.slot5, rocketGreen);
				break;
			case 2:
				CheckWeapon("rock2", gunc.slot5, rocketRed);
				break;
			case 3:
				CheckWeapon("rock3", gunc.slot5, rocketGold);
				break;
			}
		}
		gunc.UpdateWeaponList(firstTime);
	}

	private List<int> CheckWeaponOrder(string weaponType)
	{
		string text = "1234";
		if (weaponType == "rev")
		{
			text = "1324";
		}
		string text2 = PlayerPrefs.GetString(weaponType + "Ord", text);
		if (text2.Length != 4)
		{
			Debug.LogError("Faulty WeaponOrder: " + weaponType);
			text2 = text;
			PlayerPrefs.SetString(weaponType + "Ord", text);
		}
		List<int> list = new List<int>();
		for (int i = 0; i < text2.Length; i++)
		{
			list.Add(text2[i] - 48);
		}
		return list;
	}

	private void CheckWeapon(string name, List<GameObject> slot, GameObject[] prefabs)
	{
		if (prefabs == null || prefabs.Length == 0)
		{
			return;
		}
		int num = PlayerPrefs.GetInt(name, 1);
		VariantOption variantOption = VariantOption.IfEquipped;
		VariantOption variantOption2 = VariantOption.IfEquipped;
		switch (name)
		{
		case "rev0":
			variantOption = forcedLoadout?.revolver.blueVariant ?? VariantOption.IfEquipped;
			variantOption2 = forcedLoadout?.altRevolver.blueVariant ?? VariantOption.IfEquipped;
			break;
		case "rev1":
			variantOption = forcedLoadout?.revolver.redVariant ?? VariantOption.IfEquipped;
			variantOption2 = forcedLoadout?.altRevolver.redVariant ?? VariantOption.IfEquipped;
			break;
		case "rev2":
			variantOption = forcedLoadout?.revolver.greenVariant ?? VariantOption.IfEquipped;
			variantOption2 = forcedLoadout?.altRevolver.greenVariant ?? VariantOption.IfEquipped;
			break;
		case "sho0":
			variantOption = forcedLoadout?.shotgun.blueVariant ?? VariantOption.IfEquipped;
			break;
		case "sho1":
			variantOption = forcedLoadout?.shotgun.greenVariant ?? VariantOption.IfEquipped;
			break;
		case "sho2":
			variantOption = forcedLoadout?.shotgun.redVariant ?? VariantOption.IfEquipped;
			break;
		case "nai0":
			variantOption = forcedLoadout?.nailgun.blueVariant ?? VariantOption.IfEquipped;
			break;
		case "nai1":
			variantOption = forcedLoadout?.nailgun.greenVariant ?? VariantOption.IfEquipped;
			break;
		case "nai2":
			variantOption = forcedLoadout?.nailgun.redVariant ?? VariantOption.IfEquipped;
			break;
		case "rai0":
			variantOption = forcedLoadout?.railcannon.blueVariant ?? VariantOption.IfEquipped;
			break;
		case "rai1":
			variantOption = forcedLoadout?.railcannon.greenVariant ?? VariantOption.IfEquipped;
			break;
		case "rai2":
			variantOption = forcedLoadout?.railcannon.redVariant ?? VariantOption.IfEquipped;
			break;
		}
		switch (variantOption)
		{
		case VariantOption.ForceOn:
			if (prefabs[0] != null)
			{
				slot.Add(Object.Instantiate(prefabs[0], base.transform));
			}
			break;
		case VariantOption.IfEquipped:
			if (num > 0 && GameProgressSaver.CheckGear(name) > 0 && (num <= 1 || prefabs.Length < num || GameProgressSaver.CheckGear(name.Substring(0, name.Length - 1) + "alt") <= 0) && prefabs[0] != null)
			{
				slot.Add(Object.Instantiate(prefabs[0], base.transform));
			}
			break;
		}
		switch (variantOption2)
		{
		case VariantOption.ForceOn:
			if (prefabs.Length >= 2)
			{
				slot.Add(Object.Instantiate(prefabs[1], base.transform));
			}
			break;
		case VariantOption.IfEquipped:
			if (num > 0 && GameProgressSaver.CheckGear(name) > 0 && num > 1 && prefabs.Length >= num && GameProgressSaver.CheckGear(name.Substring(0, name.Length - 1) + "alt") > 0)
			{
				slot.Add(Object.Instantiate(prefabs[num - 1], base.transform));
			}
			break;
		}
	}

	public void ForceWeapon(string weaponName)
	{
		if (gunc == null)
		{
			gunc = GetComponent<GunControl>();
		}
		switch (weaponName)
		{
		case "rev0":
			if (PlayerPrefs.GetInt("rev0") == 2)
			{
				gunc.ForceWeapon(revolverPierce[1]);
			}
			else
			{
				gunc.ForceWeapon(revolverPierce[0]);
			}
			break;
		case "rev2":
			if (PlayerPrefs.GetInt("rev2") == 2)
			{
				gunc.ForceWeapon(revolverRicochet[1]);
			}
			else
			{
				gunc.ForceWeapon(revolverRicochet[0]);
			}
			break;
		case "sho0":
			gunc.ForceWeapon(shotgunGrenade[0]);
			break;
		case "sho1":
			gunc.ForceWeapon(shotgunPump[0]);
			break;
		case "nai0":
			gunc.ForceWeapon(nailMagnet[0]);
			break;
		case "nai1":
			gunc.ForceWeapon(nailOverheat[0]);
			break;
		case "rai0":
			gunc.ForceWeapon(railCannon[0]);
			break;
		case "rai1":
			gunc.ForceWeapon(railHarpoon[0]);
			break;
		case "rai2":
			gunc.ForceWeapon(railMalicious[0]);
			break;
		case "rock0":
			gunc.ForceWeapon(rocketBlue[0]);
			break;
		}
	}
}
