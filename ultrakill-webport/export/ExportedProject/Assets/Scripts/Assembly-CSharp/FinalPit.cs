using UnityEngine;

public class FinalPit : MonoBehaviour
{
	private NewMovement nmov;

	private StatsManager sm;

	private Rigidbody rb;

	private bool rotationReady;

	private GameObject player;

	private bool infoSent;

	public bool rankless;

	public bool secondPit;

	public bool forcePrimeLevel;

	public string targetLevelName;

	private int levelNumber;

	public bool musicFadeOut;

	private Quaternion targetRotation;

	private void Start()
	{
		sm = MonoSingleton<StatsManager>.Instance;
		player = MonoSingleton<NewMovement>.Instance.gameObject;
		targetRotation = Quaternion.Euler(base.transform.rotation.eulerAngles + Vector3.up * 0.01f);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject == player && (bool)MonoSingleton<NewMovement>.Instance && MonoSingleton<NewMovement>.Instance.hp > 0)
		{
			if (musicFadeOut)
			{
				MonoSingleton<MusicManager>.Instance.off = true;
			}
			nmov = MonoSingleton<NewMovement>.Instance;
			rb = nmov.rb;
			nmov.activated = false;
			nmov.cc.activated = false;
			sm.HideShit();
			sm.StopTimer();
			if ((bool)MonoSingleton<PowerUpMeter>.Instance)
			{
				MonoSingleton<PowerUpMeter>.Instance.juice = 0f;
			}
			Invoke("SendInfo", 5f);
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (!(other.gameObject == player) || !MonoSingleton<NewMovement>.Instance || MonoSingleton<NewMovement>.Instance.hp <= 0)
		{
			return;
		}
		if (nmov == null)
		{
			nmov = other.gameObject.GetComponent<NewMovement>();
			rb = nmov.rb;
		}
		if (other.transform.position.x != base.transform.position.x || other.transform.position.z != base.transform.position.z)
		{
			Vector3 vector = new Vector3(base.transform.position.x, other.transform.position.y, base.transform.position.z);
			float num = Vector3.Distance(other.transform.position, vector);
			other.transform.position = Vector3.MoveTowards(other.transform.position, vector, 1f + num * Time.deltaTime);
			rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
		}
		if (!rotationReady)
		{
			nmov.cc.transform.rotation = Quaternion.RotateTowards(nmov.cc.transform.rotation, targetRotation, Time.fixedDeltaTime * 10f * (Quaternion.Angle(nmov.cc.transform.rotation, targetRotation) + 1f));
			if (Quaternion.Angle(nmov.cc.transform.rotation, targetRotation) < 0.01f)
			{
				nmov.cc.transform.rotation = targetRotation;
				rotationReady = true;
			}
		}
		if (rotationReady && !infoSent)
		{
			SendInfo();
		}
	}

	private void SendInfo()
	{
		CancelInvoke();
		if (infoSent)
		{
			return;
		}
		infoSent = true;
		if (!rankless)
		{
			levelNumber = MonoSingleton<StatsManager>.Instance.levelNumber;
			if (((bool)MonoSingleton<MapLoader>.Instance && MonoSingleton<MapLoader>.Instance.isCustomLoaded) || levelNumber >= 420)
			{
				GameProgressSaver.SaveProgress(0);
			}
			else
			{
				GameProgressSaver.SaveProgress(levelNumber + 1);
			}
			FinalRank fr = sm.fr;
			if (forcePrimeLevel)
			{
				StockMapInfo.Instance.nextAct = "prime";
			}
			fr.targetLevelName = targetLevelName;
			if (secondPit)
			{
				fr.finalPitPos = base.transform.position;
			}
			sm.SendInfo();
			if (secondPit)
			{
				fr.reachedSecondPit = true;
			}
		}
		else if (secondPit)
		{
			Debug.Log("SecondPit");
			GameProgressSaver.SetTutorial(beat: true);
			FinalRank fr2 = MonoSingleton<StatsManager>.Instance.fr;
			fr2.gameObject.SetActive(value: true);
			fr2.finalPitPos = base.transform.position;
			fr2.RanklessNextLevel(targetLevelName);
		}
	}
}
