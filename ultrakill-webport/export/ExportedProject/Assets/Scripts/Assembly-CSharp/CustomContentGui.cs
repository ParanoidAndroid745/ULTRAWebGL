using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CustomContentGui : MonoBehaviour
{
	[SerializeField]
	private GameObject grid;

	[SerializeField]
	private GameObject buttonTemplate;

	[SerializeField]
	private GameObject downloadingPanel;

	[SerializeField]
	private GameObject workshopError;

	[SerializeField]
	private GameObject workshopInterrupt;

	[SerializeField]
	private GameObject fetchingPanel;

	[SerializeField]
	private InputField workshopSearch;

	[SerializeField]
	private Texture2D placeholderThumbnail;

	[SerializeField]
	private PersistentColors nameColors;

	[SerializeField]
	private Dropdown difficultyDropdown;

	[SerializeField]
	private GameObject workshopButtons;

	[SerializeField]
	private Button[] workshopTabButtons;

	[SerializeField]
	private GameObject localButtons;

	[SerializeField]
	private Dropdown localSortModeDropdown;

	private PublishedFileId? workshopInterruptedItem;

	private static LocalSortMode currentLocalSortMode;

	private static WorkshopTab currentWorkshopTab;

	private static bool lastTabWorkshop;

	private void Awake()
	{
		localButtons.SetActive(!lastTabWorkshop);
		workshopButtons.SetActive(lastTabWorkshop);
		workshopTabButtons[(int)currentWorkshopTab].interactable = false;
		localSortModeDropdown.SetValueWithoutNotify((int)currentLocalSortMode);
		int valueWithoutNotify = PlayerPrefs.GetInt("Diff", 2);
		difficultyDropdown.SetValueWithoutNotify(valueWithoutNotify);
		if (lastTabWorkshop)
		{
			RefreshWorkshopItems();
		}
		else
		{
			RefreshCustomMaps();
		}
	}

	private void ResetGrid()
	{
		for (int i = 1; i < grid.transform.childCount; i++)
		{
			UnityEngine.Object.Destroy(grid.transform.GetChild(i).gameObject);
		}
	}

	public void ShowInExplorer()
	{
		Application.OpenURL("file://" + MapLoader.CustomMapsPath);
	}

	public void SetLocalSortMode(int option)
	{
		currentLocalSortMode = (LocalSortMode)option;
		RefreshCustomMaps();
	}

	public void SetDifficulty(int dif)
	{
		PlayerPrefs.SetInt("Diff", dif);
	}

	public void SetWorkshopTab(int tab)
	{
		Button[] array = workshopTabButtons;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].interactable = true;
		}
		currentWorkshopTab = (WorkshopTab)tab;
		workshopTabButtons[(int)currentWorkshopTab].interactable = false;
		workshopSearch.interactable = tab > 2;
		if (!workshopSearch.interactable)
		{
			workshopSearch.text = string.Empty;
		}
		RefreshWorkshopItems();
	}

	public static UnityEngine.Color NameToColor(PersistentColors colorData, string targetName)
	{
		int seed = BitConverter.ToInt32(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(targetName)), 0);
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(seed);
		UnityEngine.Color result = colorData.colors[UnityEngine.Random.Range(0, colorData.colors.Length)];
		UnityEngine.Random.state = state;
		return result;
	}

	public async void RefreshWorkshopItems()
	{
		localButtons.SetActive(value: false);
		workshopButtons.SetActive(value: true);
		lastTabWorkshop = true;
		ResetGrid();
		if (!SteamClient.IsValid || !SteamClient.IsLoggedOn)
		{
			workshopError.SetActive(value: true);
			return;
		}
		fetchingPanel.SetActive(value: true);
		Query items = Query.Items;
		switch (currentWorkshopTab)
		{
		case WorkshopTab.Subscribed:
			items = items.WhereUserSubscribed();
			break;
		case WorkshopTab.Favorite:
			items = items.WhereUserFavorited();
			break;
		case WorkshopTab.YourUploads:
			items = items.WhereUserPublished();
			break;
		case WorkshopTab.Trending:
			items = items.RankedByTrend().SortByCreationDate();
			break;
		case WorkshopTab.BestVoted:
			items = items.RankedByVote();
			break;
		case WorkshopTab.Latest:
			items = items.RankedByPublicationDate();
			break;
		default:
			items = items.RankedByVote();
			break;
		}
		if (!string.IsNullOrEmpty(workshopSearch.text))
		{
			items.WhereSearchText(workshopSearch.text);
		}
		ResultPage? resultPage = await items.WithTag("map").GetPageAsync(1);
		if (!resultPage.HasValue)
		{
			workshopError.SetActive(value: true);
			fetchingPanel.SetActive(value: false);
		}
		else
		{
			if (!lastTabWorkshop)
			{
				return;
			}
			Debug.Log($"ResultCount: {resultPage?.ResultCount}");
			Debug.Log($"TotalCount: {resultPage?.TotalCount}");
			foreach (Item entry in resultPage.Value.Entries)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(buttonTemplate, grid.transform, worldPositionStays: false);
				gameObject.GetComponentInChildren<Text>().text = Path.GetFileName(entry.Title) + " <b><size=10><color=gray>by</color> <color=#" + ColorUtility.ToHtmlStringRGB(NameToColor(nameColors, entry.Owner.Id.Value.ToString())) + ">" + entry.Owner.Name + "</color></size></b>";
				gameObject.GetComponentsInChildren<Text>()[1].text = $"<color=#8bea83>{entry.VotesUp} /\\</color>  <color=#e26363>{entry.VotesDown} \\/</color>  <color=#ece477>{entry.NumFavorites} *</color>  <color=#8e8e8e>{entry.NumComments} C</color>";
				RawImage componentInChildren = gameObject.GetComponentInChildren<RawImage>();
				if (!string.IsNullOrEmpty(entry.PreviewImageUrl))
				{
					StartCoroutine(FetchPreviewImage(componentInChildren, entry.PreviewImageUrl));
				}
				else
				{
					componentInChildren.texture = placeholderThumbnail;
				}
				gameObject.GetComponent<Button>().onClick.AddListener(() =>
				{
					MonoSingleton<MapLoader>.Instance.AutoLoadWorkshopMap(entry.Id, WorkshopStateChanged, WorkshopInterrupt);
				});
				gameObject.SetActive(value: true);
			}
			buttonTemplate.SetActive(value: false);
			fetchingPanel.SetActive(value: false);
		}
	}

	private void WorkshopInterrupt(PublishedFileId id)
	{
		workshopInterruptedItem = id;
		workshopInterrupt.SetActive(value: true);
	}

	public void WorkshopInterruptUpdate()
	{
		MonoSingleton<MapLoader>.Instance.AutoLoadWorkshopMap(workshopInterruptedItem.Value, WorkshopStateChanged, null);
		workshopInterrupt.SetActive(value: false);
	}

	public void WorkshopInterruptForceLaunch()
	{
		MonoSingleton<MapLoader>.Instance.AutoLoadWorkshopMap(workshopInterruptedItem.Value, WorkshopStateChanged, null, overrideWorkshopUpdate: true);
		workshopInterrupt.SetActive(value: false);
	}

	public void WorkshopInterruptCancel()
	{
		workshopInterruptedItem = null;
		workshopInterrupt.SetActive(value: false);
	}

	private void WorkshopStateChanged(bool loaded, bool error)
	{
		downloadingPanel.SetActive(!loaded && !error);
		workshopError.SetActive(error);
		fetchingPanel.SetActive(value: false);
	}

	public static IEnumerator FetchPreviewImage(RawImage target, string url)
	{
		UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
		yield return www.SendWebRequest();
		if (www.isNetworkError || www.isHttpError)
		{
			Debug.Log(www.error);
		}
		else if ((bool)target)
		{
			target.texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
		}
	}

	public void RefreshCustomMaps()
	{
		workshopSearch.text = string.Empty;
		lastTabWorkshop = false;
		workshopError.SetActive(value: false);
		fetchingPanel.SetActive(value: false);
		localButtons.SetActive(value: true);
		workshopButtons.SetActive(value: false);
		ResetGrid();
		MapDataRebuild[] availableMaps = MonoSingleton<MapLoader>.Instance.GetAvailableMaps(currentLocalSortMode);
		Debug.Log($"Found {availableMaps.Length} maps");
		MapDataRebuild[] array = availableMaps;
		foreach (MapDataRebuild map in array)
		{
			GameObject obj = UnityEngine.Object.Instantiate(buttonTemplate, grid.transform, worldPositionStays: false);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(Path.GetFileName(map.mapName));
			if (!string.IsNullOrEmpty(map.author))
			{
				stringBuilder.Append(" <size=10><color=gray>by</color> ");
				stringBuilder.Append("<color=#");
				stringBuilder.Append(ColorUtility.ToHtmlStringRGB(NameToColor(nameColors, map.author)));
				stringBuilder.Append(">");
				stringBuilder.Append(map.author);
				stringBuilder.Append("</color></size>");
			}
			else
			{
				stringBuilder.Append(" <size=10><color=gray>Unknown Author</color></size>");
			}
			obj.GetComponentInChildren<Text>().text = stringBuilder.ToString();
			obj.GetComponentsInChildren<Text>()[1].text = Path.GetFileName(map.path);
			obj.GetComponentInChildren<RawImage>().texture = (map.thumbnail ? map.thumbnail : placeholderThumbnail);
			obj.GetComponent<Button>().onClick.AddListener(() =>
			{
				MonoSingleton<MapLoader>.Instance.LoadAuto(map.path);
			});
			obj.SetActive(value: true);
		}
		buttonTemplate.SetActive(value: false);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}
	}
}
