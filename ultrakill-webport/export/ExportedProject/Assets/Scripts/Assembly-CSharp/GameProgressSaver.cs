using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class GameProgressSaver
{
	private static BinaryFormatter _formatter;

	private static Dictionary<string, object> fileCache = new Dictionary<string, object>();

	private static BinaryFormatter formatter
	{
		get
		{
			if (_formatter != null)
			{
				return _formatter;
			}
			_formatter = new BinaryFormatter();
			return _formatter;
		}
	}

	private static string savesPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Saves");

	private static string currentDifficultyPath => Path.Combine(savesPath, string.Format("difficulty{0}progress.bepis", PlayerPrefs.GetInt("Diff", 2)));

	private static string customSavesDir => Path.Combine(savesPath, "Custom");

	private static string generalProgressPath => Path.Combine(savesPath, "generalprogress.bepis");

	private static string cyberGrindHighScorePath => Path.Combine(savesPath, "cybergrindhighscore.bepis");

	private static string customLevelProgressPath => Path.Combine(customSavesDir, MonoSingleton<MapLoader>.Instance.currentCustomId + ".bepis");

	private static string resolveCurrentLevelPath
	{
		get
		{
			if (!MonoSingleton<MapLoader>.Instance || !MonoSingleton<MapLoader>.Instance.isCustomLoaded)
			{
				return Path.Combine(savesPath, $"lvl{MonoSingleton<StatsManager>.Instance.levelNumber}progress.bepis");
			}
			return customLevelProgressPath;
		}
	}

	private static GameProgressMoneyAndGear newProgressMoneyAndGear => new GameProgressMoneyAndGear
	{
		secretMissions = new int[10],
		enemiesFound = new bool[Enum.GetValues(typeof(EnemyType)).Length]
	};

	private static void CreateSavesDirectory()
	{
		if (!Directory.Exists(savesPath))
		{
			Directory.CreateDirectory(savesPath);
		}
		if ((bool)MonoSingleton<MapLoader>.Instance && MonoSingleton<MapLoader>.Instance.isCustomLoaded && !Directory.Exists(customSavesDir))
		{
			Directory.CreateDirectory(customSavesDir);
		}
	}

	private static FileStream GetOpenStreamSafe(string path)
	{
		string path2 = path + "-temp";
		if (File.Exists(path2))
		{
			Debug.Log("Reading temp file instead. " + path);
			FileStream fileStream = new FileStream(path2, FileMode.Open);
			if (fileStream.Length == 0L)
			{
				fileStream.Close();
				MonoSingleton<ProgressChecker>.Instance.SaveLoadError(SaveLoadFailMessage.SaveLoadError.TempValidation, "RECOVERY_TEMP_EMPTY", () =>
				{
					MonoSingleton<OptionsManager>.Instance.RestartMission();
				});
				Debug.LogError("Failed to recover save file from temp!");
				return null;
			}
			return fileStream;
		}
		if (!File.Exists(path))
		{
			return null;
		}
		return new FileStream(path, FileMode.Open);
	}

	private static GameProgressData ReadGameProgress(string path)
	{
		if (fileCache.ContainsKey(path))
		{
			return (GameProgressData)fileCache[path];
		}
		CreateSavesDirectory();
		try
		{
			FileStream openStreamSafe = GetOpenStreamSafe(path);
			if (openStreamSafe == null)
			{
				return null;
			}
			GameProgressData gameProgressData = formatter.Deserialize(openStreamSafe) as GameProgressData;
			fileCache.Add(path, gameProgressData);
			openStreamSafe.Close();
			return gameProgressData;
		}
		catch
		{
			Debug.Log("Save Failed");
			MonoSingleton<ProgressChecker>.Instance.SaveLoadError();
			return null;
		}
	}

	private static GameProgressMoneyAndGear ReadGameProgressMoneyAndGear()
	{
		if (fileCache.ContainsKey(generalProgressPath))
		{
			return (GameProgressMoneyAndGear)fileCache[generalProgressPath];
		}
		CreateSavesDirectory();
		try
		{
			FileStream openStreamSafe = GetOpenStreamSafe(generalProgressPath);
			if (openStreamSafe == null)
			{
				return null;
			}
			GameProgressMoneyAndGear gameProgressMoneyAndGear = formatter.Deserialize(openStreamSafe) as GameProgressMoneyAndGear;
			fileCache.Add(generalProgressPath, gameProgressMoneyAndGear);
			openStreamSafe.Close();
			return gameProgressMoneyAndGear;
		}
		catch
		{
			Debug.Log("Save Failed");
			MonoSingleton<ProgressChecker>.Instance.SaveLoadError();
			return null;
		}
	}

	private static CyberRankData ReadGameCyberRankData(string path)
	{
		CreateSavesDirectory();
		try
		{
			FileStream openStreamSafe = GetOpenStreamSafe(path);
			if (openStreamSafe == null)
			{
				return null;
			}
			CyberRankData cyberRankData = formatter.Deserialize(openStreamSafe) as CyberRankData;
			openStreamSafe.Close();
			if (cyberRankData == null)
			{
				return null;
			}
			if (cyberRankData.wavesByDifficulty != null)
			{
				int[] wavesByDifficulty = cyberRankData.wavesByDifficulty;
				for (int i = 0; i < wavesByDifficulty.Length; i++)
				{
					Debug.Log(wavesByDifficulty[i]);
				}
				cyberRankData.preciseWavesByDifficulty = new float[6];
				cyberRankData.preciseWavesByDifficulty = ((IEnumerable<int>)cyberRankData.wavesByDifficulty).Select((Func<int, float>)((int oldWave) => oldWave)).ToArray();
				cyberRankData.wavesByDifficulty = null;
			}
			if (cyberRankData.preciseWavesByDifficulty == null || cyberRankData.preciseWavesByDifficulty.Length != 6)
			{
				cyberRankData.preciseWavesByDifficulty = new float[6];
			}
			if (cyberRankData.style == null || cyberRankData.style.Length != 6)
			{
				cyberRankData.style = new int[6];
			}
			if (cyberRankData.kills == null || cyberRankData.kills.Length != 6)
			{
				cyberRankData.kills = new int[6];
			}
			if (cyberRankData.time == null || cyberRankData.time.Length != 6)
			{
				cyberRankData.time = new float[6];
			}
			if (cyberRankData.wave != 0)
			{
				for (int num = 0; num < cyberRankData.preciseWavesByDifficulty.Length; num++)
				{
					cyberRankData.preciseWavesByDifficulty[num] = cyberRankData.wave;
				}
				cyberRankData.wave = 0;
			}
			return cyberRankData;
		}
		catch
		{
			Debug.Log("Save Failed");
			MonoSingleton<ProgressChecker>.Instance.SaveLoadError();
			return null;
		}
	}

	private static bool SerializeWrite(string path, object graph)
	{
		if (fileCache.ContainsKey(path))
		{
			fileCache[path] = graph;
		}
		else
		{
			fileCache.Add(path, graph);
		}
		string text = path + "-temp";
		if (File.Exists(text))
		{
			File.Delete(text);
		}
		FileStream fileStream = new FileStream(text, FileMode.Create);
		formatter.Serialize(fileStream, graph);
		fileStream.Close();
		if (!File.Exists(text))
		{
			MonoSingleton<ProgressChecker>.Instance.SaveLoadError(SaveLoadFailMessage.SaveLoadError.TempValidation, "CREATE_TEMP", () =>
			{
				string path2 = path;
				object graph2 = graph;
				SerializeWrite(path2, graph2);
			});
			Debug.LogError("Failed to create temp save file!");
			return false;
		}
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		if (File.Exists(path))
		{
			MonoSingleton<ProgressChecker>.Instance.SaveLoadError(SaveLoadFailMessage.SaveLoadError.TempValidation, "REMOVE_TARGET", () =>
			{
				string path2 = path;
				object graph2 = graph;
				SerializeWrite(path2, graph2);
			});
			Debug.LogError("Failed to remove the target save file!");
			return false;
		}
		File.Move(text, path);
		if (!File.Exists(path))
		{
			MonoSingleton<ProgressChecker>.Instance.SaveLoadError(SaveLoadFailMessage.SaveLoadError.TempValidation, "MOVE_TEMP", () =>
			{
				string path2 = path;
				object graph2 = graph;
				SerializeWrite(path2, graph2);
			});
			Debug.LogError("Failed to move the temp file!");
		}
		return true;
	}

	public static void FlushCache()
	{
		fileCache = new Dictionary<string, object>();
	}

	public static void SaveRank()
	{
		bool num = (bool)MonoSingleton<MapLoader>.Instance && MonoSingleton<MapLoader>.Instance.isCustomLoaded;
		string path = savesPath + "/Custom";
		if (num && !Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		SerializeWrite(resolveCurrentLevelPath, new RankData(MonoSingleton<StatsManager>.Instance));
	}

	public static RankData GetRank(int levelNumber)
	{
		string text = Path.Combine(savesPath, $"lvl{levelNumber}progress.bepis");
		if (fileCache.ContainsKey(text))
		{
			return (RankData)fileCache[text];
		}
		FileStream openStreamSafe = GetOpenStreamSafe(text);
		if (openStreamSafe != null)
		{
			RankData rankData = formatter.Deserialize(openStreamSafe) as RankData;
			fileCache.Add(text, rankData);
			openStreamSafe.Close();
			return rankData;
		}
		return null;
	}

	public static void ChallengeComplete()
	{
		string path = resolveCurrentLevelPath;
		FileStream openStreamSafe = GetOpenStreamSafe(path);
		if (openStreamSafe != null)
		{
			RankData rankData = formatter.Deserialize(openStreamSafe) as RankData;
			openStreamSafe.Close();
			if (rankData == null)
			{
				rankData = new RankData(MonoSingleton<StatsManager>.Instance)
				{
					challenge = true
				};
				SerializeWrite(path, rankData);
			}
			else if (!rankData.challenge && (rankData.levelNumber == MonoSingleton<StatsManager>.Instance.levelNumber || ((bool)MonoSingleton<MapLoader>.Instance && MonoSingleton<MapLoader>.Instance.isCustomLoaded)))
			{
				rankData.challenge = true;
				SerializeWrite(path, rankData);
			}
		}
		else
		{
			RankData rankData = new RankData(MonoSingleton<StatsManager>.Instance)
			{
				challenge = true
			};
			SerializeWrite(path, rankData);
		}
	}

	public static void SecretFound(int secretNum)
	{
		string path = resolveCurrentLevelPath;
		FileStream openStreamSafe = GetOpenStreamSafe(path);
		if (openStreamSafe != null)
		{
			RankData rankData = formatter.Deserialize(openStreamSafe) as RankData;
			openStreamSafe.Close();
			if ((rankData.levelNumber == MonoSingleton<StatsManager>.Instance.levelNumber || MonoSingleton<MapLoader>.Instance.isCustomLoaded) && !rankData.secretsFound[secretNum])
			{
				rankData.secretsFound[secretNum] = true;
				SerializeWrite(path, rankData);
			}
		}
		else
		{
			RankData rankData = new RankData(MonoSingleton<StatsManager>.Instance);
			bool[] array = new bool[MonoSingleton<StatsManager>.Instance.secretObjects.Length];
			array[secretNum] = true;
			rankData.secretsFound = array;
			SerializeWrite(path, rankData);
		}
	}

	public static void SaveProgress(int levelNum)
	{
		GameProgressData gameProgressData = ReadGameProgress(currentDifficultyPath);
		if (gameProgressData != null)
		{
			if (gameProgressData.levelNum < levelNum || gameProgressData.difficulty != PlayerPrefs.GetInt("Diff", 2))
			{
				GameProgressData gameProgressData2 = new GameProgressData();
				gameProgressData2.levelNum = levelNum;
				SerializeWrite(currentDifficultyPath, gameProgressData2);
			}
			return;
		}
		try
		{
			GameProgressData gameProgressData3 = new GameProgressData();
			gameProgressData3.levelNum = levelNum;
			SerializeWrite(currentDifficultyPath, gameProgressData3);
		}
		catch
		{
			MonoSingleton<ProgressChecker>.Instance.SaveLoadError();
		}
	}

	public static int GetProgress(int difficulty)
	{
		if ((bool)MonoSingleton<MapLoader>.Instance && MonoSingleton<MapLoader>.Instance.isCustomLoaded)
		{
			return 1;
		}
		int num = 1;
		for (int i = difficulty; i <= 5; i++)
		{
			GameProgressData gameProgressData = ReadGameProgress(Path.Combine(savesPath, $"difficulty{i}progress.bepis"));
			if (gameProgressData != null && gameProgressData.difficulty == i && gameProgressData.levelNum > num)
			{
				num = gameProgressData.levelNum;
			}
		}
		return num;
	}

	public static void SetPrime(int level, int state)
	{
		level--;
		GameProgressData gameProgressData = ReadGameProgress(currentDifficultyPath);
		if (gameProgressData == null)
		{
			gameProgressData = new GameProgressData();
		}
		if (gameProgressData.primeLevels == null || gameProgressData.primeLevels.Length == 0)
		{
			gameProgressData.primeLevels = new int[3];
		}
		if (level < gameProgressData.primeLevels.Length)
		{
			if (state <= gameProgressData.primeLevels[level])
			{
				return;
			}
			gameProgressData.primeLevels[level] = state;
		}
		else
		{
			int[] array = new int[3];
			for (int i = 0; i < gameProgressData.primeLevels.Length; i++)
			{
				array[i] = gameProgressData.primeLevels[i];
			}
			for (int j = gameProgressData.primeLevels.Length; j < array.Length; j++)
			{
				if (j == level)
				{
					array[j] = state;
				}
				else
				{
					array[j] = 0;
				}
			}
			gameProgressData.primeLevels = array;
		}
		SerializeWrite(currentDifficultyPath, gameProgressData);
	}

	public static int GetPrime(int difficulty, int level)
	{
		if ((bool)MonoSingleton<MapLoader>.Instance && MonoSingleton<MapLoader>.Instance.isCustomLoaded)
		{
			return 0;
		}
		level--;
		int num = 0;
		for (int i = difficulty; i <= 5; i++)
		{
			GameProgressData gameProgressData = ReadGameProgress(Path.Combine(savesPath, $"difficulty{i}progress.bepis"));
			if (gameProgressData != null && gameProgressData.difficulty == i && gameProgressData.primeLevels != null && gameProgressData.primeLevels.Length > level && gameProgressData.primeLevels[level] > num)
			{
				Debug.Log("Highest: . Data: " + gameProgressData.primeLevels[level]);
				if (gameProgressData.primeLevels[level] >= 2)
				{
					return 2;
				}
				num = gameProgressData.primeLevels[level];
			}
		}
		return num;
	}

	public static void AddGear(string gear)
	{
		if (!MonoSingleton<MapLoader>.Instance || !MonoSingleton<MapLoader>.Instance.isCustomLoaded)
		{
			GameProgressMoneyAndGear gameProgressMoneyAndGear = ReadGameProgressMoneyAndGear() ?? newProgressMoneyAndGear;
			switch (gear)
			{
			case "rev0":
				gameProgressMoneyAndGear.rev0 = 1;
				break;
			case "rev1":
				gameProgressMoneyAndGear.rev1 = 1;
				break;
			case "rev2":
				gameProgressMoneyAndGear.rev2 = 1;
				break;
			case "rev3":
				gameProgressMoneyAndGear.rev3 = 1;
				break;
			case "revalt":
				gameProgressMoneyAndGear.revalt = 1;
				break;
			case "sho0":
				gameProgressMoneyAndGear.sho0 = 1;
				break;
			case "sho1":
				gameProgressMoneyAndGear.sho1 = 1;
				break;
			case "sho2":
				gameProgressMoneyAndGear.sho2 = 1;
				break;
			case "sho3":
				gameProgressMoneyAndGear.sho3 = 1;
				break;
			case "nai0":
				gameProgressMoneyAndGear.nai0 = 1;
				break;
			case "nai1":
				gameProgressMoneyAndGear.nai1 = 1;
				break;
			case "nai2":
				gameProgressMoneyAndGear.nai2 = 1;
				break;
			case "nai3":
				gameProgressMoneyAndGear.nai3 = 1;
				break;
			case "rai0":
				gameProgressMoneyAndGear.rai0 = 1;
				break;
			case "rai1":
				gameProgressMoneyAndGear.rai1 = 1;
				break;
			case "rai2":
				gameProgressMoneyAndGear.rai2 = 1;
				break;
			case "rai3":
				gameProgressMoneyAndGear.rai3 = 1;
				break;
			case "rock0":
				gameProgressMoneyAndGear.rock0 = 1;
				break;
			case "rock1":
				gameProgressMoneyAndGear.rock1 = 1;
				break;
			case "rock2":
				gameProgressMoneyAndGear.rock2 = 1;
				break;
			case "rock3":
				gameProgressMoneyAndGear.rock3 = 1;
				break;
			case "arm1":
				gameProgressMoneyAndGear.arm1 = 1;
				break;
			case "arm2":
				gameProgressMoneyAndGear.arm2 = 1;
				break;
			case "arm3":
				gameProgressMoneyAndGear.arm3 = 1;
				break;
			}
			SerializeWrite(generalProgressPath, gameProgressMoneyAndGear);
		}
	}

	public static int CheckGear(string gear)
	{
		GameProgressMoneyAndGear gameProgressMoneyAndGear = ReadGameProgressMoneyAndGear();
		if (gameProgressMoneyAndGear == null)
		{
			return 0;
		}
		switch (gear)
		{
		case "rev0":
			return gameProgressMoneyAndGear.rev0;
		case "rev1":
			return gameProgressMoneyAndGear.rev1;
		case "rev2":
			return gameProgressMoneyAndGear.rev2;
		case "rev3":
			return gameProgressMoneyAndGear.rev3;
		case "revalt":
			return gameProgressMoneyAndGear.revalt;
		case "sho0":
			return gameProgressMoneyAndGear.sho0;
		case "sho1":
			return gameProgressMoneyAndGear.sho1;
		case "sho2":
			return gameProgressMoneyAndGear.sho2;
		case "sho3":
			return gameProgressMoneyAndGear.sho3;
		case "nai0":
			return gameProgressMoneyAndGear.nai0;
		case "nai1":
			return gameProgressMoneyAndGear.nai1;
		case "nai2":
			return gameProgressMoneyAndGear.nai2;
		case "nai3":
			return gameProgressMoneyAndGear.nai3;
		case "rai0":
			return gameProgressMoneyAndGear.rai0;
		case "rai1":
			return gameProgressMoneyAndGear.rai1;
		case "rai2":
			return gameProgressMoneyAndGear.rai2;
		case "rai3":
			return gameProgressMoneyAndGear.rai3;
		case "rock0":
			return gameProgressMoneyAndGear.rock0;
		case "rock1":
			return gameProgressMoneyAndGear.rock1;
		case "rock2":
			return gameProgressMoneyAndGear.rock2;
		case "rock3":
			return gameProgressMoneyAndGear.rock3;
		case "arm1":
			return gameProgressMoneyAndGear.arm1;
		case "arm2":
			return gameProgressMoneyAndGear.arm2;
		case "arm3":
			return gameProgressMoneyAndGear.arm3;
		default:
			return 0;
		}
	}

	public static void AddMoney(int money)
	{
		if (!MonoSingleton<MapLoader>.Instance || !MonoSingleton<MapLoader>.Instance.isCustomLoaded)
		{
			GameProgressMoneyAndGear gameProgressMoneyAndGear = ReadGameProgressMoneyAndGear() ?? newProgressMoneyAndGear;
			if (gameProgressMoneyAndGear.money + money >= 0)
			{
				gameProgressMoneyAndGear.money += money;
			}
			else
			{
				gameProgressMoneyAndGear.money = 0;
			}
			SerializeWrite(generalProgressPath, gameProgressMoneyAndGear);
		}
	}

	public static int GetMoney()
	{
		return ReadGameProgressMoneyAndGear()?.money ?? 0;
	}

	public static bool GetIntro()
	{
		if ((bool)MonoSingleton<MapLoader>.Instance && MonoSingleton<MapLoader>.Instance.isCustomLoaded)
		{
			return true;
		}
		return ReadGameProgressMoneyAndGear()?.introSeen ?? false;
	}

	public static void SetIntro(bool beat)
	{
		GameProgressMoneyAndGear gameProgressMoneyAndGear = ReadGameProgressMoneyAndGear() ?? newProgressMoneyAndGear;
		gameProgressMoneyAndGear.introSeen = beat;
		SerializeWrite(generalProgressPath, gameProgressMoneyAndGear);
	}

	public static bool GetTutorial()
	{
		if ((bool)MonoSingleton<MapLoader>.Instance && MonoSingleton<MapLoader>.Instance.isCustomLoaded)
		{
			return true;
		}
		return ReadGameProgressMoneyAndGear()?.tutorialBeat ?? false;
	}

	public static void SetTutorial(bool beat)
	{
		GameProgressMoneyAndGear gameProgressMoneyAndGear = ReadGameProgressMoneyAndGear() ?? newProgressMoneyAndGear;
		gameProgressMoneyAndGear.tutorialBeat = beat;
		SerializeWrite(generalProgressPath, gameProgressMoneyAndGear);
	}

	public static int GetSecretMission(int missionNumber)
	{
		GameProgressMoneyAndGear gameProgressMoneyAndGear = ReadGameProgressMoneyAndGear();
		if (gameProgressMoneyAndGear == null)
		{
			return 0;
		}
		if (gameProgressMoneyAndGear.secretMissions == null || gameProgressMoneyAndGear.secretMissions.Length == 0)
		{
			gameProgressMoneyAndGear.secretMissions = new int[10];
		}
		return gameProgressMoneyAndGear.secretMissions[missionNumber];
	}

	public static void FoundSecretMission(int missionNumber)
	{
		if (!MonoSingleton<MapLoader>.Instance || !MonoSingleton<MapLoader>.Instance.isCustomLoaded)
		{
			GameProgressMoneyAndGear gameProgressMoneyAndGear = ReadGameProgressMoneyAndGear() ?? newProgressMoneyAndGear;
			if (gameProgressMoneyAndGear.secretMissions[missionNumber] != 2)
			{
				gameProgressMoneyAndGear.secretMissions[missionNumber] = 1;
			}
			SerializeWrite(generalProgressPath, gameProgressMoneyAndGear);
		}
	}

	public static void SetSecretMission(int missionNumber)
	{
		if (!MonoSingleton<MapLoader>.Instance || !MonoSingleton<MapLoader>.Instance.isCustomLoaded)
		{
			GameProgressMoneyAndGear gameProgressMoneyAndGear = ReadGameProgressMoneyAndGear() ?? newProgressMoneyAndGear;
			gameProgressMoneyAndGear.secretMissions[missionNumber] = 2;
			SerializeWrite(generalProgressPath, gameProgressMoneyAndGear);
		}
	}

	public static bool GetLimboSwitch(int switchNum)
	{
		GameProgressMoneyAndGear gameProgressMoneyAndGear = ReadGameProgressMoneyAndGear();
		if (gameProgressMoneyAndGear == null)
		{
			return false;
		}
		if (gameProgressMoneyAndGear.limboSwitches == null || gameProgressMoneyAndGear.limboSwitches.Length == 0)
		{
			gameProgressMoneyAndGear.limboSwitches = new bool[4];
		}
		return gameProgressMoneyAndGear.limboSwitches[switchNum];
	}

	public static void SetLimboSwitch(int switchNum)
	{
		if (!MonoSingleton<MapLoader>.Instance || !MonoSingleton<MapLoader>.Instance.isCustomLoaded)
		{
			GameProgressMoneyAndGear gameProgressMoneyAndGear = ReadGameProgressMoneyAndGear() ?? newProgressMoneyAndGear;
			if (gameProgressMoneyAndGear.limboSwitches == null || gameProgressMoneyAndGear.limboSwitches.Length < 4)
			{
				gameProgressMoneyAndGear.limboSwitches = new bool[4];
			}
			if (switchNum < gameProgressMoneyAndGear.limboSwitches.Length)
			{
				gameProgressMoneyAndGear.limboSwitches[switchNum] = true;
			}
			SerializeWrite(generalProgressPath, gameProgressMoneyAndGear);
		}
	}

	public static bool[] GetBestiary()
	{
		GameProgressMoneyAndGear gameProgressMoneyAndGear = ReadGameProgressMoneyAndGear();
		if (gameProgressMoneyAndGear == null)
		{
			return new bool[Enum.GetValues(typeof(EnemyType)).Length];
		}
		if (gameProgressMoneyAndGear.enemiesFound == null || gameProgressMoneyAndGear.enemiesFound.Length == 0)
		{
			gameProgressMoneyAndGear.enemiesFound = new bool[Enum.GetValues(typeof(EnemyType)).Length];
		}
		return gameProgressMoneyAndGear.enemiesFound;
	}

	public static void SetBestiary(int enemy)
	{
		GameProgressMoneyAndGear gameProgressMoneyAndGear = ReadGameProgressMoneyAndGear() ?? newProgressMoneyAndGear;
		if (gameProgressMoneyAndGear.enemiesFound == null || gameProgressMoneyAndGear.enemiesFound.Length == 0)
		{
			gameProgressMoneyAndGear.enemiesFound = new bool[Enum.GetValues(typeof(EnemyType)).Length];
		}
		if (enemy < gameProgressMoneyAndGear.enemiesFound.Length)
		{
			gameProgressMoneyAndGear.enemiesFound[enemy] = true;
		}
		else
		{
			bool[] array = new bool[Enum.GetValues(typeof(EnemyType)).Length];
			for (int i = 0; i < gameProgressMoneyAndGear.enemiesFound.Length; i++)
			{
				array[i] = gameProgressMoneyAndGear.enemiesFound[i];
			}
			for (int j = gameProgressMoneyAndGear.enemiesFound.Length; j < array.Length; j++)
			{
				if (j == enemy)
				{
					array[j] = true;
				}
				else
				{
					array[j] = false;
				}
			}
			gameProgressMoneyAndGear.enemiesFound = array;
		}
		SerializeWrite(generalProgressPath, gameProgressMoneyAndGear);
	}

	public static CyberRankData GetBestCyber()
	{
		CyberRankData cyberRankData = ReadGameCyberRankData(cyberGrindHighScorePath);
		if (cyberRankData == null)
		{
			cyberRankData = new CyberRankData
			{
				preciseWavesByDifficulty = new float[6],
				kills = new int[6],
				style = new int[6],
				time = new float[6]
			};
		}
		else if (cyberRankData.wavesByDifficulty != null)
		{
			cyberRankData.preciseWavesByDifficulty = new float[6];
			cyberRankData.preciseWavesByDifficulty = ((IEnumerable<int>)cyberRankData.wavesByDifficulty).Select((Func<int, float>)((int oldWave) => oldWave)).ToArray();
			cyberRankData.wavesByDifficulty = null;
		}
		if (cyberRankData.preciseWavesByDifficulty != null && cyberRankData.preciseWavesByDifficulty.Length == 6)
		{
			return cyberRankData;
		}
		cyberRankData.preciseWavesByDifficulty = new float[6];
		cyberRankData.kills = new int[6];
		cyberRankData.time = new float[6];
		cyberRankData.style = new int[6];
		if (cyberRankData.wave != 0)
		{
			for (int num = 0; num < cyberRankData.preciseWavesByDifficulty.Length; num++)
			{
				cyberRankData.preciseWavesByDifficulty[num] = cyberRankData.wave;
			}
			cyberRankData.wave = 0;
		}
		return cyberRankData;
	}

	public static void SetBestCyber(FinalCyberRank fcr)
	{
		if ((bool)MonoSingleton<MapLoader>.Instance && MonoSingleton<MapLoader>.Instance.isCustomLoaded)
		{
			return;
		}
		CyberRankData cyberRankData = ReadGameCyberRankData(cyberGrindHighScorePath);
		int num = PlayerPrefs.GetInt("Diff", 2);
		if (cyberRankData == null)
		{
			cyberRankData = new CyberRankData
			{
				preciseWavesByDifficulty = new float[6],
				kills = new int[6],
				style = new int[6],
				time = new float[6]
			};
		}
		else if (cyberRankData.wavesByDifficulty != null)
		{
			cyberRankData.preciseWavesByDifficulty = new float[6];
			cyberRankData.preciseWavesByDifficulty = ((IEnumerable<int>)cyberRankData.wavesByDifficulty).Select((Func<int, float>)((int oldWave) => oldWave)).ToArray();
			cyberRankData.wavesByDifficulty = null;
		}
		cyberRankData.preciseWavesByDifficulty[num] = fcr.savedWaves;
		cyberRankData.kills[num] = fcr.savedKills;
		cyberRankData.style[num] = fcr.savedStyle;
		cyberRankData.time[num] = fcr.savedTime;
		SerializeWrite(cyberGrindHighScorePath, cyberRankData);
	}
}
