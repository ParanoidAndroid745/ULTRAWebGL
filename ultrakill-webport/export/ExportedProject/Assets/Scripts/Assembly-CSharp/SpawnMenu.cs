using UnityEngine;
using UnityEngine.UI;

public class SpawnMenu : MonoBehaviour
{
	[SerializeField]
	private SpawnMenuSectionReference sectionReference;

	[SerializeField]
	private SpawnableObjectsDatabase objects;

	[HideInInspector]
	public DebugArm arm;

	[SerializeField]
	private Sprite lockedIcon;

	private void Awake()
	{
		CreateButtons(objects.enemies, "ENEMIES");
		CreateButtons(objects.objects, "ITEMS");
		sectionReference.gameObject.SetActive(value: false);
	}

	private void CreateButtons(SpawnableObject[] list, string sectionName)
	{
		SpawnMenuSectionReference spawnMenuSectionReference = Object.Instantiate(sectionReference, sectionReference.transform.parent);
		spawnMenuSectionReference.sectionName.text = sectionName;
		for (int i = 0; i < list.Length; i++)
		{
			bool flag = true;
			if (list == objects.enemies && !MonoSingleton<BestiaryData>.Instance.GetEnemy(list[i].enemyType))
			{
				flag = false;
			}
			if (flag)
			{
				spawnMenuSectionReference.buttonBackgroundImage.color = list[i].backgroundColor;
				spawnMenuSectionReference.buttonForegroundImage.sprite = list[i].gridIcon;
			}
			else
			{
				spawnMenuSectionReference.buttonBackgroundImage.color = Color.gray;
				spawnMenuSectionReference.buttonForegroundImage.sprite = lockedIcon;
			}
			Button button = Object.Instantiate(spawnMenuSectionReference.button, spawnMenuSectionReference.grid.transform, worldPositionStays: false);
			SpawnableObject spawnableObj = list[i];
			if (flag)
			{
				button.onClick.AddListener(() =>
				{
					SelectObject(spawnableObj);
				});
			}
		}
		spawnMenuSectionReference.button.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		if (MonoSingleton<InputManager>.Instance.InputSource.Pause.WasPerformedThisFrame)
		{
			base.gameObject.SetActive(value: false);
			MonoSingleton<OptionsManager>.Instance.UnFreeze();
		}
	}

	private void SelectObject(SpawnableObject obj)
	{
		arm.PreviewObject(obj);
	}
}
