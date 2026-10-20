using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsMenuToManager : MonoBehaviour
{
	public GameObject pauseMenu;

	public GameObject optionsMenu;

	public Slider mouseSensitivitySlider;

	public Toggle reverseMouseX;

	public Toggle reverseMouseY;

	public Slider simplifiedDistanceSlider;

	public Toggle simplifyEnemies;

	public Toggle outlinesOnly;

	public Slider screenShakeSlider;

	public Toggle cameraTilt;

	public Toggle discordIntegration;

	public Toggle steamIntegration;

	public Dropdown resolutionDropdown;

	private Resolution[] resolutions;

	private List<Resolution> availableResolutions = new List<Resolution>();

	public Toggle fullScreen;

	public Dropdown framerateLimiter;

	public Toggle simplerExplosions;

	public Toggle simplerFire;

	public Toggle simplerSpawns;

	public Toggle noEnviroParts;

	[SerializeField]
	private Toggle simpleNailPhysicsToggle;

	[HideInInspector]
	public static bool simpleNailPhysics = true;

	public Toggle bloodAndGore;

	public Toggle freezeGore;

	public Slider bloodstainChanceSlider;

	public Slider maxGoreSlider;

	public Slider masterVolume;

	public Slider musicVolume;

	private OptionsManager opm;

	private MusicManager muman;

	private Camera mainCam;

	private CameraController cc;

	public Slider fovSlider;

	public Dropdown weaponPosDropdown;

	public Toggle variationMemory;

	private List<string> options;

	public AudioClip normalJump;

	public AudioClip quakeJump;

	private NewMovement nmov;

	private void Start()
	{
		opm = MonoSingleton<OptionsManager>.Instance;
		muman = MonoSingleton<MusicManager>.Instance;
		opm.pauseMenu = pauseMenu;
		opm.optionsMenu = optionsMenu;
		framerateLimiter.value = PlayerPrefs.GetInt("FraRatLim", 1);
		framerateLimiter.RefreshShownValue();
		if (PlayerPrefs.GetInt("SimExp", 0) == 1)
		{
			simplerExplosions.isOn = true;
		}
		if (PlayerPrefs.GetInt("SimFir", 0) == 1)
		{
			simplerFire.isOn = true;
		}
		if (PlayerPrefs.GetInt("SimSpa", 0) == 1)
		{
			simplerSpawns.isOn = true;
		}
		if (PlayerPrefs.GetInt("EnvPar", 0) == 1)
		{
			noEnviroParts.isOn = true;
		}
		if (PlayerPrefs.GetInt("SimNai", 1) == 1)
		{
			simpleNailPhysicsToggle.isOn = true;
		}
		else
		{
			simpleNailPhysics = false;
		}
		if (PlayerPrefs.GetInt("BlOn", 1) == 0)
		{
			bloodAndGore.isOn = false;
		}
		if (PlayerPrefs.GetInt("FreGor", 0) == 1)
		{
			freezeGore.isOn = true;
		}
		if (PlayerPrefs.GetInt("SiEn", 0) == 1)
		{
			simplifyEnemies.isOn = true;
			opm.simplifyEnemies = true;
			Shader.SetGlobalFloat("_UseEdgeDetection", 1f);
		}
		else
		{
			Shader.SetGlobalFloat("_UseEdgeDetection", 0f);
		}
		if (PlayerPrefs.GetInt("OuOn", 0) == 1)
		{
			outlinesOnly.isOn = true;
			opm.outlinesOnly = true;
		}
		opm.simplifiedDistance = PlayerPrefs.GetFloat("SiEnDi", 0f);
		simplifiedDistanceSlider.value = opm.simplifiedDistance;
		opm.mouseSensitivity = PlayerPrefs.GetFloat("MoSe", 50f);
		mouseSensitivitySlider.value = opm.mouseSensitivity;
		if (PlayerPrefs.GetInt("RevMouX", 0) == 1)
		{
			reverseMouseX.isOn = true;
		}
		if (PlayerPrefs.GetInt("RevMouY", 0) == 1)
		{
			reverseMouseY.isOn = true;
		}
		screenShakeSlider.value = PlayerPrefs.GetFloat("ScrSha", 100f);
		if (PlayerPrefs.GetInt("CamTil", 1) == 0)
		{
			cameraTilt.isOn = false;
		}
		if (PlayerPrefs.GetInt("DisImp", 1) == 0)
		{
			discordIntegration.isOn = false;
		}
		if (PlayerPrefs.GetInt("SteImp", 1) == 0)
		{
			steamIntegration.isOn = false;
		}
		opm.bloodstainChance = PlayerPrefs.GetFloat("BlStCh", 50f);
		bloodstainChanceSlider.value = opm.bloodstainChance;
		opm.maxGore = PlayerPrefs.GetFloat("MaGo", 3000f);
		maxGoreSlider.value = opm.maxGore / 100f;
		if (GameProgressSaver.GetIntro())
		{
			AudioListener.volume = PlayerPrefs.GetFloat("MaVo", 1f);
			masterVolume.value = AudioListener.volume * 100f;
			if ((bool)MonoSingleton<AudioMixerController>.Instance)
			{
				float num = PlayerPrefs.GetFloat("MuVo", 0.6f);
				MonoSingleton<AudioMixerController>.Instance.SetMusicVolume(num);
				musicVolume.value = num * 100f;
			}
		}
		resolutions = Screen.resolutions;
		availableResolutions.Clear();
		resolutionDropdown.ClearOptions();
		options = new List<string>();
		int value = 0;
		for (int i = 0; i < resolutions.Length; i++)
		{
			string item = resolutions[i].width + " x " + resolutions[i].height;
			if (!options.Contains(item))
			{
				options.Add(item);
				availableResolutions.Add(resolutions[i]);
				if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
				{
					value = availableResolutions.Count - 1;
				}
			}
		}
		resolutionDropdown.AddOptions(options);
		resolutionDropdown.value = value;
		resolutionDropdown.RefreshShownValue();
		weaponPosDropdown.value = PlayerPrefs.GetInt("HoldPos", 0);
		weaponPosDropdown.RefreshShownValue();
		if (PlayerPrefs.GetInt("VarMem", 0) == 1)
		{
			variationMemory.isOn = true;
		}
		nmov = MonoSingleton<NewMovement>.Instance;
		cc = MonoSingleton<CameraController>.Instance;
		mainCam = cc.GetComponent<Camera>();
		cc.defaultFov = PlayerPrefs.GetFloat("FOV", 105f);
		mainCam.fieldOfView = cc.defaultFov;
		fovSlider.value = cc.defaultFov;
	}

	public void ResolutionChange(int stuff)
	{
		Resolution resolution = availableResolutions[stuff];
		Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
		Debug.Log(Screen.width + " x " + Screen.height);
		if (cc == null)
		{
			cc = MonoSingleton<CameraController>.Instance;
			mainCam = cc.GetComponent<Camera>();
		}
		cc.CheckAspectRatio();
	}

	public void SetFullScreen(bool stuff)
	{
		Screen.fullScreen = stuff;
	}

	public void FrameRateLimiter(int stuff)
	{
		PlayerPrefs.SetInt("FraRatLim", stuff);
		switch (stuff)
		{
		case 0:
			Application.targetFrameRate = -1;
			break;
		case 1:
			Application.targetFrameRate = Screen.currentResolution.refreshRate * 2;
			break;
		case 2:
			Application.targetFrameRate = 30;
			break;
		case 3:
			Application.targetFrameRate = 60;
			break;
		case 4:
			Application.targetFrameRate = 120;
			break;
		case 5:
			Application.targetFrameRate = 144;
			break;
		case 6:
			Application.targetFrameRate = 240;
			break;
		case 7:
			Application.targetFrameRate = 288;
			break;
		}
	}

	public void UnPause()
	{
		opm.UnPause();
	}

	public void RestartCheckpoint()
	{
		opm.RestartCheckpoint();
	}

	public void RestartMission()
	{
		opm.RestartMission();
	}

	public void OpenOptions()
	{
		opm.OpenOptions();
	}

	public void CloseOptions()
	{
		opm.CloseOptions();
	}

	public void QuitMission()
	{
		opm.QuitMission();
	}

	public void QuitGame()
	{
		opm.QuitGame();
	}

	public void CheckIfTutorialBeaten()
	{
		if (!GameProgressSaver.GetTutorial())
		{
			SceneManager.LoadScene("Tutorial");
		}
	}

	public void ChangeLevel(string levelname)
	{
		opm.ChangeLevel(levelname);
	}

	public void SimpleExplosions(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("SimExp", 1);
		}
		else
		{
			PlayerPrefs.SetInt("SimExp", 0);
		}
	}

	public void SimpleFire(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("SimFir", 1);
		}
		else
		{
			PlayerPrefs.SetInt("SimFir", 0);
		}
	}

	public void SimpleSpawns(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("SimSpa", 1);
		}
		else
		{
			PlayerPrefs.SetInt("SimSpa", 0);
		}
	}

	public void DisableEnviroParts(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("EnvPar", 1);
		}
		else
		{
			PlayerPrefs.SetInt("EnvPar", 0);
		}
		EnviroParticle[] array = Object.FindObjectsOfType<EnviroParticle>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].CheckEnviroParticles();
		}
	}

	public void SimpleNailPhysics(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("SimNai", 1);
		}
		else
		{
			PlayerPrefs.SetInt("SimNai", 0);
		}
		simpleNailPhysics = stuff;
		Nail[] array = Object.FindObjectsOfType<Nail>();
		foreach (Nail nail in array)
		{
			if (nail.magnets.Count > 0 && nail.TryGetComponent<Rigidbody>(out var component))
			{
				if (simpleNailPhysics)
				{
					component.collisionDetectionMode = CollisionDetectionMode.Discrete;
				}
				else
				{
					component.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
				}
			}
		}
	}

	public void BloodAndGoreOn(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("BlOn", 1);
		}
		else
		{
			PlayerPrefs.SetInt("BlOn", 0);
		}
	}

	public void FreezeGore(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("FreGor", 1);
		}
		else
		{
			PlayerPrefs.SetInt("FreGor", 0);
		}
	}

	public void SimplifyEnemies(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("SiEn", 1);
		}
		else
		{
			PlayerPrefs.SetInt("SiEn", 0);
		}
		Shader.SetGlobalFloat("_UseEdgeDetection", stuff ? 1 : 0);
		opm.simplifyEnemies = stuff;
		simplifiedDistanceSlider.transform.parent.gameObject.SetActive(stuff);
	}

	public void OutlinesOnly(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("OuOn", 1);
		}
		else
		{
			PlayerPrefs.SetInt("OuOn", 0);
		}
		opm.outlinesOnly = stuff;
	}

	public void SimplifyEnemiesDistance(float stuff)
	{
		PlayerPrefs.SetFloat("SiEnDi", stuff);
		opm.simplifiedDistance = stuff;
	}

	public void MouseSensitivity(float stuff)
	{
		PlayerPrefs.SetFloat("MoSe", stuff);
		opm.mouseSensitivity = stuff;
	}

	public void ReverseMouseX(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("RevMouX", 1);
		}
		else
		{
			PlayerPrefs.SetInt("RevMouX", 0);
		}
		if (cc == null)
		{
			cc = MonoSingleton<CameraController>.Instance;
			mainCam = cc.GetComponent<Camera>();
		}
		cc.CheckMouseReverse();
	}

	public void ReverseMouseY(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("RevMouY", 1);
		}
		else
		{
			PlayerPrefs.SetInt("RevMouY", 0);
		}
		if (cc == null)
		{
			cc = MonoSingleton<CameraController>.Instance;
			mainCam = cc.GetComponent<Camera>();
		}
		cc.CheckMouseReverse();
	}

	public void ScreenShake(float stuff)
	{
		PlayerPrefs.SetFloat("ScrSha", stuff);
	}

	public void CameraTilt(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("CamTil", 1);
		}
		else
		{
			PlayerPrefs.SetInt("CamTil", 0);
		}
		if (cc == null)
		{
			cc = MonoSingleton<CameraController>.Instance;
			mainCam = cc.GetComponent<Camera>();
		}
		cc.CheckTilt();
	}

	public void DiscordIntegration(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("DisImp", 1);
			DiscordController.Enable();
		}
		else
		{
			PlayerPrefs.SetInt("DisImp", 0);
			DiscordController.Disable();
		}
	}

	public void SteamIntegration(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("SteImp", 1);
		}
		else
		{
			PlayerPrefs.SetInt("SteImp", 0);
		}
	}

	public void VariationMemory(bool stuff)
	{
		if (stuff)
		{
			PlayerPrefs.SetInt("VarMem", 1);
		}
		else
		{
			PlayerPrefs.SetInt("VarMem", 0);
		}
		if ((bool)MonoSingleton<GunControl>.Instance)
		{
			MonoSingleton<GunControl>.Instance.variationMemory = stuff;
		}
	}

	public void BloodStainChance(float stuff)
	{
		PlayerPrefs.SetFloat("BlStCh", stuff);
		opm.bloodstainChance = stuff;
	}

	public void maxGore(float stuff)
	{
		PlayerPrefs.SetFloat("MaGo", stuff * 100f);
		opm.maxGore = stuff * 100f;
		GoreZone[] array = Object.FindObjectsOfType<GoreZone>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].maxGore = stuff * 100f;
		}
	}

	public void MasterVolume(float stuff)
	{
		PlayerPrefs.SetFloat("MaVo", stuff / 100f);
		AudioListener.volume = stuff / 100f;
	}

	public void MusicVolume(float stuff)
	{
		PlayerPrefs.SetFloat("MuVo", stuff / 100f);
		if ((bool)MonoSingleton<AudioMixerController>.Instance)
		{
			MonoSingleton<AudioMixerController>.Instance.SetMusicVolume(stuff / 100f);
		}
	}

	public void FieldOfView(float stuff)
	{
		if (cc == null)
		{
			cc = MonoSingleton<CameraController>.Instance;
			mainCam = cc.GetComponent<Camera>();
		}
		PlayerPrefs.SetFloat("FOV", stuff);
		mainCam.fieldOfView = stuff;
		cc.defaultFov = stuff;
	}

	public void WeaponPosition(int stuff)
	{
		PlayerPrefs.SetInt("HoldPos", stuff);
		ViewModelFlip[] componentsInChildren = MonoSingleton<NewMovement>.Instance.GetComponentsInChildren<ViewModelFlip>();
		WeaponPos weaponPos = Object.FindObjectOfType<WeaponPos>();
		HUDPos[] array = Object.FindObjectsOfType<HUDPos>();
		if (weaponPos != null)
		{
			weaponPos.CheckPosition();
		}
		HUDPos[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].CheckPos();
		}
		if (stuff == 2)
		{
			ViewModelFlip[] array3 = componentsInChildren;
			for (int i = 0; i < array3.Length; i++)
			{
				array3[i].Left();
			}
		}
		else
		{
			ViewModelFlip[] array3 = componentsInChildren;
			for (int i = 0; i < array3.Length; i++)
			{
				array3[i].Right();
			}
		}
		CheckEasterEgg();
	}

	public void CheckEasterEgg()
	{
		if (!nmov)
		{
			nmov = MonoSingleton<NewMovement>.Instance;
		}
		if (PlayerPrefs.GetInt("HoldPos", 0) == 1 && PlayerPrefs.GetInt("AltHud", 1) >= 2)
		{
			nmov.quakeJump = true;
		}
		else
		{
			nmov.quakeJump = false;
		}
	}
}
