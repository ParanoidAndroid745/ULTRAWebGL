using UnityEngine;

public class V2 : MonoBehaviour
{
	private Animator anim;

	private Transform target;

	private Rigidbody targetRb;

	private Vector3 targetPos;

	private Quaternion targetRot;

	public Transform[] aimAtTarget;

	private Rigidbody rb;

	private int currentWeapon;

	public SkinnedMeshRenderer smr;

	private Material wingMaterial;

	public Texture[] wingTextures;

	public GameObject wingChangeEffect;

	public Color[] wingColors;

	public GameObject[] weapons;

	private GameObject currentWingChangeEffect;

	private TrailRenderer[] wingTrails;

	private DragBehind[] drags;

	private int currentPattern;

	private bool inPattern;

	public LayerMask environmentMask;

	public GroundCheckEnemy gc;

	public GroundCheckEnemy wc;

	private int pattern1direction;

	public GameObject jumpSound;

	public GameObject dashJumpSound;

	public bool slowMode;

	public float movementSpeed;

	private float originalMovementSpeed;

	public float jumpPower;

	public float wallJumpPower;

	public float airAcceleration;

	public bool intro;

	[HideInInspector]
	public bool inIntro;

	public bool active;

	private bool running;

	private bool aiming;

	private bool sliding;

	private bool dodging;

	private bool jumping;

	private float patternCooldown;

	private float dodgeCooldown = 3f;

	private float dodgeLeft;

	public GameObject dodgeEffect;

	public GameObject slideEffect;

	private int difficulty;

	private float slideStopTimer;

	private float shootCooldown;

	private float altShootCooldown;

	public GameObject gunFlash;

	public GameObject altFlash;

	private bool aboutToShoot;

	private bool chargingAlt;

	private float predictAmount;

	private bool aimAtGround;

	public bool dontDie;

	public Transform escapeTarget;

	private bool escaping;

	private bool dead;

	public bool longIntro;

	private bool staringAtPlayer;

	private bool introHitGround;

	private EnemyIdentifierIdentifier[] eidids;

	private BossHealthBar bhb;

	public GameObject shockwave;

	public GameObject KoScream;

	private RaycastHit rhit;

	private float distancePatience;

	private bool enraged;

	public GameObject enrageEffect;

	private GameObject currentEnrageEffect;

	private Machine mac;

	private EnemyIdentifier eid;

	private bool drilled;

	private float circleTimer = 5f;

	public GameObject spawnOnDeath;

	private void Start()
	{
		anim = GetComponentInChildren<Animator>();
		target = MonoSingleton<CameraController>.Instance.transform;
		targetRb = MonoSingleton<NewMovement>.Instance.GetComponent<Rigidbody>();
		rb = GetComponent<Rigidbody>();
		gc = GetComponentInChildren<GroundCheckEnemy>();
		wingMaterial = smr.sharedMaterials[1];
		bhb = GetComponent<BossHealthBar>();
		mac = GetComponent<Machine>();
		eid = GetComponent<EnemyIdentifier>();
		if (MonoSingleton<StatueIntroChecker>.Instance.beenSeen)
		{
			longIntro = false;
		}
		if (!intro)
		{
			active = true;
			bhb.enabled = true;
		}
		else
		{
			inIntro = true;
			rb.AddForce(base.transform.forward * 20f, ForceMode.VelocityChange);
			anim.SetBool("InAir", value: true);
			anim.SetLayerWeight(1, 1f);
			anim.SetLayerWeight(2, 0f);
			if (longIntro)
			{
				eidids = GetComponentsInChildren<EnemyIdentifierIdentifier>();
				EnemyIdentifierIdentifier[] array = eidids;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].GetComponent<Collider>().enabled = false;
				}
				bhb.enabled = false;
			}
			else
			{
				bhb.enabled = true;
			}
		}
		running = true;
		aiming = true;
		inPattern = true;
		wingMaterial.mainTexture = wingTextures[0];
		wingTrails = GetComponentsInChildren<TrailRenderer>();
		drags = GetComponentsInChildren<DragBehind>();
		difficulty = PlayerPrefs.GetInt("Diff", 2);
		if (difficulty == 2)
		{
			movementSpeed *= 0.85f;
		}
		else if (difficulty == 1)
		{
			movementSpeed *= 0.75f;
		}
		else if (difficulty == 0)
		{
			movementSpeed *= 0.65f;
		}
		originalMovementSpeed = movementSpeed;
		ChangeDirection(Random.Range(-90f, 90f));
		SwitchPattern(0);
		shootCooldown = 1f;
		altShootCooldown = 5f;
	}

	private void Update()
	{
		if (active && !escaping)
		{
			if (CheatsController.BlindEnemies)
			{
				return;
			}
			targetPos = new Vector3(target.position.x, base.transform.position.y, target.position.z);
			if (dodging)
			{
				anim.SetBool("InAir", value: true);
				anim.SetLayerWeight(1, 1f);
				anim.SetLayerWeight(2, 0f);
				if (!drags[0].active)
				{
					DragBehind[] array = drags;
					for (int i = 0; i < array.Length; i++)
					{
						array[i].active = true;
					}
				}
			}
			else if (!gc.onGround)
			{
				anim.SetBool("InAir", value: true);
				anim.SetLayerWeight(1, 1f);
				anim.SetLayerWeight(2, 0f);
				if (!drags[0].active)
				{
					DragBehind[] array = drags;
					for (int i = 0; i < array.Length; i++)
					{
						array[i].active = true;
					}
				}
			}
			else if (running && !sliding)
			{
				anim.SetBool("InAir", value: false);
				anim.SetLayerWeight(1, 1f);
				if (anim.transform.rotation.eulerAngles.y > base.transform.rotation.eulerAngles.y)
				{
					anim.SetBool("RunningLeft", value: true);
				}
				else
				{
					anim.SetBool("RunningLeft", value: false);
				}
				float num = Quaternion.Angle(anim.transform.rotation, base.transform.rotation);
				if (num > 90f)
				{
					anim.SetBool("RunningBack", value: true);
				}
				else
				{
					anim.SetBool("RunningBack", value: false);
				}
				if (num <= 90f)
				{
					anim.SetLayerWeight(2, num / 90f);
				}
				else
				{
					anim.SetLayerWeight(2, Mathf.Abs(-180f + num) / 90f);
				}
				if (drags[0].active)
				{
					DragBehind[] array = drags;
					for (int i = 0; i < array.Length; i++)
					{
						array[i].active = false;
					}
				}
			}
			else
			{
				anim.SetBool("InAir", value: false);
				anim.SetLayerWeight(1, 0f);
				anim.SetLayerWeight(2, 0f);
				if (sliding && !drags[0].active)
				{
					DragBehind[] array = drags;
					for (int i = 0; i < array.Length; i++)
					{
						array[i].active = true;
					}
				}
				else if (!sliding && drags[0].active)
				{
					DragBehind[] array = drags;
					for (int i = 0; i < array.Length; i++)
					{
						array[i].active = false;
					}
				}
			}
			if (!sliding)
			{
				targetRot = Quaternion.LookRotation(targetPos - base.transform.position, Vector3.up);
				if (inPattern && currentPattern != 0)
				{
					if (currentPattern == 1)
					{
						Quaternion rotation = targetRot;
						rotation.eulerAngles = new Vector3(rotation.eulerAngles.x, rotation.eulerAngles.y + (float)(80 * pattern1direction), rotation.eulerAngles.z);
						base.transform.rotation = rotation;
					}
					else
					{
						base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, targetRot, Time.deltaTime * 350f);
						if (base.transform.rotation == targetRot && gc.onGround && !jumping && Vector3.Distance(base.transform.position, target.position) > 10f)
						{
							Slide();
						}
					}
				}
				anim.transform.rotation = Quaternion.RotateTowards(anim.transform.rotation, targetRot, Time.deltaTime * 10f * Quaternion.Angle(anim.transform.rotation, targetRot));
			}
			else
			{
				Quaternion a = Quaternion.LookRotation(base.transform.forward, Vector3.up);
				Quaternion b = Quaternion.LookRotation(targetPos - base.transform.position, Vector3.up);
				if (Quaternion.Angle(a, b) > 90f || (distancePatience >= 5f && Quaternion.Angle(a, b) > 45f))
				{
					slideStopTimer = Mathf.MoveTowards(slideStopTimer, 0f, Time.deltaTime);
					if (slideStopTimer <= 0f || enraged)
					{
						StopSlide();
					}
				}
			}
			if (dodgeCooldown < 6f)
			{
				if (difficulty > 2)
				{
					dodgeCooldown = Mathf.MoveTowards(dodgeCooldown, 6f, Time.deltaTime * 0.5f);
				}
				else
				{
					dodgeCooldown = Mathf.MoveTowards(dodgeCooldown, 6f, Time.deltaTime * 0.1f);
				}
			}
			if (dodgeLeft > 0f)
			{
				dodgeLeft = Mathf.MoveTowards(dodgeLeft, 0f, Time.deltaTime * 3f);
				if (dodgeLeft <= 0f)
				{
					dodging = false;
					inPattern = true;
					CheckPattern();
					if (currentPattern == 2)
					{
						Quaternion rotation2 = anim.transform.rotation;
						base.transform.LookAt(targetPos);
						anim.transform.rotation = rotation2;
					}
				}
			}
			if (patternCooldown > 0f)
			{
				patternCooldown = Mathf.MoveTowards(patternCooldown, 0f, Time.deltaTime);
			}
			if (inPattern)
			{
				_ = currentPattern;
				if (gc.onGround && !jumping)
				{
					if (Physics.Raycast(base.transform.position + Vector3.up, base.transform.forward, 4f, environmentMask))
					{
						Jump();
					}
				}
				else if (wc.onGround)
				{
					if (!gc.onGround && !jumping)
					{
						WallJump();
					}
					else if (gc.onGround)
					{
						ChangeDirection(Random.Range(100, 260));
					}
				}
				if (Vector3.Distance(base.transform.position, target.position) > 15f)
				{
					distancePatience = Mathf.MoveTowards(distancePatience, 12f, Time.deltaTime);
					if (distancePatience >= 4f && currentPattern != 2)
					{
						currentPattern = 2;
						SwitchPattern(2);
					}
					if (distancePatience == 12f && !enraged)
					{
						Enrage();
					}
				}
				else if (distancePatience > 0f)
				{
					if (!enraged)
					{
						distancePatience = Mathf.MoveTowards(distancePatience, 0f, Time.deltaTime * 2f);
					}
					else
					{
						distancePatience = Mathf.MoveTowards(distancePatience, 0f, Time.deltaTime);
					}
					if (enraged && distancePatience < 10f)
					{
						if (currentEnrageEffect != null)
						{
							Object.Destroy(currentEnrageEffect);
						}
						enraged = false;
						GetComponentInChildren<EnemySimplifier>().enraged = false;
						movementSpeed = originalMovementSpeed;
					}
				}
			}
			if (currentPattern == 1)
			{
				circleTimer = Mathf.MoveTowards(circleTimer, 0f, Time.deltaTime);
				if (circleTimer <= 0f)
				{
					CheckPattern();
				}
			}
			else
			{
				circleTimer = 5f;
			}
			float num2 = 1f;
			if (difficulty == 1)
			{
				num2 = 0.85f;
			}
			if (difficulty == 0)
			{
				num2 = 0.75f;
			}
			if (altShootCooldown > 0f)
			{
				altShootCooldown = Mathf.MoveTowards(altShootCooldown, 0f, Time.deltaTime * num2);
			}
			if (shootCooldown > 0f)
			{
				shootCooldown = Mathf.MoveTowards(shootCooldown, 0f, Time.deltaTime * num2);
			}
			else if (aiming)
			{
				if (!aboutToShoot)
				{
					if (Vector3.Distance(target.position, base.transform.position) > 15f)
					{
						SwitchWeapon(0);
					}
					else
					{
						SwitchWeapon(1);
					}
				}
				if (!Physics.Raycast(base.transform.position + Vector3.up * 2f, target.position - base.transform.position, out rhit, Vector3.Distance(base.transform.position, target.position), environmentMask))
				{
					if (altShootCooldown <= 0f || (distancePatience >= 8f && currentWeapon == 0))
					{
						if (currentWeapon == 0)
						{
							predictAmount = 0.15f;
						}
						else
						{
							aimAtGround = true;
							predictAmount = 0.25f;
						}
						if (difficulty > 2)
						{
							shootCooldown = Random.Range(1f, 2f);
						}
						else
						{
							shootCooldown = 2f;
						}
						altShootCooldown = 5f;
						weapons[currentWeapon].transform.GetChild(0).SendMessage("PrepareAltFire");
						aboutToShoot = true;
						chargingAlt = true;
						if (difficulty >= 2)
						{
							Invoke("AltShootWeapon", 1f);
						}
						else if (difficulty == 1)
						{
							Invoke("AltShootWeapon", 1.25f);
						}
						else
						{
							Invoke("AltShootWeapon", 1.5f);
						}
					}
					else
					{
						if (currentWeapon == 0)
						{
							if (distancePatience >= 4f)
							{
								shootCooldown = 1f;
							}
							if (difficulty > 2)
							{
								shootCooldown = Random.Range(1.5f, 2f);
							}
							else
							{
								shootCooldown = 2f;
							}
						}
						else
						{
							predictAmount = 0.15f;
							if (difficulty > 2)
							{
								shootCooldown = Random.Range(1.5f, 2f);
							}
							else
							{
								shootCooldown = 2f;
							}
						}
						weapons[currentWeapon].transform.GetChild(0).SendMessage("PrepareFire");
						aboutToShoot = true;
						if (currentWeapon == 0)
						{
							Object.Instantiate(gunFlash, aimAtTarget[1].transform.position, Quaternion.LookRotation(target.transform.position - aimAtTarget[1].transform.position)).transform.localScale *= 20f;
							if (difficulty >= 2)
							{
								Invoke("ShootWeapon", 0.75f);
							}
							if (difficulty >= 1)
							{
								Invoke("ShootWeapon", 0.95f);
							}
							Invoke("ShootWeapon", 1.15f);
						}
						else if (difficulty >= 2)
						{
							Invoke("ShootWeapon", 0.75f);
						}
						else if (difficulty == 1)
						{
							Invoke("ShootWeapon", 1f);
						}
						else
						{
							Invoke("ShootWeapon", 1.25f);
						}
					}
				}
				else if (altShootCooldown <= 0f && rhit.transform != null && rhit.transform.gameObject.tag == "Breakable")
				{
					predictAmount = 0f;
					aimAtGround = false;
					if (distancePatience >= 4f)
					{
						shootCooldown = 1f;
					}
					else if (difficulty > 2)
					{
						shootCooldown = Random.Range(1f, 2f);
					}
					else
					{
						shootCooldown = 2f;
					}
					altShootCooldown = 5f;
					weapons[currentWeapon].transform.GetChild(0).SendMessage("PrepareAltFire");
					aboutToShoot = true;
					chargingAlt = true;
					Invoke("AltShootWeapon", 1f);
				}
			}
			if ((bool)eid)
			{
				if (eid.drilled)
				{
					slowMode = true;
					drilled = true;
				}
				else if (drilled)
				{
					slowMode = false;
					drilled = false;
				}
			}
		}
		else
		{
			if (!inIntro)
			{
				return;
			}
			if (gc.onGround)
			{
				GameObject gameObject = null;
				if (longIntro)
				{
					rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
					if (!introHitGround)
					{
						anim.SetTrigger("Intro");
						introHitGround = true;
						anim.SetLayerWeight(1, 0f);
						anim.SetLayerWeight(2, 0f);
						gameObject = Object.Instantiate(shockwave, base.transform.position, Quaternion.identity);
					}
				}
				else
				{
					inIntro = false;
					active = true;
					bhb.enabled = true;
					gameObject = Object.Instantiate(shockwave, base.transform.position, Quaternion.identity);
				}
				if ((bool)gameObject && gameObject.TryGetComponent<PhysicalShockwave>(out var component))
				{
					component.enemyType = EnemyType.V2;
				}
			}
			if (staringAtPlayer)
			{
				targetPos = new Vector3(target.position.x, base.transform.position.y, target.position.z);
				targetRot = Quaternion.LookRotation(targetPos - base.transform.position, Vector3.up);
				anim.transform.rotation = Quaternion.RotateTowards(anim.transform.rotation, targetRot, Time.deltaTime * 10f * Quaternion.Angle(anim.transform.rotation, targetRot));
			}
		}
	}

	private void FixedUpdate()
	{
		if (escaping)
		{
			targetPos = new Vector3(escapeTarget.position.x, base.transform.position.y, escapeTarget.position.z);
			if (Vector3.Distance(base.transform.position, targetPos) > 8f)
			{
				aiming = false;
				inPattern = false;
				base.transform.LookAt(targetPos);
				anim.transform.LookAt(targetPos);
				rb.velocity = new Vector3(base.transform.forward.x * movementSpeed, rb.velocity.y, base.transform.forward.z * movementSpeed);
			}
			else
			{
				if (!jumping && gc.onGround)
				{
					Jump();
				}
				rb.velocity = targetPos - base.transform.position + Vector3.up * 50f;
				GetComponent<Collider>().enabled = false;
				if (base.transform.position.y > escapeTarget.position.y)
				{
					base.gameObject.SetActive(value: false);
					ActivateNextWave componentInParent = GetComponentInParent<ActivateNextWave>();
					if (componentInParent != null)
					{
						componentInParent.deadEnemies++;
					}
				}
			}
			if (base.transform.position.y > escapeTarget.position.y - 20f && spawnOnDeath != null)
			{
				spawnOnDeath.SetActive(value: true);
				spawnOnDeath.transform.position = base.transform.position;
				spawnOnDeath = null;
			}
		}
		else if (active && (inPattern || dodging) && running)
		{
			Move();
		}
	}

	private void ShootWeapon()
	{
		if (aiming)
		{
			weapons[currentWeapon].transform.GetChild(0).SendMessage("Fire");
			aboutToShoot = false;
			predictAmount = 0f;
			aimAtGround = false;
		}
	}

	private void AltShootWeapon()
	{
		if (aiming)
		{
			weapons[currentWeapon].transform.GetChild(0).SendMessage("AltFire");
			aboutToShoot = false;
			if (!enraged)
			{
				predictAmount = 0f;
			}
			aimAtGround = false;
			chargingAlt = false;
		}
	}

	private void Move()
	{
		if (dodging)
		{
			rb.velocity = new Vector3(base.transform.forward.x * (movementSpeed * 5f * dodgeLeft), 0f, base.transform.forward.z * (movementSpeed * 5f * dodgeLeft));
			return;
		}
		if (sliding)
		{
			if (distancePatience > 4f && !enraged)
			{
				rb.velocity = new Vector3(base.transform.forward.x * movementSpeed * 2f, rb.velocity.y, base.transform.forward.z * movementSpeed * 3f);
			}
			else
			{
				rb.velocity = new Vector3(base.transform.forward.x * movementSpeed * 2f, rb.velocity.y, base.transform.forward.z * movementSpeed * 2f);
			}
			return;
		}
		if (gc.onGround)
		{
			if (distancePatience > 4f && !enraged)
			{
				rb.velocity = new Vector3(base.transform.forward.x * movementSpeed, rb.velocity.y, base.transform.forward.z * movementSpeed * 1.5f);
			}
			else
			{
				rb.velocity = new Vector3(base.transform.forward.x * movementSpeed, rb.velocity.y, base.transform.forward.z * movementSpeed);
			}
			return;
		}
		Vector3 vector = (slowMode ? new Vector3(base.transform.forward.x * movementSpeed * Time.deltaTime * 1.25f, rb.velocity.y, base.transform.forward.z * movementSpeed * Time.deltaTime * 1.25f) : ((!(distancePatience > 4f) || enraged) ? new Vector3(base.transform.forward.x * movementSpeed * Time.deltaTime * 2.5f, rb.velocity.y, base.transform.forward.z * movementSpeed * Time.deltaTime * 2.5f) : new Vector3(base.transform.forward.x * movementSpeed * Time.deltaTime * 3f, rb.velocity.y, base.transform.forward.z * movementSpeed * Time.deltaTime * 3f)));
		Vector3 zero = Vector3.zero;
		if ((vector.x > 0f && rb.velocity.x < vector.x) || (vector.x < 0f && rb.velocity.x > vector.x))
		{
			zero.x = vector.x;
		}
		else
		{
			zero.x = 0f;
		}
		if ((vector.z > 0f && rb.velocity.z < vector.z) || (vector.z < 0f && rb.velocity.z > vector.z))
		{
			zero.z = vector.z;
		}
		else
		{
			zero.z = 0f;
		}
		rb.AddForce(zero.normalized * airAcceleration);
	}

	private void LateUpdate()
	{
		if (active && aiming)
		{
			if (difficulty <= 1)
			{
				predictAmount = 0f;
			}
			aimAtTarget[0].LookAt(target.position + targetRb.velocity * (Vector3.Distance(target.position, aimAtTarget[0].position) * predictAmount));
			aimAtTarget[0].Rotate(Vector3.right, 10f, Space.Self);
			Quaternion quaternion = ((!aimAtGround) ? Quaternion.LookRotation(target.position + targetRb.velocity * predictAmount - aimAtTarget[1].position, Vector3.up) : Quaternion.LookRotation(targetRb.transform.position + targetRb.velocity * predictAmount - aimAtTarget[1].position, Vector3.up));
			quaternion = Quaternion.Euler(quaternion.eulerAngles.x + 90f, quaternion.eulerAngles.y, quaternion.eulerAngles.z);
			aimAtTarget[1].rotation = quaternion;
			aimAtTarget[1].Rotate(Vector3.up, 180f, Space.Self);
		}
	}

	private void Jump()
	{
		jumping = true;
		anim.SetLayerWeight(1, 1f);
		anim.SetLayerWeight(2, 0f);
		anim.SetTrigger("Jump");
		Invoke("NotJumping", 0.25f);
		if (sliding)
		{
			Object.Instantiate(jumpSound, base.transform.position, Quaternion.identity);
			if (slowMode)
			{
				rb.AddForce(Vector3.up * jumpPower * 1500f);
			}
			else
			{
				rb.AddForce(Vector3.up * jumpPower * 1500f * 2f);
			}
			StopSlide();
		}
		else if (dodging)
		{
			Object.Instantiate(dashJumpSound);
			if (slowMode)
			{
				rb.AddForce(Vector3.up * jumpPower * 1500f * 0.75f);
			}
			else
			{
				rb.AddForce(Vector3.up * jumpPower * 1500f * 1.5f);
			}
		}
		else
		{
			Object.Instantiate(jumpSound, base.transform.position, Quaternion.identity);
			if (slowMode)
			{
				rb.AddForce(Vector3.up * jumpPower * 1500f * 1.25f);
			}
			else
			{
				rb.AddForce(Vector3.up * jumpPower * 1500f * 2.5f);
			}
		}
	}

	private void WallJump()
	{
		if (sliding)
		{
			StopSlide();
		}
		jumping = true;
		Invoke("NotJumping", 0.25f);
		Object.Instantiate(jumpSound, base.transform.position, Quaternion.identity).GetComponent<AudioSource>().pitch = 2f;
		Vector3 vector = base.transform.position - wc.ClosestPoint();
		rb.velocity = Vector3.zero;
		Vector3 vector2 = new Vector3(vector.normalized.x * 3f, 0.75f, vector.normalized.z * 3f);
		CheckPattern();
		if (currentPattern == 0)
		{
			Quaternion rotation = anim.transform.rotation;
			Vector3 forward = new Vector3(vector.normalized.x, 0f, vector.normalized.z);
			base.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
			anim.transform.rotation = rotation;
			ChangeDirection(Random.Range(-90, 90));
		}
		else if (currentPattern == 1)
		{
			if (pattern1direction < 0)
			{
				pattern1direction = 1;
			}
			else
			{
				pattern1direction = -1;
			}
		}
		else
		{
			Quaternion rotation2 = anim.transform.rotation;
			base.transform.LookAt(targetPos);
			anim.transform.rotation = rotation2;
		}
		float num = 2000f;
		if (difficulty == 1)
		{
			num = 1000f;
		}
		else if (difficulty == 0)
		{
			num = 500f;
		}
		rb.AddForce(vector2 * wallJumpPower * num);
	}

	private void CheckPattern()
	{
		if (!(patternCooldown <= 0f) || !(distancePatience < 4f))
		{
			return;
		}
		int num = currentPattern;
		currentPattern = Random.Range(0, 3);
		if (num == currentPattern)
		{
			patternCooldown = Random.Range(0.5f, 1f);
			if (currentPattern == 1)
			{
				circleTimer++;
			}
		}
		else
		{
			patternCooldown = Random.Range(2, 5);
			SwitchPattern(currentPattern);
		}
		if (currentPattern == 1 && Random.Range(0f, 1f) > 0.5f)
		{
			pattern1direction = -1;
		}
		else
		{
			pattern1direction = 1;
		}
	}

	private void ChangeDirection(float degrees)
	{
		Quaternion rotation = anim.transform.rotation;
		base.transform.Rotate(base.transform.up, degrees, Space.World);
		anim.transform.rotation = rotation;
	}

	public void Dodge(Transform projectile)
	{
		if (!active || !(dodgeLeft <= 0f) || chargingAlt || !(Vector3.Distance(base.transform.position, target.position) > 15f))
		{
			return;
		}
		if (sliding)
		{
			StopSlide();
		}
		if (dodgeCooldown >= (float)(6 - difficulty))
		{
			Debug.Log("Dodged");
			dodgeCooldown -= 6 - difficulty;
			dodgeLeft = 1f;
			dodging = true;
			inPattern = false;
			Object.Instantiate(dodgeEffect, base.transform.position + Vector3.up * 2f, base.transform.rotation);
			Vector3 vector = new Vector3(base.transform.position.x - projectile.position.x, 0f, base.transform.position.z - projectile.position.z);
			if (currentPattern == 2)
			{
				vector = vector.normalized + (targetPos - base.transform.position).normalized;
			}
			base.transform.LookAt(base.transform.position + vector);
			if (Random.Range(0f, 1f) > 0.5f)
			{
				ChangeDirection(90f);
			}
			else
			{
				ChangeDirection(-90f);
			}
			anim.SetTrigger("Jump");
		}
		else
		{
			if (!gc.onGround || jumping)
			{
				return;
			}
			float num = ((difficulty <= 2) ? Random.Range(0f, 3f) : Random.Range(0f, 2f));
			if (num < 1f)
			{
				if (num > 0.75f)
				{
					Jump();
				}
				else
				{
					Slide();
				}
			}
		}
	}

	private void NotJumping()
	{
		jumping = false;
	}

	private void Slide()
	{
		anim.SetBool("Sliding", value: true);
		sliding = true;
		slideEffect.SetActive(value: true);
		slideStopTimer = 0.2f;
	}

	private void StopSlide()
	{
		sliding = false;
		anim.SetBool("Sliding", value: false);
		slideEffect.SetActive(value: false);
		CheckPattern();
	}

	private void SwitchWeapon(int weapon)
	{
		if (currentWeapon != weapon)
		{
			currentWeapon = weapon;
			GameObject[] array = weapons;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(value: false);
			}
			weapons[weapon].SetActive(value: true);
		}
	}

	public void SwitchPattern(int pattern)
	{
		if (currentWingChangeEffect != null)
		{
			Object.Destroy(currentWingChangeEffect);
		}
		wingMaterial.mainTexture = wingTextures[pattern];
		currentWingChangeEffect = Object.Instantiate(wingChangeEffect, base.transform.position + Vector3.up * 2f, Quaternion.identity);
		currentWingChangeEffect.GetComponent<Light>().color = wingColors[pattern];
		TrailRenderer[] array = wingTrails;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].startColor = new Color(wingColors[pattern].r, wingColors[pattern].g, wingColors[pattern].b, 0.5f);
		}
		switch (pattern)
		{
		case 0:
			currentWingChangeEffect.GetComponent<AudioSource>().pitch = 1.5f;
			break;
		case 1:
			currentWingChangeEffect.GetComponent<AudioSource>().pitch = 1.25f;
			break;
		}
	}

	public void Die()
	{
		if (dontDie && !dead)
		{
			dead = true;
			active = false;
			inPattern = false;
			aiming = false;
			inIntro = false;
			anim.transform.LookAt(new Vector3(target.transform.position.x, anim.transform.position.y, target.transform.position.z));
			anim.SetTrigger("KnockedDown");
			anim.SetLayerWeight(1, 0f);
			anim.SetLayerWeight(2, 0f);
			rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
			Object.Instantiate(KoScream, base.transform.position, Quaternion.identity);
			MonoSingleton<MusicManager>.Instance.off = true;
			eidids = GetComponentsInChildren<EnemyIdentifierIdentifier>();
			EnemyIdentifierIdentifier[] array = eidids;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].GetComponent<Collider>().enabled = false;
			}
		}
	}

	public void IntroEnd()
	{
		inIntro = false;
		active = true;
		staringAtPlayer = false;
		EnemyIdentifierIdentifier[] array = eidids;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].GetComponent<Collider>().enabled = true;
		}
		bhb.enabled = true;
		longIntro = false;
		MonoSingleton<StatueIntroChecker>.Instance.beenSeen = true;
	}

	public void StareAtPlayer()
	{
		staringAtPlayer = true;
	}

	public void BeginEscape()
	{
		escaping = true;
		anim.SetLayerWeight(1, 1f);
		anim.SetLayerWeight(2, 1f);
	}

	public void Enrage()
	{
		if (!enraged)
		{
			enraged = true;
			currentEnrageEffect = Object.Instantiate(enrageEffect, mac.chest.transform.position, base.transform.rotation);
			currentEnrageEffect.transform.SetParent(mac.chest.transform, worldPositionStays: true);
			GetComponentInChildren<EnemySimplifier>().enraged = true;
			movementSpeed = originalMovementSpeed * 2f;
		}
	}
}
