using System;
using System.Collections.Generic;
using UnityEngine;

public class RevolverBeam : MonoBehaviour
{
	public class RaycastResult : IComparable<RaycastResult>
	{
		public float distance;

		public Transform transform;

		public RaycastHit rrhit;

		public RaycastResult(RaycastHit hit)
		{
			distance = hit.distance;
			transform = hit.transform;
			rrhit = hit;
		}

		public int CompareTo(RaycastResult other)
		{
			return distance.CompareTo(other.distance);
		}
	}

	public BeamType beamType;

	public HitterAttribute[] attributes;

	private LineRenderer lr;

	private AudioSource aud;

	private Light muzzleLight;

	public Vector3 alternateStartPoint;

	[HideInInspector]
	public int bodiesPierced;

	private int enemiesPierced;

	private RaycastHit[] allHits;

	[HideInInspector]
	public List<RaycastResult> hitList = new List<RaycastResult>();

	private GunControl gc;

	private RaycastHit hit;

	private Vector3 shotHitPoint;

	public CameraController cc;

	public GameObject hitParticle;

	public int bulletForce;

	public bool quickDraw;

	public int gunVariation;

	public float damage;

	public float critDamageOverride;

	public int hitAmount;

	public int maxHitsPerTarget;

	private int currentHits;

	public bool noMuzzleflash;

	private bool fadeOut;

	private bool didntHit;

	private LayerMask ignoreEnemyTrigger;

	private LayerMask enemyLayerMask;

	private LayerMask pierceLayerMask;

	public GameObject ricochetSound;

	public GameObject enemyHitSound;

	public bool fake;

	public EnemyType ignoreEnemyType;

	public bool deflected;

	public bool strongAlt;

	[HideInInspector]
	public List<EnemyIdentifier> hitEids = new List<EnemyIdentifier>();

	private void Start()
	{
		muzzleLight = GetComponent<Light>();
		lr = GetComponent<LineRenderer>();
		cc = MonoSingleton<CameraController>.Instance;
		gc = cc.GetComponentInChildren<GunControl>();
		if (beamType == BeamType.Enemy)
		{
			enemyLayerMask = (int)enemyLayerMask | 4;
		}
		enemyLayerMask = (int)enemyLayerMask | 0x400;
		enemyLayerMask = (int)enemyLayerMask | 0x800;
		pierceLayerMask = (int)pierceLayerMask | 0x100;
		pierceLayerMask = (int)pierceLayerMask | 0x1000000;
		pierceLayerMask = (int)pierceLayerMask | 0x4000000;
		ignoreEnemyTrigger = (int)enemyLayerMask | (int)pierceLayerMask;
		if (!fake)
		{
			Shoot();
		}
		else
		{
			fadeOut = true;
		}
		if (maxHitsPerTarget == 0)
		{
			maxHitsPerTarget = 99;
		}
	}

	private void Update()
	{
		if (fadeOut)
		{
			lr.widthMultiplier -= Time.deltaTime * 1.5f;
			if (muzzleLight != null)
			{
				muzzleLight.intensity -= Time.deltaTime * 100f;
			}
			if (lr.widthMultiplier <= 0f)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}
	}

	public void FakeShoot(Vector3 target)
	{
		Vector3 position = base.transform.position;
		if (alternateStartPoint != Vector3.zero)
		{
			position = alternateStartPoint;
		}
		lr.SetPosition(0, position);
		lr.SetPosition(1, target);
		Transform child = base.transform.GetChild(0);
		if (!noMuzzleflash)
		{
			child.position = position;
			child.rotation = base.transform.rotation;
		}
		else
		{
			child.gameObject.SetActive(value: false);
		}
	}

	private void Shoot()
	{
		if (hitAmount == 1)
		{
			fadeOut = true;
			if (beamType == BeamType.Railgun)
			{
				cc.CameraShake(2f);
			}
			else if (strongAlt)
			{
				cc.CameraShake(0.25f);
			}
			if (Physics.Raycast(base.transform.position, base.transform.forward, out hit, float.PositiveInfinity, ignoreEnemyTrigger))
			{
				bool flag = false;
				if (hit.transform.gameObject.layer == 8 || hit.transform.gameObject.layer == 24)
				{
					if (Physics.SphereCast(base.transform.position, 0.4f, base.transform.forward, out var hitInfo, Vector3.Distance(base.transform.position, hit.point), enemyLayerMask))
					{
						ExecuteHits(hitInfo);
					}
					else
					{
						ExecuteHits(hit);
					}
				}
				else if (beamType != BeamType.Revolver && hit.transform.gameObject.tag == "Coin")
				{
					flag = true;
					lr.SetPosition(1, hit.transform.position);
					GameObject gameObject = UnityEngine.Object.Instantiate(base.gameObject, hit.point, base.transform.rotation);
					gameObject.SetActive(value: false);
					RevolverBeam component = gameObject.GetComponent<RevolverBeam>();
					component.bodiesPierced = 0;
					component.noMuzzleflash = true;
					component.alternateStartPoint = Vector3.zero;
					if (beamType == BeamType.MaliciousFace)
					{
						component.deflected = true;
					}
					Coin component2 = hit.transform.gameObject.GetComponent<Coin>();
					if (component2 != null)
					{
						component2.DelayedReflectRevolver(hit.point, gameObject);
					}
					fadeOut = true;
				}
				else
				{
					ExecuteHits(hit);
				}
				shotHitPoint = hit.point;
				if (hit.transform.gameObject.tag != "Armor" && !flag)
				{
					GameObject gameObject2 = UnityEngine.Object.Instantiate(hitParticle, shotHitPoint, base.transform.rotation);
					gameObject2.transform.forward = hit.normal;
					if (beamType == BeamType.MaliciousFace)
					{
						Explosion[] componentsInChildren = gameObject2.GetComponentsInChildren<Explosion>();
						int num = PlayerPrefs.GetInt("Diff", 2);
						Explosion[] array = componentsInChildren;
						foreach (Explosion explosion in array)
						{
							if (deflected)
							{
								explosion.canHit = AffectedSubjects.EnemiesOnly;
							}
							else
							{
								explosion.enemy = true;
							}
							if (num < 2)
							{
								explosion.maxSize *= 0.65f;
								explosion.speed *= 0.65f;
							}
						}
					}
				}
			}
			else if (Physics.SphereCast(base.transform.position, 0.4f, base.transform.forward, out hit, Vector3.Distance(base.transform.position, hit.point), enemyLayerMask))
			{
				ExecuteHits(hit);
				shotHitPoint = hit.point;
				if (hit.transform.gameObject.tag != "Armor")
				{
					UnityEngine.Object.Instantiate(hitParticle, shotHitPoint, base.transform.rotation).transform.forward = hit.normal;
				}
			}
			else
			{
				shotHitPoint = base.transform.position + base.transform.forward * 1000f;
			}
		}
		else
		{
			if (Physics.Raycast(base.transform.position, base.transform.forward, out hit, float.PositiveInfinity, pierceLayerMask))
			{
				shotHitPoint = hit.point;
				if (hit.transform.gameObject.tag != "Armor")
				{
					UnityEngine.Object.Instantiate(hitParticle, shotHitPoint, base.transform.rotation).transform.forward = hit.normal;
				}
			}
			else
			{
				shotHitPoint = base.transform.position + base.transform.forward * 999f;
				didntHit = true;
			}
			float radius = 0.6f;
			if (beamType == BeamType.Railgun)
			{
				radius = 1.2f;
			}
			allHits = Physics.SphereCastAll(base.transform.position, radius, base.transform.forward, Vector3.Distance(base.transform.position, shotHitPoint), enemyLayerMask, QueryTriggerInteraction.Collide);
		}
		Vector3 position = base.transform.position;
		if (alternateStartPoint != Vector3.zero)
		{
			position = alternateStartPoint;
		}
		lr.SetPosition(0, position);
		lr.SetPosition(1, shotHitPoint);
		if (hitAmount != 1)
		{
			PiercingShotOrder();
		}
		Transform child = base.transform.GetChild(0);
		if (!noMuzzleflash)
		{
			child.position = position;
			child.rotation = base.transform.rotation;
		}
		else
		{
			child.gameObject.SetActive(value: false);
		}
	}

	private void PiercingShotOrder()
	{
		hitList.Clear();
		RaycastHit[] array = allHits;
		foreach (RaycastHit raycastHit in array)
		{
			hitList.Add(new RaycastResult(raycastHit));
		}
		bool flag = false;
		if (!didntHit && (hit.transform.gameObject.layer == 8 || hit.transform.gameObject.layer == 24))
		{
			if (hit.transform.GetComponent<Breakable>() != null)
			{
				flag = true;
			}
			else if ((bool)hit.transform.GetComponent<AttributeChecker>())
			{
				flag = true;
			}
		}
		if (!didntHit && (flag || hit.transform.gameObject.tag == "Glass" || hit.transform.gameObject.tag == "GlassFloor" || hit.transform.gameObject.tag == "Armor"))
		{
			hitList.Add(new RaycastResult(hit));
		}
		hitList.Sort();
		PiercingShotCheck();
	}

	private void PiercingShotCheck()
	{
		if (enemiesPierced < hitList.Count)
		{
			if (hitList[enemiesPierced].transform == null)
			{
				enemiesPierced++;
				PiercingShotCheck();
				return;
			}
			if (hitList[enemiesPierced].transform.gameObject.tag == "Armor")
			{
				lr.SetPosition(1, hitList[enemiesPierced].rrhit.point);
				GameObject obj = UnityEngine.Object.Instantiate(base.gameObject, hitList[enemiesPierced].rrhit.point, base.transform.rotation);
				obj.transform.forward = Vector3.Reflect(base.transform.forward, hitList[enemiesPierced].rrhit.normal);
				RevolverBeam component = obj.GetComponent<RevolverBeam>();
				component.noMuzzleflash = true;
				component.alternateStartPoint = Vector3.zero;
				component.bodiesPierced = bodiesPierced;
				UnityEngine.Object.Instantiate(ricochetSound, hitList[enemiesPierced].rrhit.point, Quaternion.identity);
				fadeOut = true;
				enemiesPierced = hitList.Count;
				return;
			}
			if (hitList[enemiesPierced].transform.gameObject.tag == "Coin" && bodiesPierced < hitAmount)
			{
				lr.SetPosition(1, hitList[enemiesPierced].transform.position);
				GameObject gameObject = UnityEngine.Object.Instantiate(base.gameObject, hitList[enemiesPierced].rrhit.point, base.transform.rotation);
				gameObject.SetActive(value: false);
				RevolverBeam component2 = gameObject.GetComponent<RevolverBeam>();
				component2.bodiesPierced = 0;
				component2.noMuzzleflash = true;
				component2.alternateStartPoint = Vector3.zero;
				component2.hitEids.Clear();
				Coin component3 = hitList[enemiesPierced].transform.gameObject.GetComponent<Coin>();
				if (component3 != null)
				{
					component3.DelayedReflectRevolver(hitList[enemiesPierced].rrhit.point, gameObject);
				}
				fadeOut = true;
				return;
			}
			if ((hitList[enemiesPierced].transform.gameObject.layer == 10 || hitList[enemiesPierced].transform.gameObject.layer == 11) && hitList[enemiesPierced].transform.gameObject.tag != "Breakable" && hitList[enemiesPierced].transform.gameObject.tag != "Armor" && bodiesPierced < hitAmount)
			{
				EnemyIdentifierIdentifier componentInParent = hitList[enemiesPierced].transform.gameObject.GetComponentInParent<EnemyIdentifierIdentifier>();
				if (!componentInParent)
				{
					if (attributes.Length != 0)
					{
						AttributeChecker component4 = hitList[enemiesPierced].transform.GetComponent<AttributeChecker>();
						if ((bool)component4)
						{
							HitterAttribute[] array = attributes;
							for (int i = 0; i < array.Length; i++)
							{
								if (array[i] == component4.targetAttribute)
								{
									component4.DelayedActivate();
									break;
								}
							}
						}
					}
					enemiesPierced++;
					currentHits = 0;
					PiercingShotCheck();
					return;
				}
				EnemyIdentifier eid = componentInParent.eid;
				if (eid != null)
				{
					if (!hitEids.Contains(eid) || (eid.dead && beamType == BeamType.Revolver && enemiesPierced == hitList.Count - 1))
					{
						bool flag = false;
						if (eid.dead)
						{
							flag = true;
						}
						ExecuteHits(hitList[enemiesPierced].rrhit);
						if (!flag || hitList[enemiesPierced].transform.gameObject.layer == 11 || (beamType == BeamType.Revolver && enemiesPierced == hitList.Count - 1))
						{
							currentHits++;
							bodiesPierced++;
							UnityEngine.Object.Instantiate(hitParticle, hitList[enemiesPierced].rrhit.point, base.transform.rotation);
							cc.HitStop(0.05f);
						}
						else
						{
							if (beamType == BeamType.Revolver)
							{
								hitEids.Add(eid);
							}
							enemiesPierced++;
							currentHits = 0;
						}
						if (currentHits >= maxHitsPerTarget)
						{
							hitEids.Add(eid);
							currentHits = 0;
							enemiesPierced++;
						}
						if (beamType == BeamType.Revolver && !flag)
						{
							Invoke("PiercingShotCheck", 0.05f);
						}
						else if (beamType == BeamType.Revolver)
						{
							PiercingShotCheck();
						}
						else if (!flag)
						{
							Invoke("PiercingShotCheck", 0.025f);
						}
						else
						{
							Invoke("PiercingShotCheck", 0.01f);
						}
					}
					else
					{
						enemiesPierced++;
						currentHits = 0;
						PiercingShotCheck();
					}
				}
				else
				{
					ExecuteHits(hitList[enemiesPierced].rrhit);
					enemiesPierced++;
					PiercingShotCheck();
				}
				return;
			}
			if (hitList[enemiesPierced].transform.gameObject.tag == "Glass" || hitList[enemiesPierced].transform.gameObject.tag == "GlassFloor")
			{
				Glass component5 = hitList[enemiesPierced].transform.gameObject.GetComponent<Glass>();
				if (!component5.broken)
				{
					component5.Shatter();
				}
				enemiesPierced++;
				PiercingShotCheck();
				return;
			}
			if (beamType == BeamType.Enemy && hitList[enemiesPierced].transform == MonoSingleton<NewMovement>.Instance.transform)
			{
				ExecuteHits(hitList[enemiesPierced].rrhit);
				bodiesPierced++;
				enemiesPierced++;
				PiercingShotCheck();
				return;
			}
			Breakable component6 = hitList[enemiesPierced].transform.GetComponent<Breakable>();
			if (component6 != null && (beamType == BeamType.Railgun || component6.weak))
			{
				if (component6.interrupt)
				{
					MonoSingleton<StyleHUD>.Instance.AddPoints(100, "<color=lime>INTERRUPTION</color>");
					cc.GetComponentInChildren<Punch>().ParryFlash();
				}
				component6.Break();
			}
			else if (bodiesPierced < hitAmount)
			{
				ExecuteHits(hitList[enemiesPierced].rrhit);
			}
			enemiesPierced++;
			PiercingShotCheck();
		}
		else
		{
			enemiesPierced = 0;
			fadeOut = true;
		}
	}

	public void ExecuteHits(RaycastHit currentHit)
	{
		if (!(currentHit.transform != null))
		{
			return;
		}
		Breakable component = currentHit.transform.GetComponent<Breakable>();
		if (component != null && (strongAlt || beamType == BeamType.Railgun || component.weak))
		{
			if (component.interrupt)
			{
				MonoSingleton<StyleHUD>.Instance.AddPoints(100, "<color=lime>INTERRUPTION</color>");
				cc.GetComponentInChildren<Punch>().ParryFlash();
			}
			component.Break();
		}
		Coin component2 = currentHit.transform.GetComponent<Coin>();
		if (component2 != null && beamType == BeamType.Revolver)
		{
			if (quickDraw)
			{
				component2.quickDraw = true;
			}
			component2.DelayedReflectRevolver(currentHit.point);
		}
		if (currentHit.transform.gameObject.tag == "Enemy" || currentHit.transform.gameObject.tag == "Body" || currentHit.transform.gameObject.tag == "Limb" || currentHit.transform.gameObject.tag == "EndLimb" || currentHit.transform.gameObject.tag == "Head")
		{
			EnemyIdentifier eid = currentHit.transform.GetComponentInParent<EnemyIdentifierIdentifier>().eid;
			if ((bool)eid && (beamType == BeamType.MaliciousFace || beamType == BeamType.Enemy) && (eid.enemyType == ignoreEnemyType || EnemyIdentifier.CheckHurtException(eid.enemyType, ignoreEnemyType)) && !deflected)
			{
				return;
			}
			if (hitAmount > 1)
			{
				cc.CameraShake(1f);
			}
			else
			{
				cc.CameraShake(0.5f);
			}
			if ((bool)eid && !eid.dead && quickDraw)
			{
				MonoSingleton<StyleHUD>.Instance.AddPoints(50, "<color=cyan>QUICKDRAW</color>");
			}
			string text = "";
			if (beamType == BeamType.Revolver)
			{
				text = "revolver";
			}
			else if (beamType == BeamType.Railgun)
			{
				text = "railcannon";
			}
			else if (beamType == BeamType.MaliciousFace || beamType == BeamType.Enemy)
			{
				text = "enemy";
			}
			if ((bool)eid)
			{
				eid.hitter = text;
				if (attributes != null && attributes.Length != 0)
				{
					HitterAttribute[] array = attributes;
					foreach (HitterAttribute item in array)
					{
						eid.hitterAttributes.Add(item);
					}
				}
				if (!eid.hitterWeapons.Contains(text + gunVariation))
				{
					eid.hitterWeapons.Add(text + gunVariation);
				}
			}
			float critMultiplier = 1f;
			if (beamType != BeamType.Revolver)
			{
				critMultiplier = 0f;
			}
			if (critDamageOverride != 0f || strongAlt)
			{
				critMultiplier = critDamageOverride;
			}
			float num = damage;
			if ((bool)eid && deflected)
			{
				if (beamType == BeamType.MaliciousFace && eid.enemyType == EnemyType.MaliciousFace)
				{
					num = 999f;
				}
				else if (beamType == BeamType.Enemy)
				{
					num *= 2.5f;
				}
				MonoSingleton<StyleHUD>.Instance.AddPoints(400, "CHARGEBACK");
			}
			bool tryForExplode = false;
			if (strongAlt)
			{
				tryForExplode = true;
			}
			if ((bool)eid)
			{
				eid.DeliverDamage(currentHit.transform.gameObject, (currentHit.transform.position - base.transform.position).normalized * bulletForce, currentHit.point, num, tryForExplode, critMultiplier);
			}
			if (beamType != BeamType.MaliciousFace && beamType != BeamType.Enemy)
			{
				if ((bool)eid && !eid.dead && currentHit.transform.gameObject.tag == "Head" && beamType == BeamType.Revolver)
				{
					gc.headshots++;
					gc.headShotComboTime = 3f;
				}
				else if (currentHit.transform.gameObject.tag != "Head" || beamType == BeamType.Railgun)
				{
					gc.headshots = 0;
					gc.headShotComboTime = 0f;
				}
				if (gc.headshots > 1)
				{
					MonoSingleton<StyleHUD>.Instance.AddPoints(gc.headshots * 20, "<color=cyan>HEADSHOT COMBO x" + gc.headshots + "</color>");
				}
			}
			UnityEngine.Object.Instantiate(enemyHitSound, currentHit.point, Quaternion.identity);
		}
		else if (currentHit.transform.gameObject.layer == 10)
		{
			Grenade componentInParent = currentHit.transform.GetComponentInParent<Grenade>();
			if (componentInParent != null)
			{
				MonoSingleton<CameraController>.Instance.GetComponentInChildren<Punch>().ParryFlash();
				componentInParent.Explode(big: true);
			}
		}
		else if (beamType == BeamType.Enemy && currentHit.transform == MonoSingleton<NewMovement>.Instance.transform)
		{
			UnityEngine.Object.Instantiate(enemyHitSound, currentHit.point, Quaternion.identity);
			MonoSingleton<NewMovement>.Instance.GetHurt(Mathf.RoundToInt(damage * 10f), invincible: true);
		}
		else
		{
			gc.headshots = 0;
			gc.headShotComboTime = 0f;
			if (currentHit.transform.gameObject.tag == "Armor")
			{
				GameObject obj = UnityEngine.Object.Instantiate(base.gameObject, currentHit.point, base.transform.rotation);
				obj.transform.forward = Vector3.Reflect(base.transform.forward, currentHit.normal);
				RevolverBeam component3 = obj.GetComponent<RevolverBeam>();
				component3.noMuzzleflash = true;
				component3.alternateStartPoint = Vector3.zero;
				UnityEngine.Object.Instantiate(ricochetSound, currentHit.point, Quaternion.identity);
			}
		}
	}
}
