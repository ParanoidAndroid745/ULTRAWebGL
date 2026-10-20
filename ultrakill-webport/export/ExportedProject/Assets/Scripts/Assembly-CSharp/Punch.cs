using UnityEngine;

public class Punch : MonoBehaviour
{
	private InputManager inman;

	public FistType type;

	private string hitter;

	private float damage;

	private float screenShakeMultiplier;

	private float force;

	private bool tryForExplode;

	private float cooldownCost;

	public bool ready = true;

	[HideInInspector]
	public Animator anim;

	private SkinnedMeshRenderer smr;

	private Revolver rev;

	private AudioSource aud;

	private AudioSource aud2;

	private GameObject camObj;

	private CameraController cc;

	private RaycastHit hit;

	public LayerMask deflectionLayerMask;

	public LayerMask ignoreEnemyTrigger;

	public LayerMask environmentMask;

	private NewMovement nmov;

	private TrailRenderer tr;

	private Light parryLight;

	private GameObject currentDustParticle;

	public GameObject dustParticle;

	private GameObject parryFlash;

	public AudioClip normalHit;

	public AudioClip heavyHit;

	public AudioClip specialHit;

	private StyleHUD shud;

	private StatsManager sman;

	public bool holding;

	public Transform holder;

	public ItemIdentifier heldItem;

	private FistControl fc;

	private bool shopping;

	private int shopRequests;

	public GameObject parriedProjectileHitObject;

	private ProjectileParryZone ppz;

	private bool returnToOrigRot;

	public GameObject blastWave;

	private bool holdingInput;

	public GameObject shell;

	public Transform shellEjector;

	private AudioSource ejectorAud;

	public bool crowdReactions;

	private CrowdReactions crorea;

	private bool alreadyBoostedProjectile;

	private bool ignoreDoublePunch;

	private void Start()
	{
		inman = MonoSingleton<InputManager>.Instance;
		anim = GetComponent<Animator>();
		smr = GetComponentInChildren<SkinnedMeshRenderer>();
		rev = base.transform.parent.parent.GetComponentInChildren<Revolver>();
		camObj = MonoSingleton<CameraController>.Instance.gameObject;
		cc = MonoSingleton<CameraController>.Instance;
		aud = GetComponent<AudioSource>();
		parryLight = base.transform.Find("PunchZone").GetComponent<Light>();
		aud2 = parryLight.GetComponent<AudioSource>();
		nmov = GetComponentInParent<NewMovement>();
		tr = GetComponentInChildren<TrailRenderer>();
		parryFlash = MonoSingleton<HUDOptions>.Instance.transform.Find("ParryFlash").gameObject;
		shud = MonoSingleton<StyleHUD>.Instance;
		sman = MonoSingleton<StatsManager>.Instance;
		holdingInput = false;
		if (fc == null)
		{
			fc = GetComponentInParent<FistControl>();
		}
		switch (type)
		{
		case FistType.Standard:
			damage = 1f;
			screenShakeMultiplier = 1f;
			force = 25f;
			tryForExplode = false;
			cooldownCost = 2f;
			hitter = "punch";
			break;
		case FistType.Heavy:
			damage = 2.5f;
			screenShakeMultiplier = 2f;
			force = 100f;
			tryForExplode = true;
			cooldownCost = 3f;
			hitter = "heavypunch";
			break;
		}
		crorea = MonoSingleton<CrowdReactions>.Instance;
		if (crorea != null)
		{
			crowdReactions = true;
		}
	}

	private void OnEnable()
	{
		holdingInput = false;
		ReadyToPunch();
		ignoreDoublePunch = false;
		if (fc == null)
		{
			fc = GetComponentInParent<FistControl>();
			anim = GetComponent<Animator>();
		}
		if (fc.heldObject != null)
		{
			heldItem = fc.heldObject;
			heldItem.transform.SetParent(holder, worldPositionStays: true);
			holding = true;
			if (!heldItem.noHoldingAnimation && fc.forceNoHold <= 0)
			{
				anim.SetBool("SemiHolding", value: false);
				anim.SetBool("Holding", value: true);
			}
			else
			{
				anim.SetBool("SemiHolding", value: true);
			}
			if (heldItem.reverseTransformSettings)
			{
				heldItem.transform.localPosition = heldItem.putDownPosition;
				heldItem.transform.localScale = heldItem.putDownScale;
				heldItem.transform.localRotation = Quaternion.Euler(heldItem.putDownRotation);
			}
			else
			{
				heldItem.transform.localPosition = Vector3.zero;
				heldItem.transform.localScale = Vector3.one;
				heldItem.transform.localRotation = Quaternion.identity;
			}
		}
	}

	public void ResetHeldState()
	{
		holding = false;
		anim.SetBool("Holding", value: false);
		anim.SetBool("SemiHolding", value: false);
	}

	public void ForceThrow()
	{
		if (!heldItem)
		{
			ResetHeldState();
			return;
		}
		Rigidbody componentInChildren = heldItem.GetComponentInChildren<Rigidbody>();
		if (componentInChildren != null)
		{
			heldItem.transform.SetParent(null, worldPositionStays: true);
			heldItem.pickedUp = false;
			if (heldItem.reverseTransformSettings)
			{
				heldItem.transform.localScale = Vector3.one;
			}
			else
			{
				heldItem.transform.localScale = heldItem.putDownScale;
			}
			Transform[] componentsInChildren = heldItem.GetComponentsInChildren<Transform>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].gameObject.layer = 22;
			}
			Collider componentInChildren2 = heldItem.GetComponentInChildren<Collider>();
			if (componentInChildren2 != null)
			{
				componentInChildren2.enabled = true;
			}
			heldItem.transform.position = base.transform.parent.position + base.transform.parent.forward;
			heldItem.SendMessage("PutDown", SendMessageOptions.DontRequireReceiver);
			componentInChildren.isKinematic = false;
			componentInChildren.AddForce((base.transform.parent.forward + Vector3.up * 0.1f) * 5000f);
			anim.SetBool("Holding", value: false);
			anim.SetBool("SemiHolding", value: false);
			holding = false;
			fc.heldObject = null;
			heldItem = null;
		}
	}

	public void PlaceHeldObject(ItemPlaceZone[] placeZones, Transform target)
	{
		if (!heldItem)
		{
			ResetHeldState();
			return;
		}
		AnimatorStateInfo currentAnimatorStateInfo = anim.GetCurrentAnimatorStateInfo(0);
		if (currentAnimatorStateInfo.IsName("JabHolding"))
		{
			ignoreDoublePunch = true;
			anim.Play("Jab", 0, currentAnimatorStateInfo.normalizedTime);
		}
		holding = false;
		anim.SetBool("Holding", value: false);
		anim.SetBool("SemiHolding", value: false);
		heldItem.transform.SetParent(target);
		heldItem.pickedUp = false;
		if (heldItem.reverseTransformSettings)
		{
			heldItem.transform.localPosition = Vector3.zero;
			heldItem.transform.localScale = Vector3.one;
			heldItem.transform.localRotation = Quaternion.identity;
		}
		else
		{
			heldItem.transform.localPosition = heldItem.putDownPosition;
			heldItem.transform.localScale = heldItem.putDownScale;
			heldItem.transform.localRotation = Quaternion.Euler(heldItem.putDownRotation);
		}
		Transform[] componentsInChildren = heldItem.GetComponentsInChildren<Transform>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].gameObject.layer = 22;
		}
		Rigidbody componentInChildren = heldItem.GetComponentInChildren<Rigidbody>();
		if (componentInChildren != null)
		{
			componentInChildren.isKinematic = false;
		}
		Collider componentInChildren2 = heldItem.GetComponentInChildren<Collider>();
		if (componentInChildren2 != null)
		{
			componentInChildren2.enabled = true;
		}
		heldItem.SendMessage("PutDown", SendMessageOptions.DontRequireReceiver);
		Object.Instantiate(heldItem.pickUpSound);
		heldItem = null;
		fc.heldObject = null;
		for (int i = 0; i < placeZones.Length; i++)
		{
			placeZones[i].CheckItem();
		}
		ResetHeldState();
	}

	public void ForceHold(ItemIdentifier itid)
	{
		holding = true;
		if (!itid.noHoldingAnimation && fc.forceNoHold <= 0)
		{
			anim.SetBool("SemiHolding", value: false);
			anim.SetBool("Holding", value: true);
		}
		else
		{
			anim.SetBool("SemiHolding", value: true);
		}
		AnimatorStateInfo currentAnimatorStateInfo = anim.GetCurrentAnimatorStateInfo(0);
		if (currentAnimatorStateInfo.IsName("Jab") || currentAnimatorStateInfo.IsName("Jab2"))
		{
			ignoreDoublePunch = true;
			anim.Play("JabHolding", 0, currentAnimatorStateInfo.normalizedTime);
		}
		ItemPlaceZone[] componentsInParent = itid.GetComponentsInParent<ItemPlaceZone>();
		heldItem = itid;
		itid.transform.SetParent(holder);
		fc.heldObject = itid;
		itid.pickedUp = true;
		if (itid.reverseTransformSettings)
		{
			heldItem.transform.localPosition = itid.putDownPosition;
			heldItem.transform.localScale = itid.putDownScale;
			heldItem.transform.localRotation = Quaternion.Euler(itid.putDownRotation);
		}
		else
		{
			heldItem.transform.localPosition = Vector3.zero;
			heldItem.transform.localScale = Vector3.one;
			heldItem.transform.localRotation = Quaternion.identity;
		}
		Transform[] componentsInChildren = heldItem.GetComponentsInChildren<Transform>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].gameObject.layer = 13;
		}
		Rigidbody componentInChildren = heldItem.GetComponentInChildren<Rigidbody>();
		if (componentInChildren != null)
		{
			componentInChildren.isKinematic = true;
		}
		Collider componentInChildren2 = heldItem.GetComponentInChildren<Collider>();
		if (componentInChildren2 != null)
		{
			componentInChildren2.enabled = false;
		}
		Object.Instantiate(itid.pickUpSound);
		heldItem.SendMessage("PickUp", SendMessageOptions.DontRequireReceiver);
		if (componentsInParent != null && componentsInParent.Length != 0)
		{
			ItemPlaceZone[] array = componentsInParent;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].CheckItem();
			}
		}
	}

	private void OnDisable()
	{
		holding = false;
		anim.SetBool("Holding", value: false);
		anim.SetBool("SemiHolding", value: false);
		ignoreDoublePunch = false;
	}

	private void Update()
	{
		if (MonoSingleton<OptionsManager>.Instance.paused)
		{
			return;
		}
		if (MonoSingleton<InputManager>.Instance.InputSource.Punch.WasPerformedThisFrame && ready && !shopping && fc.fistCooldown <= 0f && fc.activated)
		{
			fc.weightCooldown += cooldownCost * 0.25f + fc.weightCooldown * cooldownCost * 0.1f;
			fc.fistCooldown += fc.weightCooldown;
			PunchStart();
			holdingInput = true;
		}
		if (holdingInput && MonoSingleton<InputManager>.Instance.InputSource.Punch.WasCanceledThisFrame)
		{
			holdingInput = false;
		}
		float layerWeight = anim.GetLayerWeight(1);
		if (shopping && layerWeight < 1f)
		{
			anim.SetLayerWeight(1, Mathf.MoveTowards(layerWeight, 1f, Time.deltaTime / 10f + 5f * Time.deltaTime * (1f - layerWeight)));
		}
		else if (!shopping && layerWeight > 0f)
		{
			anim.SetLayerWeight(1, Mathf.MoveTowards(layerWeight, 0f, Time.deltaTime / 10f + 5f * Time.deltaTime * layerWeight));
		}
		if (MonoSingleton<InputManager>.Instance.InputSource.Fire1.WasPerformedThisFrame && shopping)
		{
			anim.SetTrigger("ShopTap");
		}
		if (returnToOrigRot)
		{
			base.transform.parent.localRotation = Quaternion.RotateTowards(base.transform.parent.localRotation, Quaternion.identity, (Quaternion.Angle(base.transform.parent.localRotation, Quaternion.identity) * 5f + 5f) * Time.deltaTime * 5f);
			if (base.transform.parent.localRotation == Quaternion.identity)
			{
				returnToOrigRot = false;
			}
		}
		if (fc.shopping && !shopping)
		{
			ShopMode();
		}
		else if (!fc.shopping && shopping)
		{
			StopShop();
		}
		if (holding && (bool)heldItem)
		{
			if (!heldItem.noHoldingAnimation && fc.forceNoHold <= 0)
			{
				anim.SetBool("SemiHolding", value: false);
				anim.SetBool("Holding", value: true);
			}
			else
			{
				anim.SetBool("SemiHolding", value: true);
			}
		}
	}

	private void PunchStart()
	{
		if (ready)
		{
			ready = false;
			anim.SetFloat("PunchRandomizer", Random.Range(0f, 1f));
			anim.SetTrigger("Punch");
			aud.pitch = Random.Range(0.9f, 1.1f);
			aud.Play();
			tr.widthMultiplier = 0.5f;
			if (holding && (bool)heldItem)
			{
				heldItem.SendMessage("PunchWith", SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	private void ActiveStart()
	{
		if (ignoreDoublePunch)
		{
			ignoreDoublePunch = false;
			return;
		}
		returnToOrigRot = false;
		bool flag = false;
		if (type == FistType.Standard)
		{
			if (Physics.Raycast(cc.GetDefaultPos(), camObj.transform.forward, out hit, 4f, deflectionLayerMask) || Physics.BoxCast(cc.GetDefaultPos(), Vector3.one * 0.3f, camObj.transform.forward, out hit, camObj.transform.rotation, 4f, deflectionLayerMask))
			{
				if (hit.transform.gameObject.layer == 14)
				{
					Projectile component = hit.transform.gameObject.GetComponent<Projectile>();
					ThrownSword component2 = hit.transform.gameObject.GetComponent<ThrownSword>();
					MassSpear component3 = hit.transform.gameObject.GetComponent<MassSpear>();
					if (component != null && !component.undeflectable)
					{
						ParryProjectile(component);
						flag = true;
					}
					else if (component2 != null && !component2.friendly && component2.active)
					{
						component2.GetParried();
						anim.Play("Hook", -1, 0.065f);
						Parry();
						flag = true;
					}
					else if (component3 != null)
					{
						if (!component3.beenStopped)
						{
							component3.Deflected();
							anim.Play("Hook", -1, 0.065f);
							Parry();
							flag = true;
						}
						else if (component3.hitPlayer)
						{
							aud2.clip = specialHit;
							aud2.volume = 0.6f;
							aud2.Play();
							cc.HitStop(0.1f);
							cc.CameraShake(0.5f * screenShakeMultiplier);
							component3.GetHurt(5f);
							flag = true;
						}
					}
				}
			}
			else
			{
				if (ppz == null)
				{
					ppz = base.transform.parent.GetComponentInChildren<ProjectileParryZone>();
				}
				if (ppz != null)
				{
					Projectile projectile = ppz.CheckParryZone();
					if (projectile != null && !projectile.undeflectable && (!alreadyBoostedProjectile || !projectile.playerBullet))
					{
						ParryProjectile(projectile);
						flag = true;
					}
				}
			}
		}
		bool flag2 = holding;
		Collider[] array = Physics.OverlapSphere(cc.GetDefaultPos(), 0.1f, ignoreEnemyTrigger, QueryTriggerInteraction.Collide);
		if (array != null && array.Length != 0)
		{
			Collider[] array2 = array;
			foreach (Collider collider in array2)
			{
				PunchSuccess(cc.GetDefaultPos(), collider.transform);
			}
			flag = true;
		}
		else if (Physics.Raycast(cc.GetDefaultPos(), camObj.transform.forward, out hit, 4f, ignoreEnemyTrigger, QueryTriggerInteraction.Collide) || Physics.SphereCast(cc.GetDefaultPos(), 1f, camObj.transform.forward, out hit, 4f, ignoreEnemyTrigger, QueryTriggerInteraction.Collide))
		{
			bool flag3 = false;
			if (Physics.Raycast(cc.GetDefaultPos(), camObj.transform.forward, out var hitInfo, 4f, environmentMask) && Vector3.Distance(base.transform.position, hit.point) > Vector3.Distance(base.transform.position, hitInfo.point))
			{
				flag3 = true;
			}
			if (!flag3)
			{
				PunchSuccess(hit.point, hit.transform);
				flag = true;
			}
		}
		if (Physics.CheckSphere(cc.GetDefaultPos(), 0.01f, environmentMask, QueryTriggerInteraction.Collide))
		{
			Collider[] array2 = Physics.OverlapSphere(cc.GetDefaultPos(), 0.01f, environmentMask);
			foreach (Collider collider2 in array2)
			{
				flag = true;
				AltHit(collider2.transform);
			}
		}
		else if (Physics.Raycast(cc.GetDefaultPos(), camObj.transform.forward, out hit, 4f, environmentMask))
		{
			AltHit(hit.transform);
			if (!flag && (hit.transform.gameObject.layer == 8 || hit.transform.gameObject.layer == 24))
			{
				base.transform.parent.localRotation = Quaternion.identity;
				cc.CameraShake(0.2f * screenShakeMultiplier);
				aud2.clip = normalHit;
				aud2.volume = 0.6f;
				aud2.Play();
				currentDustParticle = Object.Instantiate(dustParticle, hit.point, base.transform.rotation);
				currentDustParticle.transform.forward = hit.normal;
				Breakable component4 = hit.collider.gameObject.GetComponent<Breakable>();
				if (component4 != null && (component4.weak || (!component4.precisionOnly && type == FistType.Heavy)))
				{
					component4.Break();
				}
				if (type == FistType.Heavy)
				{
					Glass component5 = hit.collider.gameObject.GetComponent<Glass>();
					if (component5 != null && !component5.broken)
					{
						component5.Shatter();
					}
				}
			}
		}
		if (flag2 && holding && heldItem != null)
		{
			ForceThrow();
			cc.CameraShake(0.2f * screenShakeMultiplier);
		}
		else
		{
			cc.CameraShake(0.2f * screenShakeMultiplier);
		}
	}

	public void CoinFlip()
	{
		if (ready)
		{
			anim.SetTrigger("CoinFlip");
		}
	}

	private void ActiveEnd()
	{
		tr.widthMultiplier = 0f;
		ignoreDoublePunch = false;
	}

	private void PunchEnd()
	{
	}

	private void ReadyToPunch()
	{
		returnToOrigRot = true;
		holdingInput = false;
		ready = true;
		alreadyBoostedProjectile = false;
		ignoreDoublePunch = false;
	}

	private void PunchSuccess(Vector3 point, Transform target)
	{
		base.transform.parent.LookAt(point);
		if (Quaternion.Angle(base.transform.parent.localRotation, Quaternion.identity) > 45f)
		{
			Quaternion localRotation = base.transform.parent.localRotation;
			float num = localRotation.eulerAngles.x;
			float num2 = localRotation.eulerAngles.y;
			float num3 = localRotation.eulerAngles.z;
			if (num > 180f)
			{
				num -= 360f;
			}
			if (num2 > 180f)
			{
				num2 -= 360f;
			}
			if (num3 > 180f)
			{
				num3 -= 360f;
			}
			localRotation.eulerAngles = new Vector3(Mathf.Clamp(num, -45f, 45f), Mathf.Clamp(num2, -45f, 45f), Mathf.Clamp(num3, -45f, 45f));
			base.transform.parent.localRotation = localRotation;
		}
		if (target.gameObject.tag == "Enemy" || target.gameObject.tag == "Head" || target.gameObject.tag == "Body" || target.gameObject.tag == "Limb" || target.gameObject.tag == "EndLimb")
		{
			if (anim.GetFloat("PunchRandomizer") < 0.5f)
			{
				anim.Play("Jab", 0, 0.075f);
			}
			else
			{
				anim.Play("Jab2", 0, 0.075f);
			}
			if ((bool)aud2)
			{
				aud2.clip = heavyHit;
				aud2.volume = 0.8f;
				aud2.Play();
			}
			cc.HitStop(0.1f);
			cc.CameraShake(0.5f * screenShakeMultiplier);
			EnemyIdentifier enemyIdentifier = null;
			if (target.TryGetComponent<EnemyIdentifierIdentifier>(out var component))
			{
				enemyIdentifier = component.eid;
			}
			if ((bool)enemyIdentifier)
			{
				enemyIdentifier.hitter = hitter;
				enemyIdentifier.DeliverDamage(target.gameObject, camObj.transform.forward * force * 1000f, point, damage, tryForExplode);
			}
			if (holding)
			{
				heldItem.SendMessage("HitWith", target.gameObject, SendMessageOptions.DontRequireReceiver);
			}
		}
		else if (target.gameObject.tag == "Coin" && type == FistType.Standard)
		{
			Coin component2 = target.GetComponent<Coin>();
			if ((bool)component2 && component2.doubled)
			{
				anim.Play("Hook", 0, 0.065f);
				target.GetComponent<Coin>()?.DelayedPunchflection();
			}
		}
	}

	public void Parry(bool hook = false)
	{
		if (hook)
		{
			anim.Play("Hook", 0, 0.065f);
		}
		aud.pitch = Random.Range(0.7f, 0.8f);
		nmov.GetHealth(999, silent: true);
		ParryFlash();
		shud.AddPoints(100, "<color=lime>PARRY</color>");
	}

	public void ParryFlash()
	{
		parryLight.enabled = true;
		parryFlash.SetActive(value: true);
		Invoke("hideFlash", 0.1f);
		aud2.clip = specialHit;
		aud2.volume = 0.6f;
		aud2.Play();
		parryFlash.GetComponent<AudioSource>().Play();
		cc.TrueStop(0.25f);
		cc.CameraShake(0.5f);
	}

	private void ParryProjectile(Projectile proj)
	{
		proj.hittingPlayer = false;
		proj.friendly = true;
		proj.speed *= 2f;
		proj.homingType = HomingType.None;
		proj.explosionEffect = parriedProjectileHitObject;
		proj.precheckForCollisions = true;
		Rigidbody component = proj.GetComponent<Rigidbody>();
		if (proj.playerBullet)
		{
			alreadyBoostedProjectile = true;
			proj.boosted = true;
			proj.GetComponent<SphereCollider>().radius *= 4f;
			proj.damage = 0f;
			if ((bool)component)
			{
				component.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			}
		}
		if ((bool)component)
		{
			component.constraints = RigidbodyConstraints.FreezeRotation;
		}
		anim.Play("Hook", 0, 0.065f);
		if (!proj.playerBullet)
		{
			Parry();
		}
		else
		{
			ParryFlash();
		}
		if (proj.explosive)
		{
			proj.explosive = false;
		}
		Rigidbody component2 = proj.GetComponent<Rigidbody>();
		if ((bool)component2 && component2.useGravity)
		{
			component2.useGravity = false;
		}
		Vector3 zero = Vector3.zero;
		Vector3 vector = camObj.transform.forward;
		if ((bool)MonoSingleton<CameraFrustumTargeter>.Instance && (bool)MonoSingleton<CameraFrustumTargeter>.Instance.CurrentTarget && MonoSingleton<CameraFrustumTargeter>.Instance.IsAutoAimed)
		{
			vector = MonoSingleton<CameraFrustumTargeter>.Instance.CurrentTarget.bounds.center - camObj.transform.position;
		}
		zero = ((!Physics.Raycast(cc.GetDefaultPos(), vector, out var hitInfo, float.PositiveInfinity, ignoreEnemyTrigger)) ? (cc.GetDefaultPos() + vector * 10f) : hitInfo.point);
		proj.transform.LookAt(zero);
		if (proj.speed == 0f)
		{
			component2.velocity = (zero - base.transform.position).normalized * 250f;
		}
		else if (proj.speed < 100f)
		{
			proj.speed = 100f;
		}
		if (proj.spreaded)
		{
			ProjectileSpread componentInParent = proj.GetComponentInParent<ProjectileSpread>();
			if (componentInParent != null)
			{
				componentInParent.ParriedProjectile();
			}
		}
		proj.transform.SetParent(null, worldPositionStays: true);
	}

	public void BlastCheck()
	{
		if (MonoSingleton<InputManager>.Instance.InputSource.Punch.IsPressed)
		{
			holdingInput = false;
			anim.SetTrigger("PunchBlast");
			Vector3 position = MonoSingleton<CameraController>.Instance.GetDefaultPos() + MonoSingleton<CameraController>.Instance.transform.forward * 2f;
			if (Physics.Raycast(MonoSingleton<CameraController>.Instance.GetDefaultPos(), MonoSingleton<CameraController>.Instance.transform.forward, out var hitInfo, 2f, LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies)))
			{
				position = hitInfo.point - camObj.transform.forward * 0.1f;
			}
			Object.Instantiate(blastWave, position, MonoSingleton<CameraController>.Instance.transform.rotation);
		}
	}

	public void Eject()
	{
		if (ejectorAud == null)
		{
			ejectorAud = shellEjector.GetComponent<AudioSource>();
		}
		ejectorAud.Play();
		for (int i = 0; i < 2; i++)
		{
			GameObject gameObject = Object.Instantiate(shell, shellEjector.position + shellEjector.right * 0.075f, shellEjector.rotation);
			if (i == 1)
			{
				gameObject.transform.position = gameObject.transform.position - shellEjector.right * 0.15f;
			}
			gameObject.transform.Rotate(Vector3.forward, Random.Range(-45, 45), Space.Self);
			gameObject.GetComponent<Rigidbody>().AddForce((shellEjector.forward / 1.75f + shellEjector.up / 2f + Vector3.up / 1.75f) * Random.Range(8, 12), ForceMode.VelocityChange);
		}
	}

	private void hideFlash()
	{
		parryLight.enabled = false;
		parryFlash.SetActive(value: false);
		if (crowdReactions)
		{
			if (crorea == null)
			{
				crorea = MonoSingleton<CrowdReactions>.Instance;
			}
			if (crorea.enabled)
			{
				crorea.React(crorea.cheer);
			}
			else
			{
				crowdReactions = false;
			}
		}
	}

	public void Hide()
	{
	}

	public void ShopMode()
	{
		shopping = true;
		holdingInput = false;
		shopRequests++;
	}

	public void StopShop()
	{
		shopRequests--;
		if (shopRequests <= 0)
		{
			shopping = false;
		}
	}

	public void EquipAnimation()
	{
		if (anim == null)
		{
			anim = GetComponent<Animator>();
		}
		anim.SetTrigger("Equip");
	}

	private void AltHit(Transform target)
	{
		if (target.gameObject.layer == 22)
		{
			ItemIdentifier component = target.GetComponent<ItemIdentifier>();
			ItemPlaceZone[] components = target.GetComponents<ItemPlaceZone>();
			if (holding && components != null && components.Length != 0)
			{
				PlaceHeldObject(components, target);
			}
			else if (!holding && component != null)
			{
				ForceHold(component);
			}
		}
		if (holding)
		{
			heldItem.SendMessage("HitWith", target.gameObject, SendMessageOptions.DontRequireReceiver);
		}
	}
}
