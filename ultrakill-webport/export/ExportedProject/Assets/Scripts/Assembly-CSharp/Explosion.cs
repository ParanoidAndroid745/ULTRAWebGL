using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
	public bool enemy;

	public bool harmless;

	public bool lowQuality;

	private CameraController cc;

	private Light light;

	private MeshRenderer mr;

	private Color materialColor;

	private bool fading;

	public float speed;

	public float maxSize;

	private LayerMask lmask;

	public int damage;

	public float enemyDamageMultiplier;

	public GameObject explosionChunk;

	public bool ignite;

	public bool friendlyFire;

	private List<Collider> hitColliders = new List<Collider>();

	public string hitterWeapon;

	public bool halved;

	private SphereCollider scol;

	public AffectedSubjects canHit;

	private bool hasHitPlayer;

	public List<EnemyType> toIgnore;

	private void Start()
	{
		cc = MonoSingleton<CameraController>.Instance;
		float num = Vector3.Distance(base.transform.position, cc.transform.position);
		float num2 = 1f;
		if (damage == 0)
		{
			num2 = 0.25f;
		}
		if (num < 3f * maxSize)
		{
			cc.CameraShake(1.5f * num2);
		}
		else if (num < 85f)
		{
			cc.CameraShake((1.5f - (num - 20f) / 65f * 1.5f) / 6f * maxSize * num2);
		}
		scol = GetComponent<SphereCollider>();
		if ((bool)scol)
		{
			scol.enabled = true;
		}
		if (speed == 0f)
		{
			speed = 1f;
		}
		if (!lowQuality && PlayerPrefs.GetInt("SimExp", 0) == 1)
		{
			lowQuality = true;
		}
		if (!lowQuality)
		{
			light = GetComponentInChildren<Light>();
			light.enabled = true;
			if (explosionChunk != null)
			{
				for (int i = 0; i < Random.Range(24, 30); i++)
				{
					GameObject obj = Object.Instantiate(explosionChunk, base.transform.position, Random.rotation);
					Vector3 vector = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
					obj.GetComponent<Rigidbody>().AddForce(vector * 1000f);
				}
			}
		}
		lmask = (int)lmask | 0x100;
		lmask = (int)lmask | 0x800;
		lmask = (int)lmask | 0x1000000;
		lmask = (int)lmask | 0x4000000;
	}

	private void FixedUpdate()
	{
		base.transform.localScale += Vector3.one * 0.05f * speed;
		if (light != null)
		{
			light.range += 0.05f * speed;
		}
		if (!fading && base.transform.lossyScale.x * scol.radius > maxSize)
		{
			Fade();
		}
		if (!halved && base.transform.lossyScale.x * scol.radius > maxSize / 2f)
		{
			halved = true;
			damage = Mathf.RoundToInt((float)damage / 1.5f);
		}
		if (fading)
		{
			materialColor.a -= 0.02f;
			if (light != null)
			{
				light.intensity -= 0.65f;
			}
			mr.material.SetColor("_Color", materialColor);
			if (materialColor.a <= 0f)
			{
				Object.Destroy(base.gameObject);
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!harmless && !hitColliders.Contains(other))
		{
			bool flag = false;
			if (Physics.Raycast(base.transform.position + (other.transform.position - base.transform.position).normalized * 0.1f, other.transform.position - base.transform.position, out var hitInfo, Vector3.Distance(base.transform.position + (other.transform.position - base.transform.position).normalized * 0.1f, other.transform.position), lmask, QueryTriggerInteraction.Ignore))
			{
				flag = true;
			}
			Breakable component6;
			Glass component7;
			Flammable component8;
			if (!flag || other.isTrigger || hitInfo.transform.gameObject == other.gameObject || other.gameObject.layer == 11)
			{
				if (enemy && Physics.Raycast(other.transform.position, base.transform.position - other.transform.position, Vector3.Distance(base.transform.position, other.transform.position) - 0.1f, lmask, QueryTriggerInteraction.Ignore))
				{
					return;
				}
				if (flag && other.gameObject.layer == 11)
				{
					Collider[] componentsInChildren = hitInfo.transform.GetComponentsInChildren<Collider>();
					bool flag2 = false;
					Collider[] array = componentsInChildren;
					for (int i = 0; i < array.Length; i++)
					{
						if (array[i].transform == hitInfo.transform)
						{
							flag2 = true;
						}
					}
					if (!flag2)
					{
						return;
					}
				}
				Breakable component3;
				Glass component4;
				Flammable component5;
				if (other.gameObject.tag == "Player" && !hasHitPlayer)
				{
					hasHitPlayer = true;
					hitColliders.Add(other);
					if (canHit != AffectedSubjects.EnemiesOnly)
					{
						if (damage > 0)
						{
							if (enemy)
							{
								MonoSingleton<NewMovement>.Instance.GetHurt(damage, invincible: true, 1f, explosion: true);
							}
							else
							{
								MonoSingleton<NewMovement>.Instance.GetHurt(damage, invincible: true, 0f, explosion: true);
							}
						}
						if (Mathf.Abs(base.transform.position.x - other.transform.position.x) < 0.25f && Mathf.Abs(base.transform.position.z - other.transform.position.z) < 0.25f)
						{
							MonoSingleton<NewMovement>.Instance.Launch(other.transform.position, 200f, maxSize);
						}
						else
						{
							MonoSingleton<NewMovement>.Instance.Launch(base.transform.position, 200f, maxSize);
						}
					}
				}
				else if ((other.gameObject.layer == 10 || other.gameObject.layer == 11) && canHit != AffectedSubjects.PlayerOnly)
				{
					EnemyIdentifierIdentifier componentInParent = other.GetComponentInParent<EnemyIdentifierIdentifier>();
					if (componentInParent != null && componentInParent.eid != null)
					{
						Collider component = componentInParent.eid.GetComponent<Collider>();
						if (component != null && !hitColliders.Contains(component) && !componentInParent.eid.dead)
						{
							hitColliders.Add(component);
							if (componentInParent.eid.enemyType != EnemyType.MaliciousFace && (!enemy || componentInParent.eid.enemyType != EnemyType.HideousMass) && !toIgnore.Contains(componentInParent.eid.enemyType))
							{
								if (friendlyFire)
								{
									componentInParent.eid.hitter = "ffexplosion";
								}
								else if (enemy)
								{
									componentInParent.eid.hitter = "enemy";
								}
								else
								{
									componentInParent.eid.hitter = "explosion";
								}
								if (!componentInParent.eid.hitterWeapons.Contains(hitterWeapon))
								{
									componentInParent.eid.hitterWeapons.Add(hitterWeapon);
								}
								Vector3 vector = (other.transform.position - base.transform.position).normalized;
								if (componentInParent.eid.enemyType == EnemyType.Drone && damage == 0)
								{
									vector = Vector3.zero;
								}
								else if (vector.y <= 0.5f)
								{
									vector = new Vector3(vector.x, vector.y + 0.5f, vector.z);
								}
								else if (vector.y < 1f)
								{
									vector = new Vector3(vector.x, 1f, vector.z);
								}
								componentInParent.eid.DeliverDamage(other.gameObject, vector * 50000f, other.transform.position, (float)damage / 10f, tryForExplode: false);
								if (ignite)
								{
									Flammable componentInChildren = componentInParent.eid.GetComponentInChildren<Flammable>();
									if (componentInChildren != null)
									{
										componentInChildren.Burn(damage / 10);
									}
								}
							}
						}
						else if (componentInParent.eid.dead)
						{
							hitColliders.Add(other);
							if (enemy)
							{
								componentInParent.eid.hitter = "enemy";
							}
							else
							{
								componentInParent.eid.hitter = "explosion";
							}
							componentInParent.eid.DeliverDamage(other.gameObject, (other.transform.position - base.transform.position).normalized * 5000f, other.transform.position, (float)damage / 10f * enemyDamageMultiplier, tryForExplode: false);
							if (ignite && componentInParent.TryGetComponent<Flammable>(out var _))
							{
								Flammable componentInChildren2 = componentInParent.eid.GetComponentInChildren<Flammable>();
								if (componentInChildren2 != null)
								{
									componentInChildren2.Burn(damage / 10);
								}
							}
						}
					}
				}
				else if (other.TryGetComponent<Breakable>(out component3))
				{
					if (!component3.accurateExplosionsOnly)
					{
						component3.Break();
					}
					else
					{
						Vector3 vector2 = other.ClosestPoint(base.transform.position);
						if (!Physics.Raycast(vector2 + (vector2 - base.transform.position).normalized * 0.001f, base.transform.position - vector2, Vector3.Distance(base.transform.position, vector2), lmask, QueryTriggerInteraction.Ignore))
						{
							component3.Break();
						}
					}
				}
				else if (other.TryGetComponent<Glass>(out component4))
				{
					component4.Shatter();
				}
				else if (ignite && other.TryGetComponent<Flammable>(out component5) && (!enemy || !component5.playerOnly))
				{
					component5.Burn(4f);
				}
			}
			else if (other.TryGetComponent<Breakable>(out component6) && (!component6.accurateExplosionsOnly || !flag || hitInfo.collider == other))
			{
				if (component6.accurateExplosionsOnly && !flag)
				{
					Vector3 vector3 = other.ClosestPoint(base.transform.position);
					if (!Physics.Raycast(vector3 + (vector3 - base.transform.position).normalized * 0.001f, base.transform.position - vector3, Vector3.Distance(base.transform.position, vector3), lmask, QueryTriggerInteraction.Ignore))
					{
						component6.Break();
					}
				}
				else
				{
					component6.Break();
				}
			}
			else if (other.TryGetComponent<Glass>(out component7))
			{
				component7.Shatter();
			}
			else if (ignite && other.TryGetComponent<Flammable>(out component8) && (!enemy || !component8.playerOnly))
			{
				component8.Burn(4f);
			}
		}
		if (harmless || !(other.gameObject.tag != "Player"))
		{
			return;
		}
		Rigidbody component9 = other.GetComponent<Rigidbody>();
		if ((bool)component9 && (other.gameObject.layer != 14 || component9.useGravity) && other.gameObject.tag != "IgnorePushes")
		{
			if (!hitColliders.Contains(other))
			{
				hitColliders.Add(other);
			}
			Vector3 normalized = (other.transform.position - base.transform.position).normalized;
			normalized = new Vector3(normalized.x * (5f - Vector3.Distance(other.transform.position, base.transform.position)) * 7500f, normalized.y * (5f - Vector3.Distance(other.transform.position, base.transform.position)), normalized.z * (5f - Vector3.Distance(other.transform.position, base.transform.position)) * 7500f);
			if (component9.useGravity)
			{
				normalized = new Vector3(normalized.x, 18750f, normalized.z);
			}
			component9.AddForce(normalized);
		}
		if (other.gameObject.layer != 14)
		{
			return;
		}
		ThrownSword component10 = other.GetComponent<ThrownSword>();
		Projectile component11 = other.GetComponent<Projectile>();
		if (component10 != null)
		{
			component10.deflected = true;
		}
		if (component11 != null)
		{
			component11.homingType = HomingType.None;
			other.transform.LookAt(other.transform.position + (other.transform.position - base.transform.position));
			component11.friendly = true;
			component11.target = null;
			component11.turnSpeed = 0f;
			if (component11.speed < 65f)
			{
				component11.speed = 65f;
			}
		}
	}

	private void Fade()
	{
		harmless = true;
		mr = GetComponent<MeshRenderer>();
		materialColor = mr.material.GetColor("_Color");
		fading = true;
		speed /= 4f;
	}

	private void BecomeHarmless()
	{
		harmless = true;
	}
}
