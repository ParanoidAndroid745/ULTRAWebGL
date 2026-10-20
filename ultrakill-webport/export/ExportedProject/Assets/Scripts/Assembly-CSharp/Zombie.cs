using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Zombie : MonoBehaviour
{
	public float health;

	private float originalHealth;

	private int difficulty;

	private Rigidbody[] rbs;

	public bool limp;

	public GameObject player;

	public NavMeshAgent nma;

	public Animator anim;

	private float currentSpeed;

	private Rigidbody rb;

	private ZombieMelee zm;

	private ZombieProjectiles zp;

	private AudioSource aud;

	public AudioClip[] hurtSounds;

	public float hurtSoundVol;

	public AudioClip deathSound;

	public float deathSoundVol;

	public AudioClip scream;

	private GroundCheckEnemy gc;

	public bool grounded;

	private float defaultSpeed;

	public Vector3 agentVelocity;

	private StyleCalculator scalc;

	private EnemyIdentifier eid;

	private GoreZone gz;

	public Material deadMaterial;

	public Material simplifiedMaterial;

	public Renderer smr;

	private Material originalMaterial;

	public GameObject chest;

	private float chestHP = 3f;

	public bool chestExploding;

	public GameObject chestExplosionStuff;

	public bool attacking;

	public LayerMask lmask;

	public Transform target;

	public List<EnemyIdentifier> enemyTargets = new List<EnemyIdentifier>();

	public bool friendly;

	public EnemyIdentifier targetedEnemy;

	private bool noheal;

	private float speedMultiplier = 1f;

	public bool stopped;

	public bool knockedBack;

	private float knockBackCharge;

	public float brakes;

	public float juggleWeight;

	public bool falling;

	public bool musicRequested;

	private float fallSpeed;

	private float fallTime;

	private float reduceFallTime;

	private BloodsplatterManager bsm;

	public bool variableSpeed;

	private bool blindedByCheat;

	private GoreZone GetGoreZone()
	{
		if ((bool)gz)
		{
			return gz;
		}
		gz = GoreZone.ResolveGoreZone(base.transform);
		return gz;
	}

	private void Start()
	{
		rbs = GetComponentsInChildren<Rigidbody>();
		player = MonoSingleton<NewMovement>.Instance.gameObject;
		nma = GetComponent<NavMeshAgent>();
		rb = GetComponent<Rigidbody>();
		zm = GetComponent<ZombieMelee>();
		zp = GetComponent<ZombieProjectiles>();
		anim = GetComponent<Animator>();
		gc = GetComponentInChildren<GroundCheckEnemy>();
		eid = GetComponent<EnemyIdentifier>();
		originalHealth = health;
		if (!smr)
		{
			smr = GetComponentInChildren<SkinnedMeshRenderer>();
		}
		if ((bool)smr)
		{
			originalMaterial = smr.sharedMaterial;
		}
		if (!friendly)
		{
			target = player.transform;
			EnemyScanner componentInChildren = GetComponentInChildren<EnemyScanner>();
			if (componentInChildren != null)
			{
				componentInChildren.gameObject.SetActive(value: false);
			}
		}
		if (limp)
		{
			noheal = true;
		}
		difficulty = PlayerPrefs.GetInt("Diff", 2);
		if (difficulty != 2)
		{
			if (nma == null)
			{
				nma = GetComponent<NavMeshAgent>();
			}
			if (difficulty >= 3)
			{
				if (nma != null)
				{
					nma.acceleration = 60f;
					nma.angularSpeed = 2600f;
				}
				speedMultiplier = 1.25f;
			}
			else if (difficulty == 1)
			{
				if (nma != null)
				{
					nma.acceleration = 30f;
					nma.angularSpeed = 400f;
					nma.speed = 15f;
				}
				speedMultiplier = 0.75f;
			}
			else if (difficulty == 0)
			{
				if (nma != null)
				{
					nma.acceleration = 15f;
					nma.angularSpeed = 400f;
					nma.speed = 10f;
				}
				speedMultiplier = 0.5f;
			}
		}
		if (nma != null)
		{
			defaultSpeed = nma.speed;
		}
		if ((bool)anim && !variableSpeed)
		{
			if (difficulty == 1)
			{
				anim.SetFloat("Speed", 0.875f);
			}
			else if (difficulty == 0)
			{
				anim.SetFloat("Speed", 0.75f);
			}
		}
	}

	private void Update()
	{
		if (CheatsController.BlindEnemies)
		{
			target = null;
			blindedByCheat = true;
		}
		else if (blindedByCheat)
		{
			target = player.transform;
			blindedByCheat = false;
		}
		if (anim != null && variableSpeed)
		{
			anim.SetFloat("Speed", anim.speed);
		}
		if (knockBackCharge > 0f)
		{
			knockBackCharge = Mathf.MoveTowards(knockBackCharge, 0f, Time.deltaTime);
		}
		if (!limp && friendly && enemyTargets.Count > 0)
		{
			if (target == null)
			{
				float num = 100f;
				foreach (EnemyIdentifier enemyTarget in enemyTargets)
				{
					if (Vector3.Distance(base.transform.position, enemyTarget.transform.position) < num)
					{
						num = Vector3.Distance(base.transform.position, enemyTarget.transform.position);
						target = enemyTarget.transform;
						targetedEnemy = enemyTarget;
					}
				}
			}
			else if (targetedEnemy.dead)
			{
				enemyTargets.Remove(targetedEnemy);
				if (enemyTargets.Count == 0)
				{
					target = null;
				}
				else
				{
					float num2 = 100f;
					foreach (EnemyIdentifier enemyTarget2 in enemyTargets)
					{
						if (Vector3.Distance(base.transform.position, enemyTarget2.transform.position) < num2)
						{
							num2 = Vector3.Distance(base.transform.position, enemyTarget2.transform.position);
							target = enemyTarget2.transform;
							targetedEnemy = enemyTarget2;
						}
					}
				}
			}
		}
		if (falling && !limp)
		{
			fallTime += Time.deltaTime;
			if (gc.onGround)
			{
				if (fallSpeed <= -50f)
				{
					if (eid == null)
					{
						eid = GetComponent<EnemyIdentifier>();
					}
					eid.Splatter();
					return;
				}
				fallSpeed = 0f;
				nma.updatePosition = true;
				nma.updateRotation = true;
				if (aud == null)
				{
					aud = GetComponent<AudioSource>();
				}
				if (aud.clip == scream && aud.isPlaying)
				{
					aud.Stop();
				}
				rb.isKinematic = true;
				rb.useGravity = false;
				nma.enabled = true;
				nma.Warp(base.transform.position);
				falling = false;
				anim.SetBool("Falling", value: false);
			}
			else if (fallTime > 0.05f && rb.velocity.y < fallSpeed)
			{
				fallSpeed = rb.velocity.y;
				reduceFallTime = 0.5f;
				if (aud == null)
				{
					aud = GetComponent<AudioSource>();
				}
				if (!aud.isPlaying && !limp && (!Physics.Raycast(base.transform.position, Vector3.down, out var _, 30f, lmask) || rb.velocity.y < -50f))
				{
					aud.clip = scream;
					aud.volume = 1f;
					aud.priority = 78;
					aud.pitch = Random.Range(0.8f, 1.2f);
					aud.Play();
				}
			}
			else if (fallTime > 0.05f && rb.velocity.y > fallSpeed)
			{
				reduceFallTime = Mathf.MoveTowards(reduceFallTime, 0f, Time.deltaTime);
				if (reduceFallTime <= 0f)
				{
					fallSpeed = rb.velocity.y;
				}
			}
			else if (rb.velocity.y > 0f)
			{
				fallSpeed = 0f;
			}
		}
		else if (fallTime > 0f)
		{
			fallTime = 0f;
		}
	}

	private void FixedUpdate()
	{
		if (limp)
		{
			return;
		}
		if (knockedBack && knockBackCharge <= 0f && rb.velocity.magnitude < 1f && gc.onGround)
		{
			StopKnockBack();
		}
		else if (knockedBack)
		{
			if (eid.useBrakes || gc.onGround)
			{
				if (knockBackCharge <= 0f && gc.onGround)
				{
					brakes = Mathf.MoveTowards(brakes, 0f, 0.0005f * brakes);
				}
				rb.velocity = new Vector3(rb.velocity.x * 0.95f * brakes, rb.velocity.y - juggleWeight, rb.velocity.z * 0.95f * brakes);
			}
			else if (!eid.useBrakes)
			{
				brakes = 1f;
			}
			nma.updatePosition = false;
			nma.updateRotation = false;
			nma.enabled = false;
			rb.isKinematic = false;
			rb.useGravity = true;
		}
		if (grounded && nma != null && nma.enabled && variableSpeed && nma.isOnNavMesh)
		{
			if (nma.isStopped || nma.velocity == Vector3.zero || stopped)
			{
				anim.speed = 1f * speedMultiplier;
			}
			else
			{
				anim.speed = nma.velocity.magnitude / nma.speed * speedMultiplier;
			}
		}
		else if (!grounded && gc.onGround)
		{
			grounded = true;
			nma.speed = defaultSpeed;
		}
		if (!gc.onGround && !falling)
		{
			rb.isKinematic = false;
			rb.useGravity = true;
			nma.enabled = false;
			anim.speed = 1f;
			falling = true;
			anim.SetBool("Falling", value: true);
			anim.SetTrigger("StartFalling");
			if (zp != null)
			{
				zp.CancelAttack();
			}
			if (zm != null)
			{
				zm.CancelAttack();
			}
		}
	}

	public void KnockBack(Vector3 force)
	{
		nma.enabled = false;
		rb.isKinematic = false;
		rb.useGravity = true;
		if (!knockedBack || (!gc.onGround && rb.velocity.y < 0f))
		{
			rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
		}
		if (!gc.onGround)
		{
			rb.AddForce(Vector3.up, ForceMode.VelocityChange);
		}
		rb.AddForce(force / 10f, ForceMode.VelocityChange);
		knockedBack = true;
		knockBackCharge += force.magnitude / 1500f;
		brakes = 1f;
	}

	public void StopKnockBack()
	{
		if (!(nma != null))
		{
			return;
		}
		if (Physics.Raycast(base.transform.position + Vector3.up * 0.1f, Vector3.down, out var hitInfo, float.PositiveInfinity, lmask))
		{
			_ = Vector3.zero;
			if (NavMesh.SamplePosition(hitInfo.point, out var hit, 4f, nma.areaMask))
			{
				knockedBack = false;
				nma.updatePosition = true;
				nma.updateRotation = true;
				nma.enabled = true;
				rb.isKinematic = true;
				juggleWeight = 0f;
				eid.magneted = false;
				nma.Warp(hit.position);
			}
			else
			{
				knockBackCharge = 0.5f;
			}
		}
		else
		{
			knockBackCharge = 0.5f;
		}
	}

	public void GetHurt(GameObject target, Vector3 force, float multiplier, float critMultiplier)
	{
		string hitLimb = "";
		bool flag = false;
		bool flag2 = false;
		bool flag3 = true;
		if ((bool)gc && !gc.onGround)
		{
			multiplier *= 1.5f;
		}
		if (force != Vector3.zero && !limp)
		{
			KnockBack(force / 100f);
			if (eid.hitter == "heavypunch")
			{
				eid.useBrakes = false;
			}
			else
			{
				eid.useBrakes = true;
			}
		}
		if (PlayerPrefs.GetInt("BlOn", 1) == 0)
		{
			flag3 = false;
		}
		if (eid == null)
		{
			eid = GetComponent<EnemyIdentifier>();
		}
		if (chestExploding && health <= 0f && (target.gameObject.tag == "Limb" || target.gameObject.tag == "EndLimb") && target.GetComponentInParent<EnemyIdentifier>() != null)
		{
			ChestExplodeEnd();
		}
		GameObject gameObject = null;
		if (bsm == null)
		{
			bsm = MonoSingleton<BloodsplatterManager>.Instance;
		}
		if (target.gameObject.tag == "Head")
		{
			float num = 1f * multiplier + multiplier * critMultiplier;
			health -= num;
			if (eid.hitter != "fire" && num > 0f)
			{
				gameObject = ((!(num >= 1f) && !(health <= 0f)) ? bsm.GetGore(GoreType.Small, eid.underwater, eid.sandified) : bsm.GetGore(GoreType.Head, eid.underwater, eid.sandified));
			}
			_ = (player.transform.position - base.transform.position).normalized;
			if (!limp)
			{
				flag2 = true;
				hitLimb = "head";
			}
			if (health <= 0f)
			{
				if (!limp)
				{
					GoLimp();
				}
				if (eid.hitter != "fire")
				{
					float num2 = 1f;
					if (eid.hitter == "shotgun" || eid.hitter == "shotgunzone")
					{
						num2 = 0.5f;
					}
					else if (eid.hitter == "Explosion")
					{
						num2 = 0.25f;
					}
					if (target.transform.parent != null && target.transform.parent.GetComponentInParent<Rigidbody>() != null)
					{
						target.transform.parent.GetComponentInParent<Rigidbody>().AddForce(force * 10f);
					}
					if (flag3 && eid.hitter != "harpoon")
					{
						GameObject gameObject2 = null;
						GoreZone goreZone = GetGoreZone();
						for (int i = 0; (float)i < 6f * num2; i++)
						{
							gameObject2 = bsm.GetGib(GibType.Skull);
							if ((bool)gameObject2 && (bool)goreZone && (bool)goreZone.goreZone)
							{
								ReadyGib(gameObject2, target);
							}
						}
						for (int j = 0; (float)j < 4f * num2; j++)
						{
							gameObject2 = bsm.GetGib(GibType.Brain);
							if ((bool)gameObject2 && (bool)goreZone && (bool)goreZone.goreZone)
							{
								ReadyGib(gameObject2, target);
							}
						}
						for (int k = 0; (float)k < 2f * num2; k++)
						{
							gameObject2 = bsm.GetGib(GibType.Eye);
							if ((bool)gameObject2 && (bool)goreZone && (bool)goreZone.goreZone)
							{
								ReadyGib(gameObject2, target);
							}
							gameObject2 = bsm.GetGib(GibType.Jaw);
							if ((bool)gameObject2 && (bool)goreZone && (bool)goreZone.goreZone)
							{
								ReadyGib(gameObject2, target);
							}
						}
					}
				}
			}
		}
		else if (target.gameObject.tag == "Limb" || target.gameObject.tag == "EndLimb")
		{
			if (eid == null)
			{
				eid = GetComponent<EnemyIdentifier>();
			}
			float num = 1f * multiplier + 0.5f * multiplier * critMultiplier;
			health -= num;
			if (eid.hitter != "fire" && num > 0f)
			{
				if (((num >= 1f || health <= 0f) && eid.hitter != "explosion") || (eid.hitter == "explosion" && target.gameObject.tag == "EndLimb"))
				{
					gameObject = bsm.GetGore(GoreType.Limb, eid.underwater, eid.sandified);
				}
				else if (eid.hitter != "explosion")
				{
					gameObject = bsm.GetGore(GoreType.Small, eid.underwater, eid.sandified);
				}
			}
			_ = (player.transform.position - base.transform.position).normalized;
			if (!limp)
			{
				flag2 = true;
				hitLimb = "limb";
			}
			if (health <= 0f)
			{
				if (!limp)
				{
					GoLimp();
				}
				if (eid.hitter != "fire")
				{
					if (((target.gameObject.tag == "Limb") & flag3) && eid.hitter != "harpoon" && eid.hitter != "explosion")
					{
						float num3 = 1f;
						GetGoreZone();
						if (eid.hitter == "shotgun" || eid.hitter == "shotgunzone")
						{
							num3 = 0.5f;
						}
						for (int l = 0; (float)l < 4f * num3; l++)
						{
							GameObject gib = bsm.GetGib(GibType.Gib);
							if ((bool)gib && (bool)gz && (bool)gz.goreZone)
							{
								ReadyGib(gib, target);
							}
						}
					}
					else
					{
						target.transform.localScale = Vector3.zero;
					}
				}
			}
		}
		else if (target.gameObject.tag == "Body" || (attacking && (eid.hitter == "shotgunzone" || eid.hitter == "punch") && player.GetComponent<Rigidbody>().velocity.magnitude > 18f))
		{
			float num = 1f * multiplier;
			if (eid == null)
			{
				eid = GetComponent<EnemyIdentifier>();
			}
			if (eid.hitter == "shotgunzone")
			{
				if (!attacking && (target.gameObject != chest || health - num > 0f))
				{
					num = 0f;
				}
				else if (attacking && (target.gameObject == chest || player.GetComponent<Rigidbody>().velocity.magnitude > 18f))
				{
					num *= 2f;
					player.GetComponentInChildren<Punch>().Parry();
				}
			}
			else if (eid.hitter == "punch" && attacking)
			{
				num = 2f;
				attacking = false;
				player.GetComponentInChildren<Punch>().Parry();
			}
			health -= num;
			if (eid.hitter != "fire" && num > 0f)
			{
				gameObject = ((!(num >= 1f) && !(health <= 0f)) ? bsm.GetGore(GoreType.Small, eid.underwater, eid.sandified) : bsm.GetGore(GoreType.Body, eid.underwater, eid.sandified));
			}
			if (health <= 0f && target.gameObject == chest && eid.hitter != "fire")
			{
				if (eid.hitter == "shotgunzone")
				{
					chestHP = 0f;
				}
				else
				{
					chestHP -= num;
				}
				if (chestHP <= 0f && eid.hitter != "harpoon")
				{
					CharacterJoint[] componentsInChildren = target.GetComponentsInChildren<CharacterJoint>();
					GoreZone goreZone2 = GetGoreZone();
					if (componentsInChildren.Length != 0)
					{
						CharacterJoint[] array = componentsInChildren;
						foreach (CharacterJoint characterJoint in array)
						{
							if (characterJoint.transform.parent.parent == chest.transform)
							{
								Rigidbody[] componentsInChildren2 = characterJoint.transform.GetComponentsInChildren<Rigidbody>();
								foreach (Rigidbody obj in componentsInChildren2)
								{
									obj.isKinematic = false;
									obj.useGravity = true;
								}
								Object.Destroy(characterJoint);
							}
						}
					}
					if (!limp && !eid.exploded && !eid.dead)
					{
						if (gc.onGround)
						{
							rb.isKinematic = true;
							knockedBack = false;
						}
						anim.Rebind();
						anim.speed = 1f;
						anim.SetTrigger("ChestExplosion");
						chestExploding = true;
					}
					GetGoreZone();
					if (flag3)
					{
						float num4 = 1f;
						if (eid.hitter == "shotgun" || eid.hitter == "shotgunzone" || eid.hitter == "explosion")
						{
							num4 = 0.5f;
						}
						for (int num5 = 0; (float)num5 < 2f * num4; num5++)
						{
							GameObject gib2 = bsm.GetGib(GibType.Jaw);
							if ((bool)gib2 && (bool)goreZone2 && (bool)goreZone2.goreZone)
							{
								ReadyGib(gib2, target);
							}
						}
						if (!eid.sandified)
						{
							Object.Instantiate(chestExplosionStuff, target.transform.parent.position, target.transform.parent.rotation).transform.SetParent(target.transform.parent, worldPositionStays: true);
						}
					}
					GameObject gore = bsm.GetGore(GoreType.Head, eid.underwater, eid.sandified);
					if ((bool)gore)
					{
						gore.transform.position = chest.transform.position;
						Bloodsplatter component = gore.GetComponent<Bloodsplatter>();
						if ((bool)component)
						{
							component.hpAmount = 10;
						}
						if (goreZone2 != null && goreZone2.goreZone != null)
						{
							gore.transform.SetParent(goreZone2.goreZone, worldPositionStays: true);
						}
						if (!noheal && (bool)component)
						{
							component.GetReady();
						}
					}
					target.transform.localScale = Vector3.zero;
				}
			}
			if (!limp)
			{
				flag2 = true;
				hitLimb = "body";
			}
			if (health <= 0f)
			{
				if (!limp)
				{
					GoLimp();
				}
				if (target.GetComponentInParent<Rigidbody>() != null)
				{
					target.GetComponentInParent<Rigidbody>().AddForce(force * 10f);
				}
			}
		}
		if (gameObject != null)
		{
			GetGoreZone();
			gameObject.transform.position = target.transform.position;
			if (eid.hitter == "drill")
			{
				gameObject.transform.localScale *= 2f;
			}
			if (gz != null && gz.goreZone != null)
			{
				gameObject.transform.SetParent(gz.goreZone, worldPositionStays: true);
			}
			Bloodsplatter component2 = gameObject.GetComponent<Bloodsplatter>();
			if ((bool)component2)
			{
				ParticleSystem.CollisionModule collision = component2.GetComponent<ParticleSystem>().collision;
				if (eid.hitter == "shotgun" || eid.hitter == "shotgunzone" || eid.hitter == "explosion")
				{
					if (Random.Range(0f, 1f) > 0.5f)
					{
						collision.enabled = false;
					}
					component2.hpAmount = 3;
				}
				else if (eid.hitter == "nail")
				{
					component2.hpAmount = 1;
					component2.GetComponent<AudioSource>().volume *= 0.8f;
				}
				if (!noheal)
				{
					component2.GetReady();
				}
			}
		}
		if (health <= 0f && (target.gameObject.CompareTag("Limb") || target.gameObject.CompareTag("Head")) && eid.hitter != "harpoon" && eid.hitter != "fire")
		{
			if (target.transform.childCount > 0)
			{
				Transform child = target.transform.GetChild(0);
				CharacterJoint[] componentsInChildren3 = target.GetComponentsInChildren<CharacterJoint>();
				GoreZone goreZone3 = GetGoreZone();
				if (componentsInChildren3.Length != 0)
				{
					CharacterJoint[] array = componentsInChildren3;
					foreach (CharacterJoint characterJoint2 in array)
					{
						if (goreZone3 != null && goreZone3.goreZone != null)
						{
							characterJoint2.transform.SetParent(goreZone3.goreZone);
						}
						Object.Destroy(characterJoint2);
					}
				}
				CharacterJoint component3 = target.GetComponent<CharacterJoint>();
				if (component3 != null)
				{
					component3.connectedBody = null;
					Object.Destroy(component3);
				}
				target.transform.position = child.position;
				target.transform.SetParent(child);
				if (goreZone3 != null && goreZone3.gibZone != null)
				{
					child.SetParent(goreZone3.gibZone, worldPositionStays: true);
				}
				Object.Destroy(target.GetComponent<Rigidbody>());
			}
			Object.Destroy(target.GetComponent<Collider>());
			target.transform.localScale = Vector3.zero;
		}
		else if (health <= 0f && target.gameObject.tag == "EndLimb" && eid.hitter != "harpoon" && eid.hitter != "fire")
		{
			target.transform.localScale = Vector3.zero;
		}
		if (health > 0f && !limp && hurtSounds.Length != 0)
		{
			if (aud == null)
			{
				aud = GetComponent<AudioSource>();
			}
			aud.clip = hurtSounds[Random.Range(0, hurtSounds.Length)];
			aud.volume = hurtSoundVol;
			aud.pitch = Random.Range(0.85f, 1.35f);
			aud.priority = 12;
			aud.Play();
		}
		if (eid == null)
		{
			eid = GetComponent<EnemyIdentifier>();
		}
		if (multiplier == 0f)
		{
			flag2 = false;
		}
		if (!flag2 || !(eid.hitter != "enemy"))
		{
			return;
		}
		if (scalc == null)
		{
			scalc = MonoSingleton<StyleCalculator>.Instance;
		}
		if (health <= 0f)
		{
			flag = true;
			if (!gc.onGround && (eid.hitter == "explosion" || eid.hitter == "ffexplosion" || eid.hitter == "railcannon"))
			{
				scalc.shud.AddPoints(120, "<color=cyan>FIREWORKS</color>");
			}
			else if (!gc.onGround && eid.hitter != "deathzone")
			{
				scalc.shud.AddPoints(50, "<color=cyan>AIRSHOT</color>");
			}
		}
		if (eid.hitter != "secret" && (bool)scalc)
		{
			scalc.HitCalculator(eid.hitter, "zombie", hitLimb, flag, base.gameObject);
		}
		if (flag && eid.hitter != "fire")
		{
			Flammable componentInChildren = GetComponentInChildren<Flammable>();
			if ((bool)componentInChildren && componentInChildren.burning && (bool)scalc)
			{
				scalc.shud.AddPoints(50, "<color=cyan>FINISHED OFF</color>");
			}
		}
	}

	public void GoLimp()
	{
		if (limp)
		{
			return;
		}
		gz = GetGoreZone();
		attacking = false;
		Invoke("StopHealing", 1f);
		health = 0f;
		if (eid == null)
		{
			eid = GetComponent<EnemyIdentifier>();
		}
		if (!friendly && !eid.dontCountAsKills)
		{
			if (gz != null && gz.checkpoint != null)
			{
				gz.AddDeath();
				gz.checkpoint.sm.kills++;
			}
			else
			{
				MonoSingleton<StatsManager>.Instance.kills++;
			}
		}
		EnemySimplifier componentInChildren = GetComponentInChildren<EnemySimplifier>();
		if (componentInChildren != null)
		{
			componentInChildren.Begone();
		}
		if (deadMaterial != null)
		{
			if ((bool)smr)
			{
				smr.sharedMaterial = deadMaterial;
			}
			else if ((bool)smr)
			{
				smr.sharedMaterial = originalMaterial;
			}
		}
		if (zm != null)
		{
			zm.track = false;
			if (!chestExploding)
			{
				anim.StopPlayback();
			}
			if (zm.tr != null)
			{
				zm.tr.enabled = false;
			}
			Object.Destroy(base.gameObject.GetComponentInChildren<SwingCheck2>().gameObject);
			Object.Destroy(zm);
		}
		if (zp != null)
		{
			zp.DamageEnd();
			if (!chestExploding)
			{
				anim.StopPlayback();
			}
			Object.Destroy(zp);
			Projectile componentInChildren2 = GetComponentInChildren<Projectile>();
			if (componentInChildren2 != null)
			{
				Object.Destroy(componentInChildren2.gameObject);
			}
		}
		if (nma != null)
		{
			Object.Destroy(nma);
		}
		if (!chestExploding)
		{
			Object.Destroy(anim);
		}
		Object.Destroy(base.gameObject.GetComponent<Collider>());
		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
		}
		Object.Destroy(rb);
		if (aud == null)
		{
			aud = GetComponent<AudioSource>();
		}
		if (deathSound != null)
		{
			aud.clip = deathSound;
			if (eid.hitter != "fire")
			{
				aud.volume = deathSoundVol;
			}
			else
			{
				aud.volume = 0.5f;
			}
			aud.pitch = Random.Range(0.85f, 1.35f);
			aud.priority = 11;
			aud.Play();
		}
		if (!limp && !chestExploding)
		{
			rbs = GetComponentsInChildren<Rigidbody>();
			Rigidbody[] array = rbs;
			foreach (Rigidbody obj in array)
			{
				obj.isKinematic = false;
				obj.useGravity = true;
			}
		}
		if (!limp)
		{
			if (!eid.dontCountAsKills)
			{
				ActivateNextWave componentInParent = GetComponentInParent<ActivateNextWave>();
				if (componentInParent != null)
				{
					componentInParent.deadEnemies++;
				}
			}
			if (gz != null && gz.gibZone != null)
			{
				base.transform.SetParent(gz.gibZone, worldPositionStays: true);
			}
		}
		if (musicRequested)
		{
			musicRequested = false;
			MonoSingleton<MusicManager>.Instance.PlayCleanMusic();
		}
		limp = true;
		EnemyScanner componentInChildren3 = GetComponentInChildren<EnemyScanner>();
		if (componentInChildren3 != null)
		{
			Object.Destroy(componentInChildren3.gameObject);
		}
	}

	public void ChestExplodeEnd()
	{
		anim.enabled = false;
		anim.StopPlayback();
		Object.Destroy(anim);
		rbs = GetComponentsInChildren<Rigidbody>();
		Rigidbody[] array = rbs;
		foreach (Rigidbody rigidbody in array)
		{
			if (rigidbody != null)
			{
				rigidbody.isKinematic = false;
				rigidbody.useGravity = true;
			}
		}
		chestExploding = false;
	}

	public void StopHealing()
	{
		noheal = true;
	}

	private void ReadyGib(GameObject tempGib, GameObject target)
	{
		tempGib.transform.position = target.transform.position;
		tempGib.transform.rotation = Random.rotation;
		GetGoreZone();
		tempGib.transform.SetParent(gz.goreZone);
	}
}
