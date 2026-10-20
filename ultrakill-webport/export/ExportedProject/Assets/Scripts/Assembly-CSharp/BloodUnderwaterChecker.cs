using UnityEngine;

public class BloodUnderwaterChecker : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer != 4)
		{
			return;
		}
		Vector3 vector = new Vector3(base.transform.position.x, base.transform.position.y + 1.5f, base.transform.position.z);
		if (!(Vector3.Distance(other.ClosestPointOnBounds(vector), vector) < 0.5f))
		{
			return;
		}
		GameObject gore = MonoSingleton<BloodsplatterManager>.Instance.GetGore(GoreType.Body, isUnderwater: true);
		if ((bool)gore)
		{
			Bloodsplatter component = base.transform.parent.GetComponent<Bloodsplatter>();
			Bloodsplatter component2 = gore.GetComponent<Bloodsplatter>();
			component2.hpAmount = component.hpAmount;
			if (component.ready)
			{
				component2.GetReady();
			}
			gore.transform.position = base.transform.position;
			GoreZone componentInParent = GetComponentInParent<GoreZone>();
			if (componentInParent != null && componentInParent.goreZone != null)
			{
				gore.transform.SetParent(componentInParent.goreZone, worldPositionStays: true);
			}
			_ = component.GetComponent<ParticleSystem>().collision;
			gore.SetActive(value: true);
			base.transform.parent.gameObject.SetActive(value: false);
		}
	}
}
