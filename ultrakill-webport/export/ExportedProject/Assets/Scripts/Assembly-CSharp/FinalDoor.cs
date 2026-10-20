using UnityEngine;

public class FinalDoor : MonoBehaviour
{
	public Door[] doors;

	public GameObject doorLight;

	public bool startOpen;

	private bool aboutToOpen;

	private bool opened;

	public Material[] offMaterials;

	public Material[] onMaterials;

	public bool levelNameOnOpen;

	private MeshRenderer[] allRenderers;

	private void Start()
	{
		if (doorLight == null)
		{
			doorLight = GetComponentInChildren<Light>().gameObject;
		}
		if (!aboutToOpen)
		{
			doorLight.SetActive(value: false);
		}
		if (startOpen)
		{
			Open();
		}
		allRenderers = GetComponentsInChildren<MeshRenderer>();
	}

	public void Open()
	{
		aboutToOpen = true;
		MonoSingleton<MusicManager>.Instance.ArenaMusicEnd();
		Invoke("OpenDoors", 1f);
		GetComponent<AudioSource>().Play();
		doorLight.SetActive(value: true);
		if (onMaterials.Length != 0)
		{
			if (allRenderers == null || allRenderers.Length == 0)
			{
				allRenderers = GetComponentsInChildren<MeshRenderer>();
			}
			MeshRenderer[] array = allRenderers;
			foreach (MeshRenderer meshRenderer in array)
			{
				int material = GetMaterial(meshRenderer);
				meshRenderer.sharedMaterial = onMaterials[material];
			}
		}
	}

	private int GetMaterial(MeshRenderer mr)
	{
		for (int i = 0; i < offMaterials.Length; i++)
		{
			if (mr.sharedMaterial == offMaterials[i])
			{
				return i;
			}
		}
		return offMaterials.Length - 1;
	}

	public void OpenDoors()
	{
		if (!opened)
		{
			opened = true;
			Door[] array = doors;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Open();
			}
			if (levelNameOnOpen)
			{
				Invoke("LevelNameGo", 1f);
			}
		}
	}

	private void LevelNameGo()
	{
		MonoSingleton<LevelNamePopup>.Instance.NameAppear();
	}
}
