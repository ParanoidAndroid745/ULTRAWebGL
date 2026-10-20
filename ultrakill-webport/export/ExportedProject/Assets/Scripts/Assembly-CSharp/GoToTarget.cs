using UnityEngine;

public class GoToTarget : MonoBehaviour
{
	public ToDo onTargetReach;

	public float speed;

	public bool easeIn;

	public float easeInSpeed;

	private float currentSpeed;

	public Transform target;

	private Rigidbody rb;

	private bool stopped;

	public UltrakillEvent events;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void Update()
	{
		if (stopped)
		{
			return;
		}
		if (easeIn && currentSpeed != speed)
		{
			currentSpeed = Mathf.MoveTowards(currentSpeed, speed, Time.deltaTime * (Mathf.Abs(currentSpeed + 1f) * easeInSpeed));
		}
		else
		{
			currentSpeed = speed;
		}
		rb.velocity = (target.position - base.transform.position).normalized * currentSpeed * Time.deltaTime;
		if (Vector3.Distance(base.transform.position, target.position) < 0.5f)
		{
			switch (onTargetReach)
			{
			case ToDo.Disable:
				base.gameObject.SetActive(value: false);
				break;
			case ToDo.Destroy:
				Object.Destroy(base.gameObject);
				break;
			default:
				stopped = true;
				rb.velocity = Vector3.zero;
				break;
			}
			if (events != null)
			{
				events.Invoke();
			}
		}
	}
}
