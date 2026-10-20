using System;
using UnityEngine;

[Serializable]
public class GameProgressData
{
	public int levelNum;

	public int difficulty;

	public int[] primeLevels;

	public GameProgressData()
	{
		difficulty = PlayerPrefs.GetInt("Diff", 2);
	}
}
