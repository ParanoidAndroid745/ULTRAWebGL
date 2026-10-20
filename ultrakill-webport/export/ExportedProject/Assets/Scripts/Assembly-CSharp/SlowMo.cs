using UnityEngine;

public class SlowMo : MonoBehaviour
{
	public float amount;

	private void OnEnable()
	{
		CameraController instance = MonoSingleton<CameraController>.Instance;
		if (instance == null)
		{
			instance = MonoSingleton<CameraController>.Instance;
		}
		instance.SlowDown(amount);
		Object.Destroy(this);
	}
}
