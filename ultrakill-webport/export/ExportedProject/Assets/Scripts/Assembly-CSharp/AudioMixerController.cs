using UnityEngine;
using UnityEngine.Audio;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
[DefaultExecutionOrder(-10)]
public class AudioMixerController : MonoSingleton<AudioMixerController>
{
	public AudioMixer allSound;

	public AudioMixer goreSound;

	public AudioMixer musicSound;

	public AudioMixer doorSound;

	[HideInInspector]
	public float musicVolume;

	public bool forceOff;

	protected override void Awake()
	{
		float num = (musicVolume = PlayerPrefs.GetFloat("MuVo", 0.6f));
		if (!forceOff)
		{
			if (num > 0f)
			{
				musicSound.SetFloat("allVolume", Mathf.Log10(num) * 20f);
			}
			else
			{
				musicSound.SetFloat("allVolume", -80f);
			}
		}
	}

	public void SetMusicVolume(float volume)
	{
		if (!forceOff)
		{
			musicSound.SetFloat("allVolume", CalculateMusicVolume(volume));
		}
		musicVolume = volume;
	}

	public float CalculateMusicVolume(float volume)
	{
		if (volume > 0f)
		{
			return Mathf.Log10(volume) * 20f;
		}
		return -80f;
	}
}
