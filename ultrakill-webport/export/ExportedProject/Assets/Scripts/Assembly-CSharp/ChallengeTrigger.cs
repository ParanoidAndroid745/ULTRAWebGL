using UnityEngine;

public class ChallengeTrigger : MonoBehaviour
{
	public ChallengeType type;

	public bool checkForNoEnemies;

	private void Awake()
	{
		if (type == ChallengeType.Fail)
		{
			MonoSingleton<ChallengeDoneByDefault>.Instance.Prepare();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.tag == "Player" && (!checkForNoEnemies || !CheatsController.IgnoreArenaTriggers))
		{
			Entered();
		}
	}

	public void Entered()
	{
		if (type == ChallengeType.Fail)
		{
			MonoSingleton<ChallengeManager>.Instance.ChallengeFailed();
		}
		else if (type == ChallengeType.Succeed)
		{
			MonoSingleton<ChallengeManager>.Instance.ChallengeDone();
		}
		Debug.Log("Player has entered " + base.gameObject.name);
	}
}
