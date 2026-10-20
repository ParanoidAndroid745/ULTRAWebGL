using System.Collections.Generic;
using UnityEngine;

public class GroundCheck : MonoBehaviour
{
	public bool slopeCheck;

	public bool onGround;

	public bool canJump;

	public bool heavyFall;

	public GameObject shockwave;

	public bool enemy;

	public float superJumpChance;

	public float extraJumpChance;

	private NewMovement nmov;

	private Collider col;

	private Collider currentCol;

	private Collider currentEnemyCol;

	public List<Rigidbody> movingRbs = new List<Rigidbody>();

	private void Start()
	{
		nmov = GetComponentInParent<NewMovement>();
	}

	private void Update()
	{
		if (superJumpChance > 0f)
		{
			superJumpChance = Mathf.MoveTowards(superJumpChance, 0f, Time.deltaTime);
			if (superJumpChance == 0f)
			{
				if (shockwave != null && nmov.stillHolding)
				{
					if (nmov.boostCharge >= 200f)
					{
						Object.Instantiate(shockwave, base.transform.position, Quaternion.identity).GetComponent<PhysicalShockwave>().force *= nmov.slamForce * 2.25f;
						if (!nmov.asscon.majorEnabled || !nmov.asscon.infiniteStamina)
						{
							nmov.boostCharge -= 200f;
						}
						nmov.cc.CameraShake(0.75f);
					}
					else
					{
						Object.Instantiate(nmov.staminaFailSound);
					}
				}
				extraJumpChance = 0.15f;
				nmov.stillHolding = false;
			}
		}
		if (extraJumpChance > 0f)
		{
			extraJumpChance = Mathf.MoveTowards(extraJumpChance, 0f, Time.deltaTime);
			if (extraJumpChance <= 0f)
			{
				nmov.slamForce = 0f;
			}
		}
		if (onGround && (currentCol == null || !currentCol.enabled || !currentCol.gameObject.activeInHierarchy || currentCol.gameObject.layer == 17 || currentCol.gameObject.layer == 10))
		{
			onGround = false;
		}
		if (canJump && (currentEnemyCol == null || !currentEnemyCol.gameObject.activeInHierarchy || Vector3.Distance(base.transform.position, currentEnemyCol.transform.position) > 40f))
		{
			canJump = false;
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (!other.gameObject.CompareTag("Slippery") && (other.gameObject.layer == 8 || other.gameObject.layer == 24 || other.gameObject.layer == 11 || other.gameObject.layer == 26) && (!enemy || other.gameObject.tag == "Floor" || other.gameObject.tag == "Wall" || other.gameObject.tag == "GlassFloor"))
		{
			if (currentCol == null)
			{
				currentCol = other;
			}
			onGround = true;
			canJump = false;
		}
		else if (!other.gameObject.CompareTag("Slippery") && other.gameObject.layer == 12)
		{
			currentEnemyCol = other;
			canJump = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!other.gameObject.CompareTag("Slippery") && (other.gameObject.layer == 8 || other.gameObject.layer == 24 || other.gameObject.layer == 11 || other.gameObject.layer == 26) && (!enemy || other.gameObject.tag == "Floor" || other.gameObject.tag == "Wall" || other.gameObject.tag == "GlassFloor"))
		{
			if (onGround)
			{
				onGround = false;
			}
			if (!slopeCheck && other.gameObject.CompareTag("Moving") && other.attachedRigidbody != null && movingRbs.Contains(other.attachedRigidbody))
			{
				movingRbs.Remove(other.attachedRigidbody);
				if (movingRbs.Count > 0)
				{
					nmov.transform.SetParent(movingRbs[movingRbs.Count - 1].transform, worldPositionStays: true);
				}
				else
				{
					nmov.transform.parent = null;
				}
				MonoSingleton<CameraController>.Instance.rotationY += other.transform.eulerAngles.y;
			}
		}
		else if (!other.gameObject.CompareTag("Slippery") && other.gameObject.layer == 12)
		{
			canJump = false;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.gameObject.CompareTag("Slippery") && (other.gameObject.layer == 8 || other.gameObject.layer == 24 || other.gameObject.layer == 11 || other.gameObject.layer == 26) && (!enemy || other.gameObject.tag == "Floor" || other.gameObject.tag == "Wall" || other.gameObject.tag == "GlassFloor"))
		{
			currentCol = other;
			if (!slopeCheck && other.gameObject.CompareTag("Moving") && other.attachedRigidbody != null && !movingRbs.Contains(other.attachedRigidbody))
			{
				movingRbs.Add(other.attachedRigidbody);
				MonoSingleton<CameraController>.Instance.rotationY -= other.transform.eulerAngles.y;
				nmov.transform.SetParent(other.attachedRigidbody.transform, worldPositionStays: true);
			}
		}
		if (heavyFall)
		{
			if (other.gameObject.layer == 10 || other.gameObject.layer == 11)
			{
				EnemyIdentifierIdentifier component = other.gameObject.GetComponent<EnemyIdentifierIdentifier>();
				if ((bool)component && (bool)component.eid)
				{
					component.eid.hitter = "ground slam";
					component.eid.DeliverDamage(other.gameObject, (base.transform.position - other.transform.position) * 5000f, other.transform.position, 2f, tryForExplode: true);
					if (!component.eid.exploded)
					{
						heavyFall = false;
					}
				}
			}
			else if (!other.gameObject.CompareTag("Slippery") && (other.gameObject.layer == 8 || other.gameObject.layer == 24))
			{
				Breakable component2 = other.gameObject.GetComponent<Breakable>();
				if (component2 != null && component2.weak)
				{
					component2.Break();
				}
				else
				{
					heavyFall = false;
				}
				superJumpChance = 0.075f;
			}
		}
		if (slopeCheck || other.gameObject.layer != 4 || !nmov.sliding || !(nmov.rb.velocity.y < 0f))
		{
			return;
		}
		Vector3 a = other.ClosestPoint(base.transform.position);
		if (MonoSingleton<UnderwaterController>.Instance.inWater || (!(Vector3.Distance(a, base.transform.position) < 0.1f) && Physics.Raycast(base.transform.position, Vector3.down, Vector3.Distance(a, base.transform.position), LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies), QueryTriggerInteraction.Collide)))
		{
			return;
		}
		nmov.rb.velocity = new Vector3(nmov.rb.velocity.x, 0f, nmov.rb.velocity.z);
		nmov.rb.AddForce(Vector3.up * 10f, ForceMode.VelocityChange);
		Water componentInParent = other.GetComponentInParent<Water>();
		if ((bool)componentInParent)
		{
			Object.Instantiate(componentInParent.smallSplash, base.transform.position, Quaternion.LookRotation(Vector3.up)).GetComponent<AudioSource>().volume = 0.65f;
			ChallengeTrigger component3 = componentInParent.GetComponent<ChallengeTrigger>();
			if ((bool)component3)
			{
				component3.Entered();
			}
		}
	}
}
