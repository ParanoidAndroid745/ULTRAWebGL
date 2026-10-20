using UnityEngine;

public class TimeChallenge : MonoBehaviour
{
	public float time;

	private void Update()
	{
		if (MonoSingleton<StatsManager>.Instance.seconds >= time)
		{
			MonoSingleton<ChallengeManager>.Instance.challengeFailed = true;
		}
		else
		{
			MonoSingleton<ChallengeManager>.Instance.challengeDone = true;
		}
	}
}
