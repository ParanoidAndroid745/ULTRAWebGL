using System.Collections.Generic;

[ConfigureSingleton(SingletonFlags.PersistAutoInstance)]
public class BestiaryData : MonoSingleton<BestiaryData>
{
	private bool checkedSave;

	private List<EnemyType> foundEnemies = new List<EnemyType>();

	private void Start()
	{
		if (!checkedSave)
		{
			CheckSave();
		}
	}

	public bool GetEnemy(EnemyType enemy)
	{
		if (!checkedSave)
		{
			CheckSave();
		}
		if (foundEnemies.Contains(enemy))
		{
			return true;
		}
		return false;
	}

	public void SetEnemy(EnemyType enemy)
	{
		if (!checkedSave)
		{
			CheckSave();
		}
		if (!foundEnemies.Contains(enemy))
		{
			foundEnemies.Add(enemy);
			GameProgressSaver.SetBestiary((int)enemy);
		}
	}

	private void CheckSave()
	{
		checkedSave = true;
		bool[] bestiary = GameProgressSaver.GetBestiary();
		foundEnemies.Clear();
		for (int i = 0; i < bestiary.Length; i++)
		{
			if (bestiary[i])
			{
				foundEnemies.Add((EnemyType)i);
			}
		}
	}
}
