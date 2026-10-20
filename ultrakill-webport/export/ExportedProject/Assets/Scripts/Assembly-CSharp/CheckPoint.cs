using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CheckPoint : MonoBehaviour
{
	[HideInInspector]
	public StatsManager sm;

	private bool activated;

	private bool notFirstTime;

	public GameObject toActivate;

	public GameObject[] rooms;

	public List<GameObject> roomsToInherit = new List<GameObject>();

	private List<string> inheritNames = new List<string>();

	private List<Transform> inheritParents = new List<Transform>();

	private GameObject[] roomsPriv;

	[HideInInspector]
	public List<GameObject> defaultRooms = new List<GameObject>();

	public Door[] doorsToUnlock;

	[HideInInspector]
	public List<GameObject> newRooms = new List<GameObject>();

	private int i;

	private GameObject player;

	private NewMovement nm;

	private float tempRot;

	public GameObject graphic;

	public GameObject activateEffect;

	[HideInInspector]
	public int restartKills;

	[HideInInspector]
	public int stylePoints;

	private StyleHUD shud;

	public bool multiUse;

	public bool dontAutoReset;

	[HideInInspector]
	public List<int> succesfulHitters = new List<int>();

	[Space]
	public UnityEvent onRestart;

	private void Start()
	{
		GameObject[] array = rooms;
		foreach (GameObject item in array)
		{
			defaultRooms.Add(item);
		}
		for (int j = 0; j < defaultRooms.Count; j++)
		{
			defaultRooms[j].GetComponent<GoreZone>().checkpoint = this;
			newRooms.Add(Object.Instantiate(defaultRooms[j], defaultRooms[j].transform.position, defaultRooms[j].transform.rotation, defaultRooms[j].transform.parent));
			defaultRooms[j].gameObject.SetActive(value: false);
			newRooms[j].gameObject.SetActive(value: true);
			defaultRooms[j].transform.position = new Vector3(defaultRooms[j].transform.position.x + 10000f, defaultRooms[j].transform.position.y, defaultRooms[j].transform.position.z);
		}
		player = MonoSingleton<NewMovement>.Instance.gameObject;
		sm = MonoSingleton<StatsManager>.Instance;
		if (shud == null)
		{
			shud = MonoSingleton<StyleHUD>.Instance;
		}
		for (int k = 0; k < roomsToInherit.Count; k++)
		{
			inheritNames.Add(roomsToInherit[k].name);
			inheritParents.Add(roomsToInherit[k].transform.parent);
		}
	}

	private void Update()
	{
		if (multiUse && activated && !dontAutoReset && Vector3.Distance(MonoSingleton<NewMovement>.Instance.transform.position, base.transform.position) > 15f)
		{
			ReactivateCheckpoint();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!activated && other.gameObject.tag == "Player")
		{
			ActivateCheckPoint();
		}
	}

	public void ActivateCheckPoint()
	{
		sm = MonoSingleton<StatsManager>.Instance;
		sm.currentCheckPoint = this;
		activated = true;
		if ((bool)activateEffect)
		{
			Object.Instantiate(activateEffect, MonoSingleton<NewMovement>.Instance.transform.position, Quaternion.identity);
		}
		if (!multiUse || !notFirstTime)
		{
			MonoSingleton<NewMovement>.Instance.sameCheckpointRestarts = 0;
		}
		if ((bool)graphic)
		{
			if (multiUse)
			{
				graphic.SetActive(value: false);
			}
			else
			{
				Object.Destroy(graphic);
			}
		}
		stylePoints = sm.stylePoints;
		if (notFirstTime)
		{
			defaultRooms.Clear();
			newRooms.Clear();
			GameObject[] array = rooms;
			foreach (GameObject item in array)
			{
				roomsToInherit.Add(item);
			}
		}
		if (shud == null)
		{
			shud = MonoSingleton<StyleHUD>.Instance;
		}
		if (roomsToInherit.Count == 0)
		{
			return;
		}
		for (int j = 0; j < roomsToInherit.Count; j++)
		{
			string text = inheritNames[j];
			text = text.Replace("(Clone)", "");
			GameObject gameObject = null;
			for (int num = inheritParents[j].childCount - 1; num >= 0; num--)
			{
				GameObject gameObject2 = inheritParents[j].GetChild(num).gameObject;
				if (gameObject2.name.Replace("(Clone)", "") == text)
				{
					if (gameObject == null)
					{
						gameObject = gameObject2;
					}
					else
					{
						Object.Destroy(gameObject2);
					}
				}
			}
			InheritRoom(gameObject);
		}
	}

	public void OnRespawn()
	{
		if (player == null)
		{
			player = MonoSingleton<NewMovement>.Instance.gameObject;
		}
		if (player.GetComponentInParent<GoreZone>() != null)
		{
			player.transform.parent = null;
		}
		player.transform.position = Vector3.one * -1000f;
		this.i = 0;
		if ((bool)DebugArm.debugZone)
		{
			Object.Destroy(DebugArm.debugZone.gameObject);
		}
		if (!activated && multiUse)
		{
			activated = true;
			if ((bool)graphic)
			{
				graphic.SetActive(value: false);
			}
		}
		sm.kills -= restartKills;
		restartKills = 0;
		sm.stylePoints = stylePoints;
		if (succesfulHitters.Count > 0)
		{
			KillHitterCache instance = MonoSingleton<KillHitterCache>.Instance;
			if ((bool)instance && !instance.ignoreRestarts)
			{
				foreach (int succesfulHitter in succesfulHitters)
				{
					instance.RemoveId(succesfulHitter);
				}
			}
		}
		if (shud == null)
		{
			shud = MonoSingleton<StyleHUD>.Instance;
		}
		shud.ComboOver();
		if (doorsToUnlock.Length != 0)
		{
			Door[] array = doorsToUnlock;
			foreach (Door door in array)
			{
				if (door.locked)
				{
					door.Unlock();
				}
				if (door.startOpen)
				{
					door.Open();
				}
			}
		}
		Projectile[] array2 = Object.FindObjectsOfType<Projectile>();
		if (array2 != null && array2.Length != 0)
		{
			Projectile[] array3 = array2;
			foreach (Projectile projectile in array3)
			{
				if (projectile.gameObject.activeInHierarchy)
				{
					Object.Destroy(projectile.gameObject);
				}
			}
		}
		VirtueInsignia[] array4 = Object.FindObjectsOfType<VirtueInsignia>();
		if (array4 != null && array4.Length != 0)
		{
			VirtueInsignia[] array5 = array4;
			foreach (VirtueInsignia virtueInsignia in array5)
			{
				if (virtueInsignia.gameObject.activeInHierarchy)
				{
					Object.Destroy(virtueInsignia.gameObject);
				}
			}
		}
		Explosion[] array6 = Object.FindObjectsOfType<Explosion>();
		if (array6 != null && array6.Length != 0)
		{
			Explosion[] array7 = array6;
			foreach (Explosion explosion in array7)
			{
				if (explosion.gameObject.activeInHierarchy)
				{
					Object.Destroy(explosion.gameObject);
				}
			}
		}
		Harpoon[] array8 = Object.FindObjectsOfType<Harpoon>();
		if (array8 != null && array8.Length != 0)
		{
			Harpoon[] array9 = array8;
			foreach (Harpoon harpoon in array9)
			{
				if (harpoon.gameObject.activeInHierarchy)
				{
					TimeBomb componentInChildren = harpoon.GetComponentInChildren<TimeBomb>();
					if ((bool)componentInChildren)
					{
						componentInChildren.dontExplode = true;
					}
					Object.Destroy(harpoon.gameObject);
				}
			}
		}
		if (newRooms.Count > 0)
		{
			ResetRoom();
		}
	}

	public void ResetRoom()
	{
		Vector3 position = newRooms[i].transform.position;
		Object.Destroy(newRooms[i]);
		newRooms[i] = Object.Instantiate(defaultRooms[i], position, defaultRooms[i].transform.rotation, defaultRooms[i].transform.parent);
		newRooms[i].SetActive(value: true);
		if (i + 1 < defaultRooms.Count)
		{
			i++;
			ResetRoom();
			return;
		}
		toActivate?.SetActive(value: true);
		onRestart?.Invoke();
		player.transform.position = base.transform.position + base.transform.right * 0.1f + Vector3.up * 1.25f;
		player.GetComponent<Rigidbody>().velocity = Vector3.zero;
		if (nm == null)
		{
			nm = player.GetComponent<NewMovement>();
		}
		float num = base.transform.rotation.eulerAngles.y + 0.1f;
		if ((bool)player && (bool)player.transform.parent && player.transform.parent.gameObject.tag == "Moving")
		{
			num -= player.transform.parent.rotation.eulerAngles.y;
		}
		nm.cc.ResetCamera(num, instant: true);
		if (!nm.enabled)
		{
			nm.enabled = true;
		}
		nm.Respawn();
		nm.GetHealth(0, silent: true);
		nm.cc.StopShake();
	}

	public void UpdateRooms()
	{
		Vector3 position = newRooms[i].transform.position;
		Object.Destroy(newRooms[i]);
		newRooms[i] = Object.Instantiate(defaultRooms[i], position, defaultRooms[i].transform.rotation, defaultRooms[i].transform.parent);
		newRooms[i].SetActive(value: true);
		if (i + 1 < defaultRooms.Count)
		{
			i++;
			UpdateRooms();
		}
		else
		{
			i = 0;
		}
	}

	public void InheritRoom(GameObject targetRoom)
	{
		new List<GameObject>();
		new List<GameObject>();
		defaultRooms.Add(targetRoom);
		int index = defaultRooms.IndexOf(targetRoom);
		defaultRooms[index].GetComponent<GoreZone>().checkpoint = this;
		RemoveOnTime[] componentsInChildren = defaultRooms[index].GetComponentsInChildren<RemoveOnTime>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].gameObject.SetActive(value: false);
		}
		newRooms.Add(Object.Instantiate(defaultRooms[index], defaultRooms[index].transform.position, defaultRooms[index].transform.rotation, defaultRooms[index].transform.parent));
		defaultRooms[index].gameObject.SetActive(value: false);
		newRooms[index].gameObject.SetActive(value: true);
		defaultRooms[index].transform.position = new Vector3(defaultRooms[index].transform.position.x + 10000f, defaultRooms[index].transform.position.y, defaultRooms[index].transform.position.z);
	}

	public void ReactivateCheckpoint()
	{
		if (multiUse)
		{
			activated = false;
			graphic.SetActive(value: true);
			notFirstTime = true;
			if (graphic.TryGetComponent<ScaleTransform>(out var _))
			{
				graphic.transform.localScale = new Vector3(graphic.transform.localScale.x, 0f, graphic.transform.localScale.z);
			}
			if (graphic.TryGetComponent<AudioSource>(out var component2))
			{
				component2.Play();
			}
		}
	}

	public void ApplyCurrentStyleAndKills()
	{
		ApplyCurrentKills();
		ApplyCurrentStyle();
	}

	public void ApplyCurrentKills()
	{
		restartKills = 0;
	}

	public void ApplyCurrentStyle()
	{
		stylePoints = sm.stylePoints;
	}
}
