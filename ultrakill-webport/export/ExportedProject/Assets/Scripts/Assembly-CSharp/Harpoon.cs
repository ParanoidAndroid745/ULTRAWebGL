using UnityEngine;

public class Harpoon : MonoBehaviour
{
	public bool magnet;

	public bool drill;

	private bool drilling;

	private float drillCooldown;

	private bool hit;

	private bool stopped;

	public float damage;

	private float damageLeft;

	private AudioSource aud;

	public AudioClip environmentHitSound;

	public AudioClip enemyHitSound;

	private Rigidbody rb;

	private EnemyIdentifierIdentifier target;

	public GameObject drillSound;

	public int drillHitsLeft;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		damageLeft = damage;
		Invoke("DestroyIfNotHit", 5f);
		Invoke("MasterDestroy", 30f);
		Invoke("SlowUpdate", 2f);
	}

	private void SlowUpdate()
	{
		if (base.transform.position.y < -300f || base.transform.position.x < -1000f || base.transform.position.x > 1000f || base.transform.position.z < -1000f || base.transform.position.z > 1000f)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			Invoke("SlowUpdate", 2f);
		}
	}

	private void Update()
	{
		if (!stopped)
		{
			base.transform.LookAt(base.transform.position + rb.velocity);
		}
		else if (drilling)
		{
			base.transform.Rotate(Vector3.forward, 14400f * Time.deltaTime);
		}
	}

	private void FixedUpdate()
	{
		if (!stopped || !drilling || !target)
		{
			return;
		}
		if (drillCooldown != 0f)
		{
			drillCooldown = Mathf.MoveTowards(drillCooldown, 0f, Time.deltaTime);
			return;
		}
		drillCooldown = 0.05f;
		if ((bool)target.eid)
		{
			target.eid.hitter = "drill";
			target.eid.DeliverDamage(target.gameObject, Vector3.zero, base.transform.position, 0.0625f, tryForExplode: false);
		}
		if (drillHitsLeft > 0)
		{
			drillHitsLeft--;
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void OnDestroy()
	{
		if (drill && (bool)target && (bool)target.eid)
		{
			target.eid.drilled = false;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		GoreZone componentInParent = other.GetComponentInParent<GoreZone>();
		if (!hit && (other.gameObject.layer == 10 || other.gameObject.layer == 11) && (other.gameObject.CompareTag("Head") || other.gameObject.CompareTag("Body") || other.gameObject.CompareTag("Limb") || other.gameObject.CompareTag("EndLimb")))
		{
			hit = true;
			target = other.gameObject.GetComponent<EnemyIdentifierIdentifier>();
			if (!target || !target.eid)
			{
				return;
			}
			EnemyIdentifier eid = target.eid;
			eid.hitter = "harpoon";
			eid.DeliverDamage(other.gameObject, Vector3.zero, base.transform.position, damageLeft, tryForExplode: false);
			if (drill)
			{
				eid.drilled = true;
			}
			damageLeft /= 3f;
			if (other.gameObject.layer == 10)
			{
				base.gameObject.AddComponent<FixedJoint>().connectedBody = other.gameObject.GetComponentInParent<Rigidbody>();
				if (componentInParent != null)
				{
					base.transform.SetParent(componentInParent.transform, worldPositionStays: true);
				}
			}
			else
			{
				rb.velocity = Vector3.zero;
				rb.useGravity = false;
				rb.constraints = RigidbodyConstraints.FreezeAll;
				base.transform.SetParent(other.transform, worldPositionStays: true);
			}
			if (!magnet && eid.dead && other.gameObject.layer == 10 && (!eid.machine || !eid.machine.specialDeath))
			{
				other.gameObject.transform.position = base.transform.position;
				GetComponent<Rigidbody>().AddForce(base.transform.forward, ForceMode.VelocityChange);
			}
			else
			{
				stopped = true;
				if (drill)
				{
					drilling = true;
					Object.Instantiate(drillSound, base.transform.position, base.transform.rotation).transform.SetParent(base.transform, worldPositionStays: true);
				}
				rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
				GetComponent<TrailRenderer>().emitting = false;
				TimeBomb component = GetComponent<TimeBomb>();
				if (component != null)
				{
					component.StartCountdown();
				}
				Magnet componentInChildren = GetComponentInChildren<Magnet>();
				if (componentInChildren != null)
				{
					componentInChildren.onEnemy = true;
					componentInChildren.ignoredEids.Add(eid);
				}
			}
			if (aud == null)
			{
				aud = GetComponent<AudioSource>();
			}
			aud.clip = enemyHitSound;
			aud.pitch = Random.Range(0.9f, 1.1f);
			aud.volume = 0.4f;
			aud.Play();
		}
		else
		{
			if (stopped || (other.gameObject.layer != 8 && other.gameObject.layer != 24))
			{
				return;
			}
			if (drill && !hit)
			{
				Object.Destroy(base.gameObject);
			}
			stopped = true;
			hit = true;
			rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
			rb.isKinematic = true;
			if (other.gameObject.CompareTag("Door") || other.gameObject.CompareTag("Moving"))
			{
				Rigidbody component2 = other.gameObject.GetComponent<Rigidbody>();
				if ((bool)component2)
				{
					base.gameObject.AddComponent<FixedJoint>().connectedBody = component2;
					rb.isKinematic = false;
				}
				hit = true;
				base.transform.SetParent(other.transform, worldPositionStays: true);
			}
			else if ((bool)componentInParent)
			{
				base.transform.SetParent(componentInParent.transform, worldPositionStays: true);
			}
			else
			{
				GoreZone[] array = Object.FindObjectsOfType<GoreZone>();
				if (array != null && array.Length != 0)
				{
					GoreZone goreZone = array[0];
					if (array.Length > 1)
					{
						for (int i = 1; i < array.Length; i++)
						{
							if (array[i].gameObject.activeInHierarchy && Vector3.Distance(goreZone.transform.position, base.transform.position) > Vector3.Distance(array[i].transform.position, base.transform.position))
							{
								goreZone = array[i];
							}
						}
					}
					base.transform.SetParent(goreZone.transform, worldPositionStays: true);
				}
			}
			if (aud == null)
			{
				aud = GetComponent<AudioSource>();
			}
			aud.clip = environmentHitSound;
			aud.pitch = Random.Range(0.9f, 1.1f);
			aud.volume = 0.4f;
			aud.Play();
			GetComponent<TrailRenderer>().emitting = false;
			TimeBomb component3 = GetComponent<TimeBomb>();
			if (component3 != null)
			{
				component3.StartCountdown();
			}
		}
	}

	private void DestroyIfNotHit()
	{
		if (!hit)
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void MasterDestroy()
	{
		Object.Destroy(base.gameObject);
	}
}
