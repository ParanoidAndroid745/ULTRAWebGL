using UnityEngine;

public class MixerVolumeDip : MonoBehaviour
{
	private bool dipped;

	public float targetAmount;

	private void OnEnable()
	{
		if (!dipped)
		{
			dipped = true;
			MonoSingleton<AudioMixerController>.Instance.allSound.SetFloat("allVolume", targetAmount);
		}
	}

	private void OnDisable()
	{
		if (dipped)
		{
			dipped = false;
			if ((bool)MonoSingleton<AudioMixerController>.Instance)
			{
				MonoSingleton<AudioMixerController>.Instance.allSound.SetFloat("allVolume", targetAmount);
			}
		}
	}
}
