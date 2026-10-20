using UnityEngine;

public class Grenade : MonoBehaviour
{
	public string hitterWeapon;

	public GameObject explosion;

	public GameObject harmlessExplosion;

	public GameObject superExplosion;

	private bool exploded;

	public bool enemy;

	public bool rocket;

	private Rigidbody rb;

	public GameObject ineffectiveSound;

	private void Awake()
	{
		if (!enemy)
		{
			Physics.IgnoreCollision(GetComponent<Collider>(), MonoSingleton<NewMovement>.Instance.GetComponent<Collider>(), ignore: true);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (exploded || (!enemy && !(collision.gameObject.tag != "Player")) || collision.transform.gameObject.layer == 14)
		{
			return;
		}
		bool flag = false;
		if (collision.gameObject.layer == 11)
		{
			EnemyIdentifier eid = collision.gameObject.GetComponent<EnemyIdentifierIdentifier>().eid;
			if (eid != null && !eid.dead && eid.enemyType == EnemyType.MaliciousFace)
			{
				flag = true;
			}
		}
		if (!flag && collision.gameObject.tag == "Armor")
		{
			flag = true;
		}
		if (flag)
		{
			LayerMask layerMask = 256;
			layerMask = (int)layerMask | 0x1000000;
			if (rb == null)
			{
				rb = GetComponent<Rigidbody>();
			}
			if (Physics.Raycast(base.transform.position - rb.velocity.normalized, base.transform.forward, out var hitInfo, float.PositiveInfinity, layerMask))
			{
				Vector3 velocity = rb.velocity;
				rb.velocity = Vector3.zero;
				rb.AddForce(Vector3.Reflect(velocity.normalized, hitInfo.normal) * velocity.magnitude * 2f, ForceMode.VelocityChange);
			}
			Object.Instantiate(ineffectiveSound, base.transform.position, Quaternion.identity).GetComponent<AudioSource>().volume = 0.75f;
			return;
		}
		bool harmless = false;
		bool big = false;
		bool super = false;
		if (rocket)
		{
			if (collision.gameObject.layer == 10 || collision.gameObject.layer == 11)
			{
				EnemyIdentifierIdentifier component = collision.gameObject.GetComponent<EnemyIdentifierIdentifier>();
				if ((bool)component && (bool)component.eid && !component.eid.dead && !component.eid.flying && (bool)component.eid.gce && !component.eid.gce.onGround)
				{
					super = true;
				}
				MonoSingleton<CameraController>.Instance.HitStop(0.05f);
			}
			else
			{
				harmless = true;
			}
		}
		else if (collision.gameObject.layer != 8)
		{
			MonoSingleton<CameraController>.Instance.HitStop(0.05f);
		}
		Explode(big, harmless, super);
	}

	public void Explode(bool big = false, bool harmless = false, bool super = false)
	{
		exploded = true;
		GameObject gameObject = (harmless ? Object.Instantiate(harmlessExplosion, base.transform.position, Quaternion.identity) : ((!super) ? Object.Instantiate(this.explosion, base.transform.position, Quaternion.identity) : Object.Instantiate(superExplosion, base.transform.position, Quaternion.identity)));
		Explosion[] componentsInChildren = gameObject.GetComponentsInChildren<Explosion>();
		foreach (Explosion explosion in componentsInChildren)
		{
			explosion.hitterWeapon = hitterWeapon;
			if (enemy)
			{
				explosion.enemy = true;
			}
			if (big)
			{
				explosion.maxSize *= 1.5f;
				explosion.speed *= 1.5f;
			}
		}
		Object.Destroy(base.gameObject);
	}
}
