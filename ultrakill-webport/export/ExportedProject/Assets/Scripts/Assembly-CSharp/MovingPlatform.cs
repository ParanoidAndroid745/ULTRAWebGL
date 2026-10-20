using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
	[HideInInspector]
	public bool infoSet;

	public Vector3[] relativePoints;

	private Vector3 originalPosition;

	private Vector3 targetPosition;

	private int currentPoint;

	public float speed;

	public bool ease;

	public bool reverseAtEnd;

	private bool forward;

	private bool moving;

	public float startOffset;

	public float moveDelay;

	private AudioSource aud;

	private float origPitch;

	public AudioClip moveSound;

	public AudioClip stopSound;

	private void Start()
	{
		if (relativePoints.Length > 1 && !infoSet)
		{
			infoSet = true;
			aud = GetComponentInChildren<AudioSource>();
			if ((bool)aud)
			{
				origPitch = aud.pitch;
			}
			originalPosition = base.transform.position;
			Invoke("NextPoint", startOffset);
		}
	}

	private void FixedUpdate()
	{
		if (!moving)
		{
			return;
		}
		float num = speed;
		if (ease && Vector3.Distance(base.transform.position, targetPosition) > 5f)
		{
			num *= Vector3.Distance(base.transform.position, targetPosition) / 5f;
		}
		base.transform.position = Vector3.MoveTowards(base.transform.position, targetPosition, Time.deltaTime * num);
		if (!(Vector3.Distance(base.transform.position, targetPosition) < 0.1f))
		{
			return;
		}
		base.transform.position = targetPosition;
		moving = false;
		Invoke("NextPoint", moveDelay);
		if ((bool)aud)
		{
			aud.clip = stopSound;
			aud.loop = false;
			aud.pitch = origPitch + Random.Range(-0.05f, 0.05f);
			if (aud.clip != null)
			{
				aud.Play();
			}
		}
	}

	private void NextPoint()
	{
		int num = 1;
		if (!forward)
		{
			num = -1;
		}
		if ((forward && currentPoint < relativePoints.Length - 1) || (!forward && currentPoint > 0))
		{
			currentPoint += num;
		}
		else if (!reverseAtEnd)
		{
			currentPoint = 0;
		}
		else if (forward)
		{
			forward = false;
		}
		else
		{
			forward = true;
		}
		targetPosition = originalPosition + relativePoints[currentPoint];
		moving = true;
		if ((bool)aud)
		{
			aud.clip = moveSound;
			aud.loop = true;
			aud.pitch = origPitch + Random.Range(-0.05f, 0.05f);
			if (aud.clip != null)
			{
				aud.Play();
			}
		}
	}
}
