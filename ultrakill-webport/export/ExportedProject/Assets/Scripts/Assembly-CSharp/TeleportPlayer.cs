using UnityEngine;

public class TeleportPlayer : MonoBehaviour
{
	public bool affectPosition = true;

	public Vector3 relativePosition;

	public bool notRelative;

	public bool relativeToCollider;

	public Vector3 objectivePosition;

	public bool affectRotation;

	public bool notRelativeRotation;

	public Vector2 rotationDelta;

	public Vector2 objectiveRotation;

	public UltrakillEvent onTeleportPlayer;

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			PerformTheTeleport(other.transform);
		}
	}

	public void PerformTheTeleport()
	{
		PerformTheTeleport(MonoSingleton<NewMovement>.Instance.transform);
	}

	private void PerformTheTeleport(Transform target)
	{
		if (affectPosition)
		{
			if (notRelative)
			{
				target.position = objectivePosition;
			}
			else if (relativeToCollider)
			{
				target.position = base.transform.position + relativePosition;
			}
			else
			{
				target.position += relativePosition;
			}
		}
		if (affectRotation)
		{
			if (notRelativeRotation)
			{
				MonoSingleton<CameraController>.Instance.rotationY = objectiveRotation.y;
				MonoSingleton<CameraController>.Instance.rotationX = objectiveRotation.x;
			}
			else
			{
				MonoSingleton<CameraController>.Instance.rotationY += rotationDelta.y;
				MonoSingleton<CameraController>.Instance.rotationX += rotationDelta.x;
			}
		}
		onTeleportPlayer.Invoke();
	}
}
