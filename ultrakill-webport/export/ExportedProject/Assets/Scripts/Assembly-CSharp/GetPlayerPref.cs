using UnityEngine;
using UnityEngine.Events;

public class GetPlayerPref : MonoBehaviour
{
	public string pref;

	public int valueToCheckFor;

	public UnityEvent onCheckSuccess;

	private void Start()
	{
		if (PlayerPrefs.GetInt(pref, 0) == valueToCheckFor)
		{
			onCheckSuccess?.Invoke();
		}
	}
}
