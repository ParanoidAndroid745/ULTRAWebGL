using UnityEngine;
using UnityEngine.UI;

public class LevelStats : MonoBehaviour
{
	public Text levelName;

	private StatsManager sman;

	private bool ready;

	public Text time;

	public Text timeRank;

	private float seconds;

	private float minutes;

	public Text kills;

	public Text killsRank;

	public Text style;

	public Text styleRank;

	public Text secrets;

	private int currentSecrets;

	public Text challenge;

	public Text majorAssists;

	private void Start()
	{
		sman = MonoSingleton<StatsManager>.Instance;
		if ((bool)MonoSingleton<MapLoader>.Instance && MonoSingleton<MapLoader>.Instance.isCustomLoaded)
		{
			MapInfo instance = MapInfo.Instance;
			levelName.text = ((instance != null) ? instance.levelName : "???");
			ready = true;
			CheckStats();
		}
		RankData rankData = null;
		if (sman.levelNumber != 0 && !Debug.isDebugBuild)
		{
			rankData = GameProgressSaver.GetRank(sman.levelNumber);
		}
		if (sman.levelNumber != 0 && (Debug.isDebugBuild || (rankData != null && rankData.levelNumber == sman.levelNumber)))
		{
			StockMapInfo instance2 = StockMapInfo.Instance;
			if (instance2 != null)
			{
				levelName.text = instance2.assets.LargeText;
			}
			else
			{
				levelName.text = "???";
			}
			ready = true;
			CheckStats();
		}
		else
		{
			base.gameObject.SetActive(value: false);
		}
	}

	private void Update()
	{
		if (ready)
		{
			CheckStats();
		}
	}

	private void CheckStats()
	{
		seconds = sman.seconds;
		minutes = 0f;
		while (seconds >= 60f)
		{
			seconds -= 60f;
			minutes++;
		}
		time.text = minutes + ":" + seconds.ToString("00.000");
		timeRank.text = sman.GetRanks(sman.timeRanks, sman.seconds, reverse: true);
		kills.text = sman.kills.ToString();
		killsRank.text = sman.GetRanks(sman.killRanks, sman.kills, reverse: false);
		style.text = sman.stylePoints.ToString();
		styleRank.text = sman.GetRanks(sman.styleRanks, sman.stylePoints, reverse: false);
		int num = sman.secrets + sman.prevSecrets.Count;
		secrets.text = num + "/" + sman.secretObjects.Length;
		if (MonoSingleton<ChallengeManager>.Instance.challengeDone && !MonoSingleton<ChallengeManager>.Instance.challengeFailed)
		{
			challenge.text = "<color=#FFAF00>YES</color>";
		}
		else
		{
			challenge.text = "NO";
		}
		if (sman.majorUsed)
		{
			majorAssists.text = "<color=#4C99E6>YES</color>";
		}
		else
		{
			majorAssists.text = "NO";
		}
	}
}
