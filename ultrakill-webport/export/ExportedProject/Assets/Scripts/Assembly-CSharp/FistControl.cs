using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class FistControl : MonoSingleton<FistControl>
{
	private InputManager inman;

	public ForcedLoadout forcedLoadout;

	public GameObject blueArm;

	public GameObject redArm;

	public GameObject greenArm;

	public GameObject goldArm;

	private int currentOrderNum;

	private int currentVarNum;

	private List<GameObject> spawnedArms = new List<GameObject>();

	private List<int> spawnedArmNums = new List<int>();

	private AudioSource aud;

	public bool shopping;

	private int shopRequests;

	public GameObject[] fistPanels;

	public Image fistIcon;

	public ItemIdentifier heldObject;

	public float fistCooldown;

	public float weightCooldown;

	public bool activated = true;

	public Punch currentPunch;

	[HideInInspector]
	public int forceNoHold;

	public GameObject currentArmObject => spawnedArms[currentOrderNum];

	private void Start()
	{
		inman = MonoSingleton<InputManager>.Instance;
		aud = GetComponent<AudioSource>();
		currentVarNum = PlayerPrefs.GetInt("CurArm", 0);
		ResetFists();
		fistCooldown = 0f;
	}

	private void Update()
	{
		if (spawnedArms.Count > 1 && !shopping && activated && MonoSingleton<InputManager>.Instance.InputSource.ChangeFist.WasPerformedThisFrame)
		{
			ScrollArm();
		}
		if (spawnedArms.Count == 0 && MonoSingleton<InputManager>.Instance.InputSource.Punch.WasPerformedThisFrame && (forcedLoadout == null || forcedLoadout.arm.blueVariant != VariantOption.ForceOff || forcedLoadout.arm.redVariant != VariantOption.ForceOff))
		{
			MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("<color=red>CAN'T PUNCH IF YOU HAVE NO ARM EQUIPPED, DUMBASS</color>\nArms can be re-equipped at the shop");
		}
		if (fistCooldown > -1f)
		{
			fistCooldown = Mathf.MoveTowards(fistCooldown, 0f, Time.deltaTime * 2f);
		}
		if (weightCooldown > 0f)
		{
			weightCooldown = Mathf.MoveTowards(weightCooldown, 0f, Time.deltaTime * (weightCooldown / 2f + 0.1f));
		}
	}

	public void ScrollArm()
	{
		if (currentOrderNum < spawnedArms.Count - 1)
		{
			ArmChange(currentOrderNum + 1);
		}
		else
		{
			ArmChange(0);
		}
		aud.Play();
	}

	public void RefreshArm()
	{
		ArmChange(currentOrderNum);
	}

	public void ForceArm(int varNum, bool animation = false)
	{
		if (spawnedArmNums.Contains(varNum))
		{
			ArmChange(spawnedArmNums.IndexOf(varNum));
		}
		else
		{
			ArmChange(0);
		}
		if (animation)
		{
			spawnedArms[currentOrderNum].GetComponent<Punch>().EquipAnimation();
			aud.Play();
		}
	}

	public void ArmChange(int orderNum)
	{
		if (orderNum < spawnedArms.Count)
		{
			if (currentOrderNum < spawnedArms.Count)
			{
				spawnedArms[currentOrderNum].SetActive(value: false);
			}
			spawnedArms[orderNum].SetActive(value: true);
			currentOrderNum = orderNum;
			currentVarNum = spawnedArmNums[orderNum];
			PlayerPrefs.SetInt("CurArm", currentVarNum);
			UpdateFistIcon();
			currentPunch = currentArmObject.GetComponent<Punch>();
		}
	}

	public void UpdateFistIcon()
	{
		if ((bool)fistIcon)
		{
			int num = currentVarNum;
			switch (num)
			{
			case 1:
				num = 2;
				break;
			case 2:
				num = 1;
				break;
			}
			fistIcon.color = MonoSingleton<ColorBlindSettings>.Instance.variationColors[num];
		}
	}

	public void NoFist()
	{
		if (spawnedArms.Count > 0)
		{
			spawnedArms[currentOrderNum].SetActive(value: false);
		}
		activated = false;
	}

	public void YesFist()
	{
		if (spawnedArms.Count > 0)
		{
			spawnedArms[currentOrderNum].SetActive(value: true);
		}
		activated = true;
		UpdateFistIcon();
	}

	public void ResetFists()
	{
		if (spawnedArms.Count > 0)
		{
			for (int i = 0; i < spawnedArms.Count; i++)
			{
				Object.Destroy(spawnedArms[i]);
			}
			spawnedArms.Clear();
			spawnedArmNums.Clear();
		}
		if ((PlayerPrefs.GetInt("arm0", 1) == 1 && (forcedLoadout == null || forcedLoadout.arm.blueVariant == VariantOption.IfEquipped)) || (forcedLoadout != null && forcedLoadout.arm.blueVariant == VariantOption.ForceOn))
		{
			GameObject item = Object.Instantiate(blueArm, base.transform);
			spawnedArms.Add(item);
			spawnedArmNums.Add(0);
		}
		CheckFist("arm1");
		CheckFist("arm2");
		CheckFist("arm3");
		if (spawnedArms.Count <= 1 || PlayerPrefs.GetInt("ArmIco", 1) == 0)
		{
			GameObject[] array = fistPanels;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].SetActive(value: false);
			}
		}
		else
		{
			GameObject[] array = fistPanels;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].SetActive(value: true);
			}
		}
		ForceArm(currentVarNum);
		UpdateFistIcon();
	}

	private void CheckFist(string name)
	{
		if (forcedLoadout != null && name == "arm1")
		{
			if (forcedLoadout.arm.redVariant == VariantOption.ForceOn)
			{
				spawnedArmNums.Add(1);
				spawnedArms.Add(Object.Instantiate(redArm, base.transform));
				return;
			}
			if (forcedLoadout.arm.redVariant == VariantOption.ForceOff)
			{
				return;
			}
		}
		if (PlayerPrefs.GetInt(name, 0) == 1 && GameProgressSaver.CheckGear(name) == 1)
		{
			GameObject gameObject = null;
			switch (name)
			{
			case "arm1":
				gameObject = Object.Instantiate(redArm, base.transform);
				spawnedArmNums.Add(1);
				break;
			case "arm2":
				gameObject = Object.Instantiate(greenArm, base.transform);
				spawnedArmNums.Add(2);
				break;
			case "arm3":
				gameObject = Object.Instantiate(goldArm, base.transform);
				spawnedArmNums.Add(3);
				break;
			}
			if (gameObject != null)
			{
				spawnedArms.Add(gameObject);
			}
		}
	}

	public void ShopMode()
	{
		shopping = true;
		shopRequests++;
	}

	public void StopShop()
	{
		shopRequests--;
		if (shopRequests <= 0)
		{
			shopping = false;
		}
	}
}
