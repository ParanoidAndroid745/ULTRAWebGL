using UnityEngine;
using UnityEngine.Events;

public class AltPickUp : MonoBehaviour
{
	public string pPref;

	public UnityEvent onPickUp;

	private void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.tag == "Player")
		{
			GotActivated();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.tag == "Player")
		{
			GotActivated();
		}
	}

	private void GotActivated()
	{
		GameProgressSaver.AddGear(pPref + "alt");
		PlayerPrefs.SetInt(pPref + "0", 2);
		MonoSingleton<GunSetter>.Instance.ResetWeapons();
		MonoSingleton<GunSetter>.Instance.ForceWeapon(pPref + "0");
		onPickUp?.Invoke();
		Object.Destroy(base.gameObject);
	}
}
