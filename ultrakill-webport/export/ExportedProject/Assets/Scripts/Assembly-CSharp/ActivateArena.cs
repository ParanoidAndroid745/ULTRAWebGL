using UnityEngine;

[DefaultExecutionOrder(500)]
public class ActivateArena : MonoBehaviour
{
	public bool onlyWave;

	private bool activated;

	public Door[] doors;

	public GameObject[] enemies;

	private int currentEnemy;

	public bool forEnemy;

	public int waitForStatus;

	public bool activateOnEnable;

	private ArenaStatus astat;

	private void OnEnable()
	{
		if (activated || !activateOnEnable || CheatsController.IgnoreArenaTriggers)
		{
			return;
		}
		if (waitForStatus > 0)
		{
			if (astat == null)
			{
				astat = GetComponentInParent<ArenaStatus>();
			}
			if (astat == null || astat.currentStatus < waitForStatus)
			{
				return;
			}
		}
		Activate();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (CheatsController.IgnoreArenaTriggers)
		{
			return;
		}
		if (waitForStatus > 0)
		{
			if (astat == null)
			{
				astat = GetComponentInParent<ArenaStatus>();
			}
			if (astat == null || astat.currentStatus < waitForStatus)
			{
				return;
			}
		}
		if ((!forEnemy && other.gameObject.tag == "Player" && !activated) || (forEnemy && other.gameObject.tag == "Enemy" && !activated))
		{
			Activate();
		}
	}

	public void Activate()
	{
		if (CheatsController.IgnoreArenaTriggers)
		{
			return;
		}
		activated = true;
		if (!onlyWave && !forEnemy)
		{
			MonoSingleton<MusicManager>.Instance.ArenaMusicStart();
		}
		if (doors.Length != 0)
		{
			Door[] array = doors;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Lock();
			}
			if (enemies.Length != 0)
			{
				Invoke("SpawnEnemy", 1f);
			}
		}
		else if (enemies.Length != 0)
		{
			SpawnEnemy();
		}
		else
		{
			Object.Destroy(this);
		}
	}

	private void SpawnEnemy()
	{
		float time = 0.1f;
		if (enemies[currentEnemy] != null)
		{
			if (enemies[currentEnemy].activeSelf)
			{
				time = 0f;
			}
			else
			{
				enemies[currentEnemy].SetActive(value: true);
			}
		}
		currentEnemy++;
		if (currentEnemy < enemies.Length)
		{
			Invoke("SpawnEnemy", time);
		}
		else
		{
			Object.Destroy(this);
		}
	}
}
