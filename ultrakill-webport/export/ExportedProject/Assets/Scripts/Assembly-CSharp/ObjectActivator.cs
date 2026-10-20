using UnityEngine;

public class ObjectActivator : MonoBehaviour
{
	public bool oneTime;

	public bool disableOnExit;

	public bool dontActivateOnEnable;

	public bool reactivateOnEnable;

	private bool activated;

	public float delay;

	public ObjectActivationCheck obac;

	private bool nonCollider;

	public UltrakillEvent events;

	public bool forEnemies;

	public bool onlyIfPlayerIsAlive;

	private void Start()
	{
		if (GetComponent<Collider>() == null && !dontActivateOnEnable)
		{
			nonCollider = true;
			if ((!obac || obac.readyToActivate) && (!onlyIfPlayerIsAlive || !MonoSingleton<NewMovement>.Instance.dead))
			{
				Invoke("Activate", delay);
			}
		}
	}

	private void Update()
	{
		if (nonCollider && (!oneTime || !activated) && (bool)obac && obac.readyToActivate && (!onlyIfPlayerIsAlive || !MonoSingleton<NewMovement>.Instance.dead))
		{
			activated = true;
			Invoke("Activate", delay);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (((!forEnemies && (!oneTime || !activated) && other.gameObject.CompareTag("Player")) || (forEnemies && !activated && other.gameObject.CompareTag("Enemy"))) && (!obac || obac.readyToActivate) && (!onlyIfPlayerIsAlive || !MonoSingleton<NewMovement>.Instance.dead))
		{
			if (oneTime)
			{
				activated = true;
			}
			Invoke("Activate", delay);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (disableOnExit && ((!forEnemies && activated && other.gameObject.CompareTag("Player")) || (forEnemies && activated && other.gameObject.CompareTag("Enemy"))) && (!onlyIfPlayerIsAlive || !MonoSingleton<NewMovement>.Instance.dead))
		{
			Deactivate();
		}
	}

	public void Activate()
	{
		if (base.gameObject.activeSelf && (!onlyIfPlayerIsAlive || !MonoSingleton<NewMovement>.Instance.dead))
		{
			activated = true;
			events.Invoke();
		}
	}

	public void Deactivate()
	{
		if (!oneTime)
		{
			activated = false;
		}
		events.Revert();
		CancelInvoke("Activate");
	}

	private void OnDisable()
	{
		if (activated && nonCollider && disableOnExit && (!onlyIfPlayerIsAlive || !MonoSingleton<NewMovement>.Instance.dead))
		{
			Deactivate();
		}
		CancelInvoke("Activate");
	}

	private void OnEnable()
	{
		if ((!activated || reactivateOnEnable) && nonCollider && (!obac || obac.readyToActivate) && (!onlyIfPlayerIsAlive || !MonoSingleton<NewMovement>.Instance.dead))
		{
			Invoke("Activate", delay);
		}
	}
}
