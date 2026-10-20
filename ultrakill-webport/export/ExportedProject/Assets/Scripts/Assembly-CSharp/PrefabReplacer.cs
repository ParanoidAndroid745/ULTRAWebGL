using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-20)]
public class PrefabReplacer : MonoBehaviour
{
	public static PrefabReplacer Instance;

	[SerializeField]
	private PlaceholderPrefabDatabase database;

	private Dictionary<string, GameObject> placeholderPrefabs;

	private void Awake()
	{
		if (!Instance)
		{
			Instance = this;
		}
		placeholderPrefabs = new Dictionary<string, GameObject>();
		PlaceholderPrefabTarget[] array = database.placeholderPrefabs;
		foreach (PlaceholderPrefabTarget placeholderPrefabTarget in array)
		{
			placeholderPrefabs.Add(placeholderPrefabTarget.uniqueId, placeholderPrefabTarget.actualPrefab);
		}
	}

	public void ReplacePrefab(PlaceholderPrefab placeholder)
	{
		if (placeholderPrefabs.ContainsKey(placeholder.uniqueId))
		{
			Debug.Log("Key " + placeholder.uniqueId + " matched");
			GameObject gameObject = Object.Instantiate(placeholderPrefabs[placeholder.uniqueId]);
			Transform transform = placeholder.transform;
			gameObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
			IPlaceholdableComponent[] componentsInChildren = placeholder.GetComponentsInChildren<IPlaceholdableComponent>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].WillReplace(placeholder.gameObject, gameObject, isSelfBeingReplaced: true);
			}
			componentsInChildren = gameObject.GetComponentsInChildren<IPlaceholdableComponent>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].WillReplace(placeholder.gameObject, gameObject, isSelfBeingReplaced: false);
			}
			Object.Destroy(placeholder.gameObject);
		}
		else
		{
			Debug.Log("Key " + placeholder.uniqueId + " not matched");
		}
	}
}
