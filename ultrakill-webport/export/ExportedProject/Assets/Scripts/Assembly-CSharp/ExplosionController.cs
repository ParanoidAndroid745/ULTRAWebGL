using UnityEngine;

public class ExplosionController : MonoBehaviour
{
	public GameObject[] toActivate;

	public string playerPref;

	private void Start()
	{
		if (PlayerPrefs.GetInt(playerPref, 0) != 0)
		{
			return;
		}
		GameObject[] array = toActivate;
		foreach (GameObject gameObject in array)
		{
			if ((bool)gameObject)
			{
				gameObject.SetActive(value: true);
			}
		}
	}
}
