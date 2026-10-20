using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[Serializable]
public class RankData
{
	public int[] ranks;

	public int secretsAmount;

	public bool[] secretsFound;

	public bool challenge;

	public int levelNumber;

	public bool[] majorAssists;

	public RankData(StatsManager sman)
	{
		int num = PlayerPrefs.GetInt("Diff", 2);
		string path = string.Concat(Directory.GetParent(Application.dataPath), "/Saves/lvl", sman.levelNumber, "progress.bepis");
		levelNumber = sman.levelNumber;
		if (File.Exists(path))
		{
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			FileStream fileStream = new FileStream(path, FileMode.Open);
			RankData rankData = binaryFormatter.Deserialize(fileStream) as RankData;
			fileStream.Close();
			ranks = rankData.ranks;
			if (rankData.majorAssists != null)
			{
				majorAssists = rankData.majorAssists;
			}
			else
			{
				majorAssists = new bool[5];
			}
			if ((sman.rankScore >= rankData.ranks[num] && (rankData.majorAssists == null || (!sman.majorUsed && rankData.majorAssists[num]))) || sman.rankScore > rankData.ranks[num] || rankData.levelNumber != levelNumber)
			{
				majorAssists[num] = sman.majorUsed;
				ranks[num] = sman.rankScore;
			}
			secretsAmount = sman.secretObjects.Length;
			secretsFound = new bool[secretsAmount];
			for (int i = 0; i < secretsAmount; i++)
			{
				if (sman.secretObjects[i] == null || rankData.secretsFound[i])
				{
					secretsFound[i] = true;
				}
			}
			challenge = rankData.challenge;
			return;
		}
		ranks = new int[6];
		majorAssists = new bool[6];
		for (int j = 0; j < ranks.Length; j++)
		{
			ranks[j] = -1;
		}
		ranks[num] = sman.rankScore;
		majorAssists[num] = sman.majorUsed;
		secretsAmount = sman.secretObjects.Length;
		secretsFound = new bool[secretsAmount];
		for (int k = 0; k < secretsAmount; k++)
		{
			if (sman.secretObjects[k] == null)
			{
				secretsFound[k] = true;
			}
		}
	}
}
