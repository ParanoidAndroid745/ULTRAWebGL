using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CustomTextures : MonoBehaviour
{
	private enum EditMode
	{
		None = 0,
		Grid = 1,
		Skybox = 2,
		Emission = 3
	}

	[SerializeField]
	private GameObject textureButtonTemplate;

	[SerializeField]
	private GameObject folderIconTemplate;

	[SerializeField]
	private GameObject backButton;

	[SerializeField]
	private Transform texturesGrid;

	[SerializeField]
	private Material[] gridMaterials;

	[SerializeField]
	private OutdoorLightMaster olm;

	[SerializeField]
	private Material skyMaterial;

	[SerializeField]
	private Texture defaultGlow;

	[SerializeField]
	private Text pageText;

	[SerializeField]
	private Texture[] defaultGridTextures;

	[SerializeField]
	private Texture defaultEmission;

	[SerializeField]
	private Texture[] defaultSkyboxes;

	[SerializeField]
	private GameObject gridWrapper;

	[SerializeField]
	private Image gridBtnFrame;

	[SerializeField]
	private Image skyboxBtnFrame;

	[SerializeField]
	private Image emissionBtnFrame;

	private Dictionary<string, Texture2D> imageCache = new Dictionary<string, Texture2D>();

	private EditMode currentEditMode;

	private int currentPage = 1;

	private int maxPages = 1;

	private int maxItemsPerPage = 14;

	private string currentRelativePath = string.Empty;

	private bool editBase = true;

	private bool editTop = true;

	private bool editTopRow = true;

	[SerializeField]
	private Image baseBtnFrame;

	[SerializeField]
	private Image topBtnFrame;

	[SerializeField]
	private Image topRowBtnFrame;

	[SerializeField]
	private Slider glowSlider;

	private static readonly int EmissiveTex = Shader.PropertyToID("_EmissiveTex");

	private static readonly string[] AllowedExtensions = new string[3] { ".png", ".jpg", ".jpeg" };

	private string TexturesPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "CyberGrind", "Textures");

	public bool TryToLoad(string key)
	{
		if (!File.Exists(Path.Combine(TexturesPath, key)))
		{
			Debug.LogError("Tried to load an invalid texture! " + key);
			return false;
		}
		LoadTexture(key);
		return true;
	}

	public void SetEditMode(int m)
	{
		currentRelativePath = string.Empty;
		currentPage = 1;
		BuildButtons();
		EditMode editMode = (currentEditMode = (EditMode)m);
		switch (editMode)
		{
		case EditMode.Grid:
			gridBtnFrame.color = Color.red;
			skyboxBtnFrame.color = Color.white;
			emissionBtnFrame.color = Color.white;
			break;
		case EditMode.Skybox:
			gridBtnFrame.color = Color.white;
			skyboxBtnFrame.color = Color.red;
			emissionBtnFrame.color = Color.white;
			break;
		case EditMode.Emission:
			gridBtnFrame.color = Color.white;
			skyboxBtnFrame.color = Color.white;
			emissionBtnFrame.color = Color.red;
			break;
		}
		gridWrapper.SetActive(editMode != EditMode.None);
	}

	public void SetGridEditMode(int num)
	{
		switch (num)
		{
		case 0:
			if (editBase)
			{
				editBase = false;
			}
			else
			{
				editBase = true;
			}
			break;
		case 1:
			if (editTop)
			{
				editTop = false;
			}
			else
			{
				editTop = true;
			}
			break;
		case 2:
			if (editTopRow)
			{
				editTopRow = false;
			}
			else
			{
				editTopRow = true;
			}
			break;
		}
		if (editBase)
		{
			baseBtnFrame.color = Color.red;
		}
		else
		{
			baseBtnFrame.color = Color.white;
		}
		if (editTop)
		{
			topBtnFrame.color = Color.red;
		}
		else
		{
			topBtnFrame.color = Color.white;
		}
		if (editTopRow)
		{
			topRowBtnFrame.color = Color.red;
		}
		else
		{
			topRowBtnFrame.color = Color.white;
		}
	}

	public void NextPage()
	{
		if (currentPage != maxPages)
		{
			currentPage++;
			BuildButtons();
		}
	}

	public void PreviousPage()
	{
		if (currentPage != 1)
		{
			currentPage--;
			BuildButtons();
		}
	}

	public void OpenDirectory(string dir)
	{
		currentPage = 1;
		currentRelativePath = Path.Combine(currentRelativePath, dir);
		backButton.SetActive(value: true);
		BuildButtons();
	}

	public void GoUpTheHierarchy()
	{
		currentRelativePath = Path.GetDirectoryName(currentRelativePath);
		if (currentRelativePath == string.Empty)
		{
			backButton.SetActive(value: false);
		}
		BuildButtons();
	}

	public void SetTexture(string key)
	{
		switch (currentEditMode)
		{
		case EditMode.Grid:
			if (editBase)
			{
				PlayerPrefs.SetString("cg_custom_grid_0", key);
				gridMaterials[0].mainTexture = imageCache[key];
			}
			if (editTop)
			{
				PlayerPrefs.SetString("cg_custom_grid_1", key);
				gridMaterials[1].mainTexture = imageCache[key];
			}
			if (editTopRow)
			{
				PlayerPrefs.SetString("cg_custom_grid_2", key);
				gridMaterials[2].mainTexture = imageCache[key];
			}
			break;
		case EditMode.Emission:
			if (editBase)
			{
				PlayerPrefs.SetString("cg_custom_glow_0", key);
				gridMaterials[0].SetTexture(EmissiveTex, imageCache[key]);
			}
			if (editTop)
			{
				PlayerPrefs.SetString("cg_custom_glow_1", key);
				gridMaterials[1].SetTexture(EmissiveTex, imageCache[key]);
			}
			if (editTopRow)
			{
				PlayerPrefs.SetString("cg_custom_glow_2", key);
				gridMaterials[2].SetTexture(EmissiveTex, imageCache[key]);
			}
			break;
		case EditMode.Skybox:
			PlayerPrefs.SetString("cg_custom_skybox", key);
			skyMaterial.mainTexture = imageCache[key];
			olm?.UpdateSkyboxMaterial();
			break;
		}
	}

	public void SetGlowIntensity()
	{
		MonoSingleton<EndlessGrid>.Instance.glowMultiplier = glowSlider.value;
		MonoSingleton<EndlessGrid>.Instance.UpdateGlow();
		PlayerPrefs.SetFloat("cg_glow_intensity", glowSlider.value);
	}

	private void Start()
	{
#if !UNITY_WEBGL
		// En WebGL no hay sistema de ficheros: Directory.CreateDirectory lanza
		// excepcion con la ruta URL de Application.dataPath.
		Directory.CreateDirectory(Path.GetDirectoryName(TexturesPath));
		Directory.CreateDirectory(TexturesPath);
#endif
		BuildButtons();
		string[] array = new string[3]
		{
			PlayerPrefs.GetString("cg_custom_grid_0"),
			PlayerPrefs.GetString("cg_custom_grid_1"),
			PlayerPrefs.GetString("cg_custom_grid_2")
		};
		string[] array2 = new string[3]
		{
			PlayerPrefs.GetString("cg_custom_glow_0"),
			PlayerPrefs.GetString("cg_custom_glow_1"),
			PlayerPrefs.GetString("cg_custom_glow_2")
		};
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != string.Empty && TryToLoad(array[i]))
			{
				gridMaterials[i].mainTexture = imageCache[array[i]];
			}
			else
			{
				PlayerPrefs.DeleteKey($"cg_custom_grid_{i}");
			}
		}
		for (int j = 0; j < array2.Length; j++)
		{
			if (!string.IsNullOrEmpty(array2[j]) && TryToLoad(array2[j]))
			{
				gridMaterials[j].SetTexture(EmissiveTex, imageCache[array2[j]]);
				continue;
			}
			PlayerPrefs.DeleteKey($"cg_custom_glow_{j}");
			gridMaterials[j].SetTexture("_EmissiveTex", defaultGlow);
		}
		string text = PlayerPrefs.GetString("cg_custom_skybox");
		if (text != string.Empty && TryToLoad(text))
		{
			skyMaterial.mainTexture = imageCache[text];
			olm?.UpdateSkyboxMaterial();
		}
		float num = PlayerPrefs.GetFloat("cg_glow_intensity", -1f);
		if (num != -1f)
		{
			glowSlider.SetValueWithoutNotify(num);
			MonoSingleton<EndlessGrid>.Instance.glowMultiplier = glowSlider.value;
			MonoSingleton<EndlessGrid>.Instance.UpdateGlow();
		}
	}

	public void BuildButtons()
	{
#if UNITY_WEBGL
		// Sin sistema de ficheros no hay texturas custom que listar.
		return;
#else
		GridTile[] collection = (from f in Directory.GetDirectories(Path.Combine(TexturesPath, currentRelativePath), "*", SearchOption.TopDirectoryOnly)
			select new GridTile
			{
				path = Path.GetFileName(f),
				folder = true
			}).ToArray();
		string[] files = Directory.GetFiles(Path.Combine(TexturesPath, currentRelativePath), "*", SearchOption.TopDirectoryOnly);
		List<string> list = new List<string>();
		List<GridTile> list2 = new List<GridTile>();
		list2.AddRange(collection);
		string[] array = files;
		foreach (string path in array)
		{
			string fileName = Path.GetFileName(path);
			string extension = Path.GetExtension(path);
			if (AllowedExtensions.Contains(extension))
			{
				list.Add(fileName);
				list2.Add(new GridTile
				{
					path = fileName
				});
			}
		}
		list.ToArray();
		int num2 = 1;
		for (int num3 = 4; num3 < texturesGrid.childCount; num3++)
		{
			Object.Destroy(texturesGrid.GetChild(num3).gameObject);
		}
		maxPages = Mathf.CeilToInt((float)list2.Count / (float)maxItemsPerPage);
		bool flag = currentRelativePath != string.Empty;
		if (currentPage == 1)
		{
			if (currentRelativePath == string.Empty)
			{
				backButton.SetActive(value: false);
			}
			else
			{
				backButton.SetActive(value: true);
				num2++;
			}
		}
		else
		{
			backButton.SetActive(value: false);
		}
		for (int num4 = (currentPage - 1) * (maxItemsPerPage - (flag ? 1 : 0)); num4 < list2.Count; num4++)
		{
			if (list2[num4].folder)
			{
				GameObject obj = Object.Instantiate(folderIconTemplate, texturesGrid, worldPositionStays: false);
				string dir = list2[num4].path;
				obj.GetComponent<Button>().onClick.AddListener(() =>
				{
					OpenDirectory(dir);
				});
				obj.GetComponentInChildren<Text>().text = list2[num4].path;
				obj.SetActive(value: true);
			}
			else
			{
				string file = list2[num4].path;
				Texture2D texture2D = LoadTexture(Path.Combine(currentRelativePath, file));
				GameObject obj2 = Object.Instantiate(textureButtonTemplate, texturesGrid, worldPositionStays: false);
				Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
				sprite.texture.filterMode = FilterMode.Point;
				obj2.GetComponent<Button>().onClick.AddListener(() =>
				{
					SetTexture(Path.Combine(currentRelativePath, file));
				});
				obj2.GetComponent<Image>().sprite = sprite;
				obj2.SetActive(value: true);
			}
			num2++;
			if (num2 > maxItemsPerPage)
			{
				break;
			}
		}
		pageText.text = $"{currentPage}/{maxPages}";
		textureButtonTemplate.SetActive(value: false);
#endif
	}

	private Texture2D LoadTexture(string name)
	{
		if (imageCache.ContainsKey(name))
		{
			return imageCache[name];
		}
		byte[] data = File.ReadAllBytes(Path.Combine(TexturesPath, name));
		Texture2D texture2D = new Texture2D(0, 0, TextureFormat.RGBA32, mipChain: false);
		texture2D.filterMode = FilterMode.Point;
		texture2D.LoadImage(data);
		imageCache[name] = texture2D;
		return texture2D;
	}

	public void RemoveCustomPrefs()
	{
		PlayerPrefs.DeleteKey("cg_custom_grid_0");
		PlayerPrefs.DeleteKey("cg_custom_grid_1");
		PlayerPrefs.DeleteKey("cg_custom_grid_2");
		PlayerPrefs.DeleteKey("cg_custom_glow_0");
		PlayerPrefs.DeleteKey("cg_custom_glow_1");
		PlayerPrefs.DeleteKey("cg_custom_glow_2");
		PlayerPrefs.DeleteKey("cg_custom_skybox");
		PlayerPrefs.DeleteKey("cg_glow_intensity");
	}

	public void ResetTexture()
	{
		int num = PlayerPrefs.GetInt("cgtheme", 0);
		switch (currentEditMode)
		{
		case EditMode.Grid:
			if (editBase)
			{
				PlayerPrefs.DeleteKey("cg_custom_grid_0");
				gridMaterials[0].mainTexture = defaultGridTextures[num];
			}
			if (editTop)
			{
				PlayerPrefs.DeleteKey("cg_custom_grid_1");
				gridMaterials[1].mainTexture = defaultGridTextures[num];
			}
			if (editTopRow)
			{
				PlayerPrefs.DeleteKey("cg_custom_grid_2");
				gridMaterials[2].mainTexture = defaultGridTextures[num];
			}
			break;
		case EditMode.Emission:
			if (editBase)
			{
				PlayerPrefs.DeleteKey("cg_custom_glow_0");
				gridMaterials[0].SetTexture(EmissiveTex, defaultEmission);
			}
			if (editTop)
			{
				PlayerPrefs.DeleteKey("cg_custom_glow_1");
				gridMaterials[1].SetTexture(EmissiveTex, defaultEmission);
			}
			if (editTopRow)
			{
				PlayerPrefs.DeleteKey("cg_custom_glow_2");
				gridMaterials[2].SetTexture(EmissiveTex, defaultEmission);
			}
			break;
		case EditMode.Skybox:
			skyMaterial.mainTexture = defaultSkyboxes[num];
			olm?.UpdateSkyboxMaterial();
			PlayerPrefs.DeleteKey("cg_custom_skybox");
			break;
		}
	}
}
