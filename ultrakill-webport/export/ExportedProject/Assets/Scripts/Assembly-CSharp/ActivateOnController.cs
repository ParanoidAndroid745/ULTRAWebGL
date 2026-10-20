using UnityEngine;
using UnityEngine.InputSystem;

public class ActivateOnController : MonoBehaviour
{
	public UltrakillEvent onController;

	private bool activated;

	private void Start()
	{
		Check();
	}

	private void Update()
	{
		Check();
	}

	private void Check()
	{
		if (activated && (Gamepad.current == null || !Gamepad.current.enabled))
		{
			activated = false;
			onController.Revert();
		}
		else if (!activated && Gamepad.current != null && Gamepad.current.enabled)
		{
			activated = true;
			onController.Invoke();
		}
	}
}
