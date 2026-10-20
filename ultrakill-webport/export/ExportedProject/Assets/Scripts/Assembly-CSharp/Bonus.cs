using UnityEngine;

public class Bonus : MonoBehaviour
{
	private Vector3 cRotation;

	public GameObject breakEffect;

	private bool activated;

	public bool ghost;

	public bool tutorial;

	public bool superCharge;

	public bool dontReplaceWithGhost;

	private void Start()
	{
		cRotation = new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5));
	}

	private void Update()
	{
		base.transform.Rotate(cRotation * Time.deltaTime * 5f);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!(other.gameObject.tag == "Player") || activated)
		{
			return;
		}
		if (!ghost)
		{
			activated = true;
			other.gameObject.GetComponentInChildren<Punch>().ParryFlash();
			StyleHUD instance = MonoSingleton<StyleHUD>.Instance;
			StatsManager instance2 = MonoSingleton<StatsManager>.Instance;
			Object.Instantiate(breakEffect, base.transform.position, Quaternion.identity);
			instance.AddPoints(0, "<color=cyan>SECRET</color>");
			instance2.secrets++;
			instance2.SecretFound(base.gameObject);
		}
		else
		{
			if (tutorial)
			{
				other.gameObject.GetComponentInChildren<Punch>().ParryFlash();
			}
			Object.Instantiate(breakEffect, base.transform.position, Quaternion.identity);
			Object.Destroy(base.gameObject);
		}
		if (superCharge)
		{
			other.gameObject.GetComponent<NewMovement>().SuperCharge();
			if (PlayerPrefs.GetInt("SupChar", 0) == 0)
			{
				PlayerPrefs.SetInt("SupChar", 1);
				MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("<color=red>RED SOUL ORBS</color> give <color=lime>200 HEALTH</color>. \nOverheal cannot be regained with blood.", "", "", 1);
			}
		}
	}
}
