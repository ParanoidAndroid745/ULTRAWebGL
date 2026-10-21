using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class GraphicsOptions : MonoBehaviour
{
	// Port WebGL: se han eliminado los ajustes PSX del menu de GRAPHICS.
	// Los GameObjects de los controles (pixelization, textureWarping,
	// vertexWarping, colorCompression) se han BORRADO del prefab Canvas y de
	// las escenas (Tutorial y Main Menu), asi que estas referencias son null
	// y hay que comprobarlas antes de usarlas.
	public Dropdown pixelization;

	public Slider textureWarping;

	public Dropdown vertexWarping;

	public Dropdown colorCompression;

	public Toggle vSync;

	// Valores neutros (el juego se ve SIEMPRE como el preset PC, y ademas sin
	// compresion de color):
	//   Pixelization 0     -> _ResY = 0      => sin rejilla de vertices
	//   TextureWarping 0   -> _TextureWarping = 0
	//   VertexWarping 0    -> _VertexWarping = 9999 (valor "off" del juego)
	//   ColorCompression 0 -> _ColorPrecision = 2048 (sin cuantizar color)
	private const int NeutralPixelization = 0;

	private const float NeutralTextureWarping = 0f;

	private const int NeutralVertexWarping = 0;

	private const int NeutralColorCompression = 0;

	private CameraController cc;

	private void Awake()
	{
		Initialize();
	}

	private void Start()
	{
		Initialize();
	}

	/// <summary>
	/// Aplica el aspecto neutro (PC) del port. Se ignora cualquier valor
	/// guardado en PlayerPrefs: el port es siempre-PC.
	/// </summary>
	public void ApplyNeutral()
	{
		Pixelization(NeutralPixelization);
		TextureWarping(NeutralTextureWarping);
		VertexWarping(NeutralVertexWarping);
		ColorCompression(NeutralColorCompression);
	}

	public void PCPreset()
	{
		ApplyNeutral();
	}

	public void PSXPreset()
	{
		// El boton "PSX" se ha eliminado del menu, pero por si acaso se llama
		// desde algun sitio, se aplican los valores neutros: nunca el look PSX.
		ApplyNeutral();
	}

	public void Initialize()
	{
		// Los PlayerPrefs ("Pix", "TexWar", "VerWar", "ColCom") se IGNORAN a
		// proposito: aunque el navegador tenga "Pix" = 3 (PSX) guardado de una
		// partida anterior, este port arranca siempre en neutro.
		ApplyNeutral();
		if (PlayerPrefs.GetInt("VSync", 1) == 1)
		{
			QualitySettings.vSyncCount = 1;
			if (vSync != null)
			{
				vSync.isOn = true;
			}
		}
		else
		{
			QualitySettings.vSyncCount = 0;
			if (vSync != null)
			{
				vSync.isOn = false;
			}
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
		if (pixelization != null)
		{
			pixelization.value = stuff;
			pixelization.RefreshShownValue();
		}
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
		if (textureWarping != null)
		{
			textureWarping.value = stuff;
		}
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
		if (vertexWarping != null)
		{
			vertexWarping.value = stuff;
			vertexWarping.RefreshShownValue();
		}
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
		if (colorCompression != null)
		{
			colorCompression.value = stuff;
			colorCompression.RefreshShownValue();
		}
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
