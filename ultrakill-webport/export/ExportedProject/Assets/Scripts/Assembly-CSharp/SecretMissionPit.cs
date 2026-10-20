using UnityEngine;

public class SecretMissionPit : MonoBehaviour
{
	public int missionNumber;

	public bool halfUnlock;

	public bool primeMission;

	private void OnTriggerEnter(Collider other)
	{
		if (!(other.gameObject.tag == "Player"))
		{
			return;
		}
		if (primeMission)
		{
			if (halfUnlock)
			{
				GameProgressSaver.SetPrime(missionNumber, 1);
				Debug.Log("Help");
			}
			else
			{
				GameProgressSaver.SetPrime(missionNumber, 2);
			}
		}
		else if (halfUnlock)
		{
			GameProgressSaver.FoundSecretMission(missionNumber);
		}
		else
		{
			GameProgressSaver.SetSecretMission(missionNumber);
		}
	}
}
