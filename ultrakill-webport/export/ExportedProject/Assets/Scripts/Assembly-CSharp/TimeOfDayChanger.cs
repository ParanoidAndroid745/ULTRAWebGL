using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TimeOfDayChanger : MonoBehaviour
{
	private bool allOff;

	private bool allDone = true;

	public Light[] oldLights;

	public Light[] newLights;

	private List<float> origIntensities = new List<float>();

	public Material oldWalls;

	public Material oldSky;

	public Material newWalls;

	public Material newSky;

	public bool toBattleMusic;

	public bool toBossMusic;

	public bool musicWaitsUntilChange;

	public Material newSkybox;

	private Color skyboxColor;

	public Color fogColor;

	private Material oldSkyboxTemp;

	private Material newSkyboxTemp;

	public SpriteRenderer sunSprite;

	public Color sunSpriteColor;

	public UnityEvent onMaterialChange;

	private void Awake()
	{
		if (newLights.Length != 0)
		{
			for (int i = 0; i < newLights.Length; i++)
			{
				origIntensities.Add(newLights[i].intensity);
				newLights[i].intensity = 0f;
				newLights[i].enabled = true;
			}
		}
	}

	private void OnEnable()
	{
		if ((bool)newSkybox)
		{
			oldSkyboxTemp = new Material(RenderSettings.skybox);
			RenderSettings.skybox = oldSkyboxTemp;
		}
		allDone = false;
		if (!musicWaitsUntilChange)
		{
			if (toBattleMusic)
			{
				MonoSingleton<MusicManager>.Instance.ArenaMusicStart();
			}
			else if (toBossMusic)
			{
				MonoSingleton<MusicManager>.Instance.PlayBossMusic();
			}
		}
	}

	private void ChangeMaterials()
	{
		MeshRenderer[] array = Object.FindObjectsOfType<MeshRenderer>();
		foreach (MeshRenderer meshRenderer in array)
		{
			if (meshRenderer.sharedMaterial == oldWalls)
			{
				meshRenderer.material = newWalls;
			}
			else if (meshRenderer.sharedMaterial == oldSky)
			{
				meshRenderer.material = newSky;
			}
		}
		if (musicWaitsUntilChange)
		{
			if (toBattleMusic)
			{
				MonoSingleton<MusicManager>.Instance.ArenaMusicStart();
			}
			else if (toBossMusic)
			{
				MonoSingleton<MusicManager>.Instance.PlayBossMusic();
			}
		}
		if ((bool)newSkybox)
		{
			newSkyboxTemp = new Material(newSkybox);
			RenderSettings.skybox = newSkyboxTemp;
			skyboxColor = RenderSettings.skybox.GetColor("_Tint");
			RenderSettings.skybox.SetColor("_Tint", Color.black);
		}
		onMaterialChange?.Invoke();
	}

	private void Update()
	{
		if (allDone)
		{
			return;
		}
		RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, fogColor, Time.deltaTime);
		if ((bool)sunSprite)
		{
			sunSprite.color = Color.Lerp(sunSprite.color, sunSpriteColor, Time.deltaTime);
		}
		if (!allOff)
		{
			bool flag = true;
			Light[] array = oldLights;
			foreach (Light light in array)
			{
				if (light.intensity != 0f)
				{
					light.intensity = Mathf.MoveTowards(light.intensity, 0f, Time.deltaTime);
					if (light.intensity != 0f)
					{
						flag = false;
					}
				}
			}
			if ((bool)newSkybox)
			{
				RenderSettings.skybox.SetColor("_Tint", Color.Lerp(RenderSettings.skybox.GetColor("_Tint"), Color.black, Time.deltaTime));
			}
			if (flag)
			{
				allOff = true;
				ChangeMaterials();
			}
		}
		else if (newLights.Length != 0)
		{
			bool flag2 = true;
			for (int j = 0; j < newLights.Length; j++)
			{
				if (newLights[j].intensity != origIntensities[j])
				{
					newLights[j].intensity = Mathf.MoveTowards(newLights[j].intensity, origIntensities[j], Time.deltaTime);
					if (newLights[j].intensity != origIntensities[j])
					{
						flag2 = false;
					}
				}
			}
			RenderSettings.skybox.SetColor("_Tint", Color.Lerp(RenderSettings.skybox.GetColor("_Tint"), skyboxColor, Time.deltaTime));
			if (flag2)
			{
				RenderSettings.skybox.SetColor("_Tint", skyboxColor);
				RenderSettings.fogColor = fogColor;
				allDone = true;
			}
		}
		else
		{
			RenderSettings.skybox.SetColor("_Tint", skyboxColor);
			RenderSettings.fogColor = fogColor;
			allDone = true;
		}
	}
}
