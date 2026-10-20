using UnityEngine;

public class SubDoor : MonoBehaviour
{
	public Vector3 openPos;

	[HideInInspector]
	public Vector3 origPos;

	private Vector3 targetPos;

	public float speed;

	private AudioSource aud;

	private float origPitch;

	[HideInInspector]
	public Door dr;

	private void Start()
	{
		origPos = base.transform.localPosition;
		targetPos = origPos;
		aud = GetComponent<AudioSource>();
		if ((bool)aud)
		{
			origPitch = aud.pitch;
		}
	}

	private void Update()
	{
		if (!(base.transform.localPosition != targetPos))
		{
			return;
		}
		base.transform.localPosition = Vector3.MoveTowards(base.transform.localPosition, targetPos, Time.deltaTime * speed);
		if (base.transform.localPosition == targetPos)
		{
			if (targetPos == origPos)
			{
				dr?.BigDoorClosed();
			}
			aud.Stop();
		}
	}

	public void Open()
	{
		targetPos = origPos + openPos;
		if ((bool)aud && base.transform.localPosition != targetPos)
		{
			aud.pitch = origPitch + Random.Range(-0.1f, 0.1f);
			aud.Play();
		}
	}

	public void Close()
	{
		targetPos = origPos;
		if ((bool)aud && base.transform.localPosition != targetPos)
		{
			aud.pitch = origPitch + Random.Range(-0.1f, 0.1f);
			aud.Play();
		}
	}
}
