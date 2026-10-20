using System.Collections.Generic;
using UnityEngine;

public class Magnet : MonoBehaviour
{
	private List<Rigidbody> affectedRbs = new List<Rigidbody>();

	private List<Rigidbody> removeRbs = new List<Rigidbody>();

	private List<EnemyIdentifier> eids = new List<EnemyIdentifier>();

	private List<Rigidbody> eidRbs = new List<Rigidbody>();

	public List<EnemyIdentifier> ignoredEids = new List<EnemyIdentifier>();

	public bool onEnemy;

	private SphereCollider col;

	public float strength;

	private LayerMask lmask;

	private RaycastHit rhit;

	public float maxWeight = 10f;

	private TimeBomb tb;

	private void Start()
	{
		col = GetComponent<SphereCollider>();
		lmask = (int)lmask | 0x400;
		lmask = (int)lmask | 0x800;
		tb = GetComponentInParent<TimeBomb>();
	}

	private void OnDestroy()
	{
		Launch();
	}

	public void Launch()
	{
		if (affectedRbs.Count == 0)
		{
			return;
		}
		List<Nail> list = new List<Nail>();
		foreach (Rigidbody affectedRb in affectedRbs)
		{
			if (!(affectedRb != null))
			{
				continue;
			}
			affectedRb.mass *= 2f;
			affectedRb.velocity = Vector3.zero;
			if (Physics.SphereCast(new Ray(affectedRb.transform.position, affectedRb.transform.position - base.transform.position), 5f, out rhit, 100f, lmask))
			{
				affectedRb.AddForce((rhit.point - affectedRb.transform.position).normalized * strength * 10f);
			}
			else
			{
				affectedRb.AddForce((base.transform.position - affectedRb.transform.position).normalized * strength * -10f);
			}
			if (affectedRb.TryGetComponent<Nail>(out var component))
			{
				component.MagnetRelease(this);
				if (component.magnets.Count == 0)
				{
					affectedRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
					list.Add(component);
				}
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		GameObject obj = new GameObject("NailBurstController");
		NailBurstController nailBurstController = obj.AddComponent<NailBurstController>();
		nailBurstController.nails = new List<Nail>(list);
		obj.AddComponent<RemoveOnTime>().time = 5f;
		foreach (Nail item in list)
		{
			item.nbc = nailBurstController;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer == 14 && other.gameObject.tag == "Metal")
		{
			Rigidbody component = other.gameObject.GetComponent<Rigidbody>();
			if (!(component != null) || affectedRbs.Contains(component))
			{
				return;
			}
			affectedRbs.Add(component);
			component.mass /= 2f;
			if (other.TryGetComponent<Nail>(out var component2))
			{
				component2.MagnetCaught(this);
				if (OptionsMenuToManager.simpleNailPhysics)
				{
					component.collisionDetectionMode = CollisionDetectionMode.Discrete;
				}
			}
		}
		else
		{
			if (other.gameObject.layer != 12)
			{
				return;
			}
			EnemyIdentifier component3 = other.gameObject.GetComponent<EnemyIdentifier>();
			if (component3 != null && !component3.bigEnemy && !eids.Contains(component3) && !ignoredEids.Contains(component3))
			{
				Rigidbody component4 = component3.GetComponent<Rigidbody>();
				if (component4 != null)
				{
					component4.mass /= 2f;
					eids.Add(component3);
					eidRbs.Add(component4);
				}
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.layer == 14 && other.gameObject.tag == "Metal")
		{
			Rigidbody component = other.gameObject.GetComponent<Rigidbody>();
			if (!(component != null) || !affectedRbs.Contains(component))
			{
				return;
			}
			affectedRbs.Remove(component);
			component.mass *= 2f;
			if (other.TryGetComponent<Nail>(out var component2))
			{
				component2.MagnetRelease(this);
				if (component2.magnets.Count == 0)
				{
					component.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
				}
			}
		}
		else if (other.gameObject.layer == 12)
		{
			EnemyIdentifier component3 = other.gameObject.GetComponent<EnemyIdentifier>();
			if (component3 != null && eids.Contains(component3))
			{
				int index = eids.IndexOf(component3);
				eids.RemoveAt(index);
				eidRbs[index].mass *= 2f;
				eidRbs.RemoveAt(index);
			}
		}
	}

	private void Update()
	{
		float num = 0f;
		if (affectedRbs.Count > 0)
		{
			foreach (Rigidbody affectedRb in affectedRbs)
			{
				if (affectedRb != null)
				{
					affectedRb.AddForce((base.transform.position - affectedRb.transform.position) * ((col.radius - Vector3.Distance(affectedRb.transform.position, base.transform.position)) / col.radius * strength * 50f * Time.deltaTime));
					num += affectedRb.mass;
				}
				else
				{
					removeRbs.Add(affectedRb);
				}
			}
			if (removeRbs.Count > 0)
			{
				foreach (Rigidbody removeRb in removeRbs)
				{
					affectedRbs.Remove(removeRb);
				}
				removeRbs.Clear();
			}
		}
		if (eids.Count > 0)
		{
			for (int i = 0; i < eids.Count; i++)
			{
				if (eids[i] != null && eidRbs[i] != null && !ignoredEids.Contains(eids[i]))
				{
					if (eids[i].nailsAmount > 0 && !eidRbs[i].isKinematic)
					{
						eids[i].useBrakes = false;
						eids[i].magneted = true;
						eidRbs[i].AddForce((base.transform.position - eidRbs[i].transform.position).normalized * ((col.radius - Vector3.Distance(eidRbs[i].transform.position, base.transform.position)) / col.radius * strength * (float)eids[i].nailsAmount * 5f * Time.deltaTime));
						num += eidRbs[i].mass;
					}
				}
				else
				{
					eids.RemoveAt(i);
					eidRbs.RemoveAt(i);
				}
			}
		}
		if (num > maxWeight)
		{
			Object.Destroy(tb.gameObject);
			return;
		}
		tb.beeperColor = Color.Lerp(Color.green, Color.red, num / maxWeight);
		tb.beeperPitch = num / maxWeight / 2f + 0.25f;
		tb.beeperSizeMultiplier = num / maxWeight + 1f;
	}
}
