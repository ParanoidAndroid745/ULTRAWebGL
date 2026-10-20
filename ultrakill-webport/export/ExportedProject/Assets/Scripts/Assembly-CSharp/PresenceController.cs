using UnityEngine;
using UnityEngine.SceneManagement;

public class PresenceController : MonoBehaviour
{
	public static PresenceController Instance;

	public string[] diffNames;

	private void Start()
	{
		if ((bool)Instance)
		{
			Object.DestroyImmediate(base.gameObject);
			return;
		}
		Instance = this;
		base.transform.SetParent(null);
		Object.DontDestroyOnLoad(base.gameObject);
		SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
		SceneManagerOnsceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
	}

	public static void UpdateCyberGrindWave(int wave)
	{
		if ((bool)Instance)
		{
			DiscordController.UpdateWave(wave);
			SteamController.Instance.UpdateWave(wave);
		}
	}

	private void SceneManagerOnsceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (mode != LoadSceneMode.Additive)
		{
			DiscordController.Instance.FetchSceneActivity(scene.name);
			SteamController.Instance.FetchSceneActivity(scene.name);
		}
	}
}
