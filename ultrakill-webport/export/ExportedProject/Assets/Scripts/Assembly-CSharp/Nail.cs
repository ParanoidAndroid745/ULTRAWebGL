using System.Collections.Generic;
using UnityEngine;

public class Nail : MonoBehaviour
{
	[HideInInspector]
	public bool hit;

	public float damage;

	private AudioSource aud;

	private Rigidbody rb;

	public AudioClip environmentHitSound;

	public AudioClip enemyHitSound;

	public Material zapMaterial;

	public GameObject zapParticle;

	private bool zapped;

	public bool fodderDamageBoost;

	public string weaponType;

	public bool heated;

	[HideInInspector]
	public List<Magnet> magnets = new List<Magnet>();

	private bool launched;

	[HideInInspector]
	public NailBurstController nbc;

	private void Start()
	{
		aud = GetComponent<AudioSource>();
		rb = GetComponent<Rigidbody>();
		Invoke("RemoveTime", 5f);
		Invoke("MasterRemoveTime", 25f);
		Invoke("SlowUpdate", 2f);
	}

	private void OnDestroy()
	{
		if (zapped)
		{
			Object.Instantiate(zapParticle, base.transform.position, base.transform.rotation);
		}
	}

	private void SlowUpdate()
	{
		if (base.transform.position.y < -300f || base.transform.position.x < -1000f || base.transform.position.x > 1000f || base.transform.position.z < -1000f || base.transform.position.z > 1000f)
		{
			RemoveTime();
		}
		else
		{
			Invoke("SlowUpdate", 2f);
		}
	}

	private void Update()
	{
		if (!hit)
		{
			if (!rb)
			{
				rb = GetComponent<Rigidbody>();
			}
			if ((bool)rb)
			{
				base.transform.LookAt(base.transform.position + rb.velocity * -1f);
			}
		}
	}

	private void OnCollisionEnter(Collision other)
	{
		if (!hit && (other.gameObject.layer == 10 || other.gameObject.layer == 11) && (other.gameObject.tag == "Head" || other.gameObject.tag == "Body" || other.gameObject.tag == "Limb" || other.gameObject.tag == "EndLimb" || other.gameObject.tag == "Enemy"))
		{
			hit = true;
			HitEnemy(other.transform);
		}
		else
		{
			if (hit || magnets.Count != 0 || (other.gameObject.layer != 8 && other.gameObject.layer != 24))
			{
				return;
			}
			hit = true;
			CancelInvoke("RemoveTime");
			Invoke("RemoveTime", 1f);
			if (aud == null)
			{
				aud = GetComponent<AudioSource>();
			}
			aud.clip = environmentHitSound;
			aud.pitch = Random.Range(0.9f, 1.1f);
			aud.volume = 0.2f;
			aud.Play();
			Breakable component = other.gameObject.GetComponent<Breakable>();
			if (component != null && (component.weak || heated) && !component.precisionOnly)
			{
				component.Break();
			}
			if (heated)
			{
				Flammable componentInChildren = other.gameObject.GetComponentInChildren<Flammable>();
				if (componentInChildren != null)
				{
					componentInChildren.Burn(2f);
				}
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!hit && (other.gameObject.layer == 10 || other.gameObject.layer == 11) && (other.gameObject.tag == "Head" || other.gameObject.tag == "Body" || other.gameObject.tag == "Limb" || other.gameObject.tag == "EndLimb" || other.gameObject.tag == "Enemy"))
		{
			hit = true;
			HitEnemy(other.transform);
		}
	}

	private void HitEnemy(Transform other)
	{
		EnemyIdentifierIdentifier component = other.gameObject.GetComponent<EnemyIdentifierIdentifier>();
		if ((bool)component && (bool)component.eid)
		{
			component.eid.hitter = "nail";
			if (!component.eid.hitterWeapons.Contains(weaponType))
			{
				component.eid.hitterWeapons.Add(weaponType);
			}
			bool flag = false;
			if (magnets.Count > 0)
			{
				foreach (Magnet magnet in magnets)
				{
					if (magnet.ignoredEids.Contains(component.eid))
					{
						flag = true;
						damage *= 0.65f;
						break;
					}
				}
			}
			bool dead = component.eid.dead;
			if (fodderDamageBoost)
			{
				damage *= GetFodderDamageMultiplier(component.eid.enemyType);
			}
			if (!nbc)
			{
				component.eid.DeliverDamage(other.gameObject, (other.transform.position - base.transform.position).normalized * 3000f, base.transform.position, damage, tryForExplode: false);
			}
			else if (!nbc.damagedEnemies.Contains(component.eid))
			{
				component.eid.DeliverDamage(other.gameObject, (other.transform.position - base.transform.position).normalized * 3000f, base.transform.position, damage * (float)(nbc.nails.Count / 2), tryForExplode: true);
				nbc.damagedEnemies.Add(component.eid);
			}
			if (!dead && component.eid.dead && !flag && magnets.Count > 0)
			{
				if (magnets.Count > 1)
				{
					MonoSingleton<StyleHUD>.Instance.AddPoints(Mathf.RoundToInt(120f), "<color=cyan>BIPOLAR</color>");
				}
				else
				{
					MonoSingleton<StyleHUD>.Instance.AddPoints(Mathf.RoundToInt(60f), "<color=cyan>ATTRAPTOR</color>");
				}
			}
			else if (launched)
			{
				if (!dead && component.eid.dead)
				{
					MonoSingleton<StyleHUD>.Instance.AddPoints(Mathf.RoundToInt(120f), "NAILBOMBED");
				}
				else if (!component.eid.dead)
				{
					MonoSingleton<StyleHUD>.Instance.AddPoints(Mathf.RoundToInt(10f), "<color=grey>NAILBOMBED</color>");
				}
			}
			component.eid.nailsAmount++;
			component.eid.nails.Add(this);
			if (heated)
			{
				Flammable componentInChildren = component.eid.GetComponentInChildren<Flammable>();
				if (componentInChildren != null)
				{
					componentInChildren.Burn(2f, componentInChildren.burning);
				}
			}
		}
		if (aud == null)
		{
			aud = GetComponent<AudioSource>();
		}
		aud.clip = enemyHitSound;
		aud.pitch = Random.Range(0.9f, 1.1f);
		aud.volume = 0.2f;
		aud.Play();
		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
		}
		GetComponent<Collider>().enabled = false;
		rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
		rb.isKinematic = true;
		Object.Destroy(rb);
		base.transform.position += base.transform.forward * -0.5f;
		base.transform.SetParent(other.transform, worldPositionStays: true);
		GetComponent<TrailRenderer>().enabled = false;
		CancelInvoke("RemoveTime");
	}

	public void MagnetCaught(Magnet mag)
	{
		if (!hit)
		{
			CancelInvoke("RemoveTime");
		}
		launched = false;
		if (!magnets.Contains(mag))
		{
			magnets.Add(mag);
		}
		if ((bool)nbc)
		{
			nbc.nails.Remove(this);
			nbc = null;
		}
	}

	public void MagnetRelease(Magnet mag)
	{
		if (!hit)
		{
			Invoke("RemoveTime", 5f);
		}
		if (magnets.Contains(mag))
		{
			magnets.Remove(mag);
			if (magnets.Count == 0)
			{
				GetComponent<SphereCollider>().enabled = true;
				launched = true;
			}
		}
	}

	public void Zap()
	{
		MeshRenderer component = GetComponent<MeshRenderer>();
		if ((bool)component)
		{
			component.material = zapMaterial;
		}
		zapped = true;
	}

	private void RemoveTime()
	{
		Object.Destroy(base.gameObject);
	}

	private void MasterRemoveTime()
	{
		RemoveTime();
	}

	private float GetFodderDamageMultiplier(EnemyType et)
	{
		switch (et)
		{
		case EnemyType.Filth:
			return 2f;
		case EnemyType.Schism:
			return 1.5f;
		case EnemyType.Soldier:
			return 1.5f;
		case EnemyType.Stalker:
			return 1.5f;
		case EnemyType.Stray:
			return 2f;
		default:
			return 1f;
		}
	}
}
