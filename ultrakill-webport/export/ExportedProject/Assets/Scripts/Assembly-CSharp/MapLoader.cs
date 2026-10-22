using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[ConfigureSingleton(SingletonFlags.NoAutoInstance | SingletonFlags.DestroyDuplicates)]
public class MapLoader : MonoSingleton<MapLoader>
{
	// Cambiar al reconstruir act-0 (evita que el navegador sirva el bundle viejo).
	// v21: shaders transparentes (agua, cristal, sangre, grietas) reparados: el
	// header "Pass Blend ..." estaba mal formado y Unity los compilaba vacios.
	private const string WebGLBundleQuery = "?v=211";

	public delegate void LoadCallback(AssetBundle actBundle);

	public delegate void WorkshopLoadingState(bool loaded, bool error);

	public delegate void WorkshopUpdateInterrupt(PublishedFileId id);

	public delegate void LoadErrorCallback();

	[SerializeField]
	private string agonyVersion;

	[SerializeField]
	private GameObject helperCanvas;

	[SerializeField]
	private AudioMixerGroup masterMixer;

	[SerializeField]
	private AudioMixerGroup musicMixer;

	[SerializeField]
	private GameObject requiredMapObject;

	[SerializeField]
	private GameObject finalRoomPit;

	[HideInInspector]
	public string currentCustomId;

	[HideInInspector]
	public ulong currentWorkshopId;

	public bool isCustomLoaded;

	public bool isCommonLoaded;

	private AssetBundle loadedBundle;

	private AssetBundleCreateRequest loadRequest;

	private bool customSecret;

	private List<string> debugLoadedBundles;

	private string queuedBundle;

	private PublishedFileId queuedWorkshopItem;

	private WorkshopLoadingState cachedWorkshopLoadingState;

	private LoadCallback queuedCallback;

	public static string CustomMapsPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Mods", "Maps");

	protected override void Awake()
	{
		base.Awake();
		debugLoadedBundles = new List<string>();
	}

	private void Start()
	{
		base.transform.SetParent(null);
		Object.DontDestroyOnLoad(base.gameObject);
	}

	public void SpawnFinalPitAndFinish()
	{
		FinalRoom finalRoom = Object.FindObjectOfType<FinalRoom>();
		if (finalRoom != null)
		{
			if ((bool)finalRoom.doorOpener)
			{
				finalRoom.doorOpener.SetActive(value: true);
			}
			MonoSingleton<NewMovement>.Instance.transform.position = finalRoom.dropPoint.position;
		}
		else
		{
			GameObject obj = Object.Instantiate(finalRoomPit);
			finalRoom = obj.GetComponent<FinalRoom>();
			obj.transform.position = new Vector3(50000f, -1000f, 50000f);
			MonoSingleton<NewMovement>.Instance.transform.position = finalRoom.dropPoint.position;
		}
	}

	public void DropLoadedBundle()
	{
		if ((bool)loadedBundle)
		{
			if (debugLoadedBundles.Contains(loadedBundle.name))
			{
				debugLoadedBundles.Remove(loadedBundle.name);
			}
			loadedBundle.Unload(unloadAllLoadedObjects: true);
		}
		isCustomLoaded = false;
		currentCustomId = string.Empty;
		currentWorkshopId = 0uL;
	}

	private void OnGUI()
	{
		if (!Debug.isDebugBuild)
		{
			return;
		}
		int num = 0;
		if (isCustomLoaded)
		{
			GUI.Label(new Rect(0f, num * 20, 500f, 50f), "Agony [" + agonyVersion + "]");
			num++;
		}
		if (currentWorkshopId != 0L)
		{
			GUI.Label(new Rect(0f, num * 20, 500f, 50f), $"WorkshopId: {currentWorkshopId}");
			num++;
		}
		if (debugLoadedBundles != null && debugLoadedBundles.Count != 0)
		{
			GUI.Label(new Rect(0f, num * 20, 500f, 50f), "external: " + isCustomLoaded);
		}
		num++;
		foreach (string debugLoadedBundle in debugLoadedBundles)
		{
			GUI.Label(new Rect(0f, num * 20, 500f, 50f), debugLoadedBundle);
			num++;
		}
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
	}

	public static bool CheckForAct(string act)
	{
#if UNITY_WEBGL
		return true;
#else
		return File.Exists(Path.Combine(Application.streamingAssetsPath, "acts", act));
#endif
	}

	private void LoadWorkshopMapFromPath(string path)
	{
		EnsureCommonIsLoaded();
		MapDataRebuild mapDataRebuild = LoadMapData(path, withBundle: true);
		if (loadedBundle != null)
		{
			if (debugLoadedBundles.Contains(loadedBundle.name))
			{
				debugLoadedBundles.Remove(loadedBundle.name);
			}
			loadedBundle.Unload(unloadAllLoadedObjects: false);
		}
		isCustomLoaded = true;
		currentCustomId = mapDataRebuild.uniqueId;
		AssetBundle assetBundle = AssetBundle.LoadFromMemory(mapDataRebuild.bundle);
		debugLoadedBundles.Add(assetBundle.name);
		loadedBundle = assetBundle;
		SceneManager.LoadScene(Path.GetFileNameWithoutExtension(assetBundle.GetAllScenePaths()[0]));
		helperCanvas.SetActive(value: true);
	}

	public async Task<Item?> FetchWorkshopItemInfo(PublishedFileId id)
	{
		Item? result = await SteamUGC.QueryFileAsync(id);
		if (result.HasValue)
		{
			return result;
		}
		Debug.LogError("Invalid item Id");
		return null;
	}

	public async void AutoLoadWorkshopMap(PublishedFileId id, WorkshopLoadingState stateChanged, WorkshopUpdateInterrupt workshopInterrupt, bool overrideWorkshopUpdate = false)
	{
		Item? item = await Item.GetAsync(id);
		if (!item.HasValue)
		{
			stateChanged(loaded: false, error: true);
			Debug.LogError("Invalid item Id");
			return;
		}
		Debug.Log("Title: " + item?.Title);
		Debug.Log($"IsInstalled: {item?.IsInstalled}");
		Debug.Log($"IsDownloading: {item?.IsDownloading}");
		Debug.Log($"IsDownloadPending: {item?.IsDownloadPending}");
		Debug.Log($"IsSubscribed: {item?.IsSubscribed}");
		Debug.Log($"NeedsUpdate: {item?.NeedsUpdate}");
		Debug.Log("Description: " + item?.Description);
		if ((item?.NeedsUpdate).Value && !overrideWorkshopUpdate && workshopInterrupt != null)
		{
			workshopInterrupt(id);
			return;
		}
		bool flag = item.Value.IsInstalled;
		if ((item?.NeedsUpdate).Value && !overrideWorkshopUpdate)
		{
			flag = false;
		}
		if (flag)
		{
			Debug.Log(item.Value.Directory);
			currentWorkshopId = id.Value;
			string[] files = Directory.GetFiles(item.Value.Directory);
			Debug.Log(files[0]);
			LoadWorkshopMapFromPath(files[0]);
			stateChanged(loaded: true, error: false);
		}
		else
		{
			Debug.Log("Updating workshop map: " + item?.Title);
			stateChanged(loaded: false, error: false);
			queuedWorkshopItem = id;
			cachedWorkshopLoadingState = stateChanged;
			SteamUGC.OnDownloadItemResult += SteamUGCOnOnDownloadItemResult;
			SteamUGC.Download(id);
		}
	}

	private void SteamUGCOnOnDownloadItemResult(Result obj)
	{
		if (obj == Result.OK)
		{
			SteamUGC.OnDownloadItemResult -= SteamUGCOnOnDownloadItemResult;
			AutoLoadWorkshopMap(queuedWorkshopItem, cachedWorkshopLoadingState, null, overrideWorkshopUpdate: true);
		}
	}

	/// <summary>
	/// Carga directa de un nivel (lo usa el menu de niveles de la pagina).
	/// FinalRank.RanklessNextLevel solo carga el bundle del acto de la escena
	/// actual, asi que aqui se elige el acto segun el nombre del nivel.
	/// </summary>
	public void DebugLoadLevel(string levelName)
	{
		if (string.IsNullOrEmpty(levelName)) return;
		string act = "act-0";
		if (levelName.StartsWith("Level 1-") || levelName.StartsWith("Level 2-") || levelName.StartsWith("Level 3-"))
		{
			act = "act-1";
		}
		else if (levelName.StartsWith("Level P-"))
		{
			act = "prime";
		}
		Debug.Log("[DebugLoadLevel] " + levelName + " -> bundle " + act);
		LoadAct(act, (AssetBundle bundle) =>
		{
			string[] scenes = (from p in bundle.GetAllScenePaths() select Path.GetFileNameWithoutExtension(p)).ToArray();
			if (!scenes.Contains(levelName))
			{
				Debug.LogError("[DebugLoadLevel] el bundle " + act + " no contiene " + levelName);
				return;
			}
			SceneManager.LoadSceneAsync(levelName);
		}, () =>
		{
			Debug.LogError("[DebugLoadLevel] no se pudo cargar el bundle " + act);
		});
	}

	public void LoadAct(string act, LoadCallback callback, LoadErrorCallback errorCallback)
	{
		EnsureCommonIsLoaded();
#if UNITY_WEBGL
		if ((bool)loadedBundle && loadedBundle.name == "acts/" + act)
		{
			Debug.Log("Act " + act + " already loaded");
			callback(loadedBundle);
			return;
		}
		if ((bool)loadedBundle)
		{
			if (debugLoadedBundles.Contains(loadedBundle.name))
			{
				debugLoadedBundles.Remove(loadedBundle.name);
			}
			loadedBundle.Unload(unloadAllLoadedObjects: false);
		}
		isCustomLoaded = false;
		currentCustomId = string.Empty;
		SmartLoadBundle(Application.streamingAssetsPath + "/acts/" + act + WebGLBundleQuery, callback, errorCallback);
#else
		if (!File.Exists(Path.Combine(Application.streamingAssetsPath, "acts", act)))
		{
			Debug.LogError(act + " is missing from streaming assets");
			errorCallback();
			return;
		}
		if (loadRequest != null && loadRequest.assetBundle.name != "acts/" + act)
		{
			Debug.Log("Interrupting " + loadRequest.assetBundle.name + " load to load acts/" + act);
			queuedBundle = Path.Combine(Application.streamingAssetsPath, "acts", act);
			return;
		}
		if ((bool)loadedBundle && loadedBundle.name == "acts/" + act)
		{
			Debug.Log("Act " + act + " already loaded");
			callback(loadedBundle);
			return;
		}
		if ((bool)loadedBundle)
		{
			if (debugLoadedBundles.Contains(loadedBundle.name))
			{
				debugLoadedBundles.Remove(loadedBundle.name);
			}
			loadedBundle.Unload(unloadAllLoadedObjects: false);
		}
		isCustomLoaded = false;
		currentCustomId = string.Empty;
		SmartLoadBundle(Path.Combine(Application.streamingAssetsPath, "acts", act), callback);
#endif
	}

	private void SmartLoadBundle(string bundle, LoadCallback callback, LoadErrorCallback errorCallback = null)
	{
#if UNITY_WEBGL
		StartCoroutine(LoadBundleWebGL(bundle, callback, errorCallback));
#else
		loadRequest = AssetBundle.LoadFromFileAsync(bundle);
		loadRequest.completed += (AsyncOperation operation) =>
		{
			debugLoadedBundles.Add(loadRequest.assetBundle.name);
			loadedBundle = loadRequest.assetBundle;
			if (!string.IsNullOrEmpty(queuedBundle))
			{
				if (debugLoadedBundles.Contains(loadedBundle.name))
				{
					debugLoadedBundles.Remove(loadedBundle.name);
				}
				loadedBundle.Unload(unloadAllLoadedObjects: false);
				SmartLoadBundle(queuedBundle, queuedCallback);
				queuedBundle = string.Empty;
				queuedCallback = null;
			}
			else
			{
				callback(loadRequest.assetBundle);
				loadRequest = null;
			}
		};
#endif
	}

#if UNITY_WEBGL
	private IEnumerator LoadBundleWebGL(string url, LoadCallback callback, LoadErrorCallback errorCallback)
	{
		Debug.Log("[WebGL] Loading bundle " + url);
		using (UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url))
		{
			yield return request.SendWebRequest();
			if (request.isNetworkError || request.isHttpError)
			{
				Debug.LogError("[WebGL] Error loading " + url + ": " + request.error);
				if (errorCallback != null)
				{
					errorCallback();
				}
				yield break;
			}
			AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
			if (bundle == null)
			{
				Debug.LogError("[WebGL] Null bundle " + url);
				if (errorCallback != null)
				{
					errorCallback();
				}
				yield break;
			}
			debugLoadedBundles.Add(bundle.name);
			loadedBundle = bundle;
			loadRequest = null;
			callback(bundle);
		}
	}
#endif

	public void EnsureCommonIsLoaded()
	{
#if UNITY_WEBGL
		isCommonLoaded = true;
#else
		if (!isCommonLoaded)
		{
			Debug.Log("Loading the common bundle");
			AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "common"));
			debugLoadedBundles.Add("common");
			isCommonLoaded = true;
		}
#endif
	}

	public MapDataRebuild[] GetAvailableMaps(LocalSortMode sortMode)
	{
		Directory.CreateDirectory(CustomMapsPath);
		FileInfo[] array = new DirectoryInfo(CustomMapsPath).GetFiles("*");
		if (sortMode == LocalSortMode.Date)
		{
			array = array.OrderBy((FileInfo x) => x.LastWriteTime).Reverse().ToArray();
		}
		Debug.Log($"Found {array.Length} files in {CustomMapsPath}");
		array = array.Where((FileInfo f) => f.Extension == ".blood" || f.Extension == ".cum").ToArray();
		return array.Select((FileInfo file) => LoadMapData(file.FullName, withBundle: false)).ToArray();
	}

	public static MapDataRebuild LoadMapData(string file, bool withBundle)
	{
		FileStream fileStream = new FileStream(file, FileMode.Open);
		BinaryReader binaryReader = new BinaryReader(fileStream);
		MapHeader mapHeader = JsonUtility.FromJson<MapHeader>(binaryReader.ReadString());
		byte[] data = binaryReader.ReadBytes(mapHeader.ThumbSize);
		Texture2D texture2D = null;
		if (mapHeader.ThumbSize != 0)
		{
			texture2D = new Texture2D(640, 480);
			texture2D.LoadImage(data);
			texture2D.Apply();
		}
		MapDataRebuild mapDataRebuild = new MapDataRebuild
		{
			uniqueId = mapHeader.UniqueIdentifier,
			author = mapHeader.Author,
			description = mapHeader.Description,
			mapName = mapHeader.Name,
			path = file,
			thumbnail = texture2D
		};
		if (withBundle)
		{
			mapDataRebuild.bundle = binaryReader.ReadBytes(mapHeader.BundleSize);
		}
		binaryReader.Close();
		fileStream.Close();
		return mapDataRebuild;
	}

	public void SceneChanged()
	{
		helperCanvas.SetActive(value: true);
	}

	public void LoadAuto(string relativePath)
	{
		if (File.Exists(Path.Combine(CustomMapsPath, relativePath)))
		{
			EnsureCommonIsLoaded();
			MapDataRebuild mapDataRebuild = LoadMapData(Path.Combine(CustomMapsPath, relativePath), withBundle: true);
			if (loadedBundle != null)
			{
				if (debugLoadedBundles.Contains(loadedBundle.name))
				{
					debugLoadedBundles.Remove(loadedBundle.name);
				}
				loadedBundle.Unload(unloadAllLoadedObjects: false);
			}
			isCustomLoaded = true;
			currentCustomId = mapDataRebuild.uniqueId;
			AssetBundle assetBundle = AssetBundle.LoadFromMemory(mapDataRebuild.bundle);
			debugLoadedBundles.Add(assetBundle.name);
			loadedBundle = assetBundle;
			SceneManager.LoadScene(Path.GetFileNameWithoutExtension(assetBundle.GetAllScenePaths()[0]));
			helperCanvas.SetActive(value: true);
		}
		else
		{
			Debug.LogError("Tried to load an invalid map, " + relativePath);
		}
	}

	private void SceneManagerOnsceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name == "Main Menu" || scene.name == "Endless")
		{
			isCustomLoaded = false;
			currentCustomId = string.Empty;
		}
		else
		{
			if (!isCustomLoaded)
			{
				return;
			}
			MapInfo mapInfo = Object.FindObjectOfType<MapInfo>();
			if ((bool)mapInfo && mapInfo.renderSkybox)
			{
				MonoSingleton<CameraController>.Instance.GetComponent<Camera>().clearFlags = CameraClearFlags.Skybox;
			}
			Object.Instantiate(requiredMapObject);
			AudioSource[] array = Object.FindObjectsOfType<AudioSource>();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].outputAudioMixerGroup = masterMixer;
			}
			if ((bool)MonoSingleton<MusicManager>.Instance)
			{
				if ((bool)MonoSingleton<MusicManager>.Instance.battleTheme)
				{
					MonoSingleton<MusicManager>.Instance.battleTheme.outputAudioMixerGroup = musicMixer;
				}
				if ((bool)MonoSingleton<MusicManager>.Instance.cleanTheme)
				{
					MonoSingleton<MusicManager>.Instance.cleanTheme.outputAudioMixerGroup = musicMixer;
				}
				if ((bool)MonoSingleton<MusicManager>.Instance.bossTheme)
				{
					MonoSingleton<MusicManager>.Instance.bossTheme.outputAudioMixerGroup = musicMixer;
				}
			}
		}
	}
}
