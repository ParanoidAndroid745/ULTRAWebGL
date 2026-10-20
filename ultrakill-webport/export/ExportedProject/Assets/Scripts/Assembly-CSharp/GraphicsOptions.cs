using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class GraphicsOptions : MonoBehaviour
{
	public Dropdown pixelization;

	public Slider textureWarping;

	public Dropdown vertexWarping;

	public Dropdown colorCompression;

	public Toggle vSync;

	private CameraController cc;

	private void Awake()
	{
		Initialize();
	}

	private void Start()
	{
		Initialize();
	}

	public void PCPreset()
	{
		pixelization.value = 0;
		pixelization.RefreshShownValue();
		textureWarping.value = 0f;
		vertexWarping.value = 0;
		vertexWarping.RefreshShownValue();
		colorCompression.value = 2;
		colorCompression.RefreshShownValue();
		// Asignar Dropdown.value NO dispara onValueChanged, asi que el preset
		// solo cambiaba la etiqueta y no aplicaba nada. Hay que llamar a los
		// metodos de aplicacion a mano.
		Pixelization(0);
		TextureWarping(0f);
		VertexWarping(0);
		ColorCompression(2);
		PlayerPrefs.Save();
	}

	public void PSXPreset()
	{
		pixelization.value = 3;
		pixelization.RefreshShownValue();
		textureWarping.value = 100f;
		vertexWarping.value = 2;
		vertexWarping.RefreshShownValue();
		colorCompression.value = 2;
		colorCompression.RefreshShownValue();
		// Ver PCPreset(): el preset no aplicaba los valores.
		Pixelization(3);
		TextureWarping(100f);
		VertexWarping(2);
		ColorCompression(2);
		PlayerPrefs.Save();
	}

	public void Initialize()
	{
		int num = PlayerPrefs.GetInt("Pix", 3);
		pixelization.value = num;
		pixelization.RefreshShownValue();
		Pixelization(num);
		float num2 = PlayerPrefs.GetFloat("TexWar", 100f);
		textureWarping.value = num2;
		TextureWarping(num2);
		num = PlayerPrefs.GetInt("VerWar", 2);
		vertexWarping.value = num;
		vertexWarping.RefreshShownValue();
		VertexWarping(num);
		num = PlayerPrefs.GetInt("ColCom", 2);
		colorCompression.value = num;
		colorCompression.RefreshShownValue();
		ColorCompression(num);
		if (PlayerPrefs.GetInt("VSync", 1) == 1)
		{
			QualitySettings.vSyncCount = 1;
			vSync.isOn = true;
		}
		else
		{
			QualitySettings.vSyncCount = 0;
			vSync.isOn = false;
		}
	}

	public void Pixelization(int stuff)
	{
		PlayerPrefs.SetInt("Pix", stuff);
		float value = 0f;
		switch (stuff)
		{
		case 0:
			value = 0f;
			break;
		case 1:
			value = 720f;
			break;
		case 2:
			value = 480f;
			break;
		case 3:
			value = 360f;
			break;
		case 4:
			value = 240f;
			break;
		case 5:
			value = 144f;
			break;
		case 6:
			value = 36f;
			break;
		}
		Shader.SetGlobalFloat("_ResY", value);
		// En WebGL no se llama a OnApplicationQuit, asi que PlayerPrefs no se
		// guarda solo: hay que forzar el flush.
		PlayerPrefs.Save();
		DownscaleChangeSprite[] array = Object.FindObjectsOfType<DownscaleChangeSprite>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].CheckScale();
		}
	}

	public void TextureWarping(float stuff)
	{
		PlayerPrefs.SetFloat("TexWar", stuff);
		float value = stuff * 0.003f;
		Shader.SetGlobalFloat("_TextureWarping", value);
		PlayerPrefs.Save();
	}

	public void VertexWarping(int stuff)
	{
		PlayerPrefs.SetInt("VerWar", stuff);
		float value = 0f;
		switch (stuff)
		{
		case 0:
			value = 9999f;
			break;
		case 1:
			value = 400f;
			break;
		case 2:
			value = 160f;
			break;
		case 3:
			value = 80f;
			break;
		case 4:
			value = 40f;
			break;
		case 5:
			value = 16f;
			break;
		}
		Shader.SetGlobalFloat("_VertexWarping", value);
		PlayerPrefs.Save();
	}

	public void ColorCompression(int stuff)
	{
		PlayerPrefs.SetInt("ColCom", stuff);
		switch (stuff)
		{
		case 0:
			Shader.SetGlobalFloat("_ColorPrecision", 2048f);
			break;
		case 1:
			Shader.SetGlobalFloat("_ColorPrecision", 64f);
			break;
		case 2:
			Shader.SetGlobalFloat("_ColorPrecision", 32f);
			break;
		case 3:
			Shader.SetGlobalFloat("_ColorPrecision", 16f);
			break;
		case 4:
			Shader.SetGlobalFloat("_ColorPrecision", 8f);
			break;
		case 5:
			Shader.SetGlobalFloat("_ColorPrecision", 3f);
			break;
		}
		PlayerPrefs.Save();
	}

	public void VSync(bool stuff)
	{
		if (stuff)
		{
			QualitySettings.vSyncCount = 1;
			PlayerPrefs.SetInt("VSync", 1);
		}
		else
		{
			QualitySettings.vSyncCount = 0;
			PlayerPrefs.SetInt("VSync", 0);
		}
	}
}
