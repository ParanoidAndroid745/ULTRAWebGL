using UnityEngine;
using UnityEngine.UI;

public class CheatsController : MonoBehaviour
{
	public static bool NoCooldown = false;

	public static bool BlindEnemies = false;

	public static bool IgnoreArenaTriggers = false;

	private static readonly KeyCode[] Sequence = new KeyCode[10]
	{
		KeyCode.UpArrow,
		KeyCode.UpArrow,
		KeyCode.DownArrow,
		KeyCode.DownArrow,
		KeyCode.LeftArrow,
		KeyCode.RightArrow,
		KeyCode.LeftArrow,
		KeyCode.RightArrow,
		KeyCode.B,
		KeyCode.A
	};

	[SerializeField]
	private CameraController cameraCtrl;

	[SerializeField]
	private GunControl gunControl;

	[SerializeField]
	private GameObject spawnerArm;

	[SerializeField]
	private Transform camera;

	[SerializeField]
	private Rigidbody rigidbody;

	[SerializeField]
	private NewMovement movementComponent;

	[SerializeField]
	private GameObject consentScreen;

	[SerializeField]
	private GameObject cheatsEnabledPanel;

	[SerializeField]
	private GameObject cheatsInfoPanel;

	[SerializeField]
	private GameObject cheatsTeleportPanel;

	[SerializeField]
	private string enabledColor;

	[SerializeField]
	private string disabledColor;

	[SerializeField]
	private Text cheatsInfo;

	[Space]
	[SerializeField]
	private GameObject cheatEnabledSound;

	[SerializeField]
	private GameObject cheatDisabledSound;

	private int sequenceIndex;

	private bool cheatsEnabled;

	private bool noclip;

	private bool flight;

	private bool infiniteJumps;

	private bool spawnerArmSpawned;

	private bool stayEnabled;

	private DebugArm arm;

	private void RenderInfo()
	{
		cheatsInfo.text = "[V] <color=" + (noclip ? enabledColor : disabledColor) + ">NOCLIP</color>\n[B] <color=" + (flight ? enabledColor : disabledColor) + ">FLIGHT</color>\n[N] <color=" + (infiniteJumps ? enabledColor : disabledColor) + ">INFINITE WALL JUMPS</color>\n[M] <color=" + (BlindEnemies ? enabledColor : disabledColor) + ">BLIND ENEMIES</color>\n[C] <color=" + (NoCooldown ? enabledColor : disabledColor) + ">NO WEAPON COOLDOWN</color>\n[I] <color=" + (IgnoreArenaTriggers ? enabledColor : disabledColor) + ">DISABLE ENEMY SPAWNS</color>\n[L] <color=" + disabledColor + ">TELEPORT MENU</color>\n" + (spawnerArmSpawned ? "<color=#7cbf7c>[P] ARM IN WEAPON SLOT 5</color>\n\n" : "[P] GET THE SPAWNER ARM\n\n") + "[O] <color=" + (stayEnabled ? enabledColor : disabledColor) + ">KEEP ENABLED</color>\n" + (stayEnabled ? "<color=#44FF45>CHEATS WILL STAY ENABLED</color>" : "CHEATS WILL BE DISABLED ON LEVEL CHANGE OR RESTART");
	}

	private void Start()
	{
		NoCooldown = false;
		BlindEnemies = false;
		IgnoreArenaTriggers = false;
		stayEnabled = PlayerPrefs.GetInt("CheatsStayEnabled", 0) == 1;
		if (stayEnabled)
		{
			MonoSingleton<AssistController>.Instance.cheatsEnabled = true;
			consentScreen.SetActive(value: false);
			cheatsEnabled = true;
			cheatsEnabledPanel.SetActive(value: true);
			LoadCheatState();
			RenderInfo();
		}
		else
		{
#if !UNITY_WEBGL
			PlayerPrefs.SetInt("CheatsInfoPanelOpen", 1);
#else
			// En WebGL el panel de cheats salia al entrar en cada nivel y tapaba
			// la pantalla: lo dejamos cerrado (se abre con la secuencia de teclas).
			PlayerPrefs.SetInt("CheatsInfoPanelOpen", 0);
			cheatsInfoPanel.SetActive(value: false);
			cheatsEnabledPanel.SetActive(value: false);
#endif
			PlayerPrefs.SetInt("CheatState_InfiniteJumps", 0);
			PlayerPrefs.SetInt("CheatState_BlindEnemies", 0);
			PlayerPrefs.SetInt("CheatState_NoCooldown", 0);
			PlayerPrefs.SetInt("CheatState_IgnoreArenaTriggers", 0);
		}
	}

	private void LoadCheatState()
	{
		cheatsInfoPanel.SetActive(PlayerPrefs.GetInt("CheatsInfoPanelOpen", 1) == 1);
		infiniteJumps = PlayerPrefs.GetInt("CheatState_InfiniteJumps", 0) == 1;
		BlindEnemies = PlayerPrefs.GetInt("CheatState_BlindEnemies", 0) == 1;
		NoCooldown = PlayerPrefs.GetInt("CheatState_NoCooldown", 0) == 1;
		IgnoreArenaTriggers = PlayerPrefs.GetInt("CheatState_IgnoreArenaTriggers", 0) == 1;
	}

	private void SaveCheatState()
	{
		PlayerPrefs.SetInt("CheatsInfoPanelOpen", cheatsInfoPanel.activeSelf ? 1 : 0);
		PlayerPrefs.SetInt("CheatState_InfiniteJumps", infiniteJumps ? 1 : 0);
		PlayerPrefs.SetInt("CheatState_BlindEnemies", BlindEnemies ? 1 : 0);
		PlayerPrefs.SetInt("CheatState_NoCooldown", NoCooldown ? 1 : 0);
		PlayerPrefs.SetInt("CheatState_IgnoreArenaTriggers", IgnoreArenaTriggers ? 1 : 0);
	}

	private void OnDisable()
	{
		NoCooldown = false;
		BlindEnemies = false;
		IgnoreArenaTriggers = false;
	}

	private void OnGUI()
	{
		if (cheatsEnabled)
		{
			return;
		}
		Event current = Event.current;
		if (!current.isKey || current.type != EventType.KeyDown || current.keyCode == KeyCode.None)
		{
			return;
		}
		if (current.keyCode == Sequence[sequenceIndex])
		{
			sequenceIndex++;
			if (sequenceIndex == Sequence.Length)
			{
				MonoSingleton<OptionsManager>.Instance.Pause();
				consentScreen.SetActive(value: true);
				sequenceIndex = 0;
			}
		}
		else
		{
			sequenceIndex = 0;
		}
	}

	private void PlayToggleSound(bool newState)
	{
		Object.Instantiate(newState ? cheatEnabledSound : cheatDisabledSound);
	}

	private void Update()
	{
		if (!cheatsEnabled)
		{
			NoCooldown = false;
			return;
		}
		if (Input.GetKeyDown(KeyCode.Home) || Input.GetKeyDown(KeyCode.Tilde) || Input.GetKeyDown(KeyCode.BackQuote))
		{
			cheatsInfoPanel.SetActive(!cheatsInfoPanel.activeSelf);
			if (Debug.isDebugBuild)
			{
				cheatsEnabledPanel.SetActive(cheatsInfoPanel.activeSelf);
			}
			SaveCheatState();
		}
		if (Input.GetKeyDown(KeyCode.V))
		{
			noclip = !noclip;
			PlayToggleSound(noclip);
			flight = false;
			movementComponent.enabled = !noclip;
			rigidbody.isKinematic = noclip;
			RenderInfo();
			SaveCheatState();
		}
		if (Input.GetKeyDown(KeyCode.B))
		{
			flight = !flight;
			PlayToggleSound(flight);
			noclip = false;
			movementComponent.enabled = !flight;
			rigidbody.isKinematic = false;
			rigidbody.useGravity = !flight;
			RenderInfo();
			SaveCheatState();
		}
		if (Input.GetKeyDown(KeyCode.N))
		{
			infiniteJumps = !infiniteJumps;
			PlayToggleSound(infiniteJumps);
			RenderInfo();
			SaveCheatState();
		}
		if (Input.GetKeyDown(KeyCode.M))
		{
			BlindEnemies = !BlindEnemies;
			PlayToggleSound(BlindEnemies);
			RenderInfo();
			SaveCheatState();
		}
		if (Input.GetKeyDown(KeyCode.C))
		{
			NoCooldown = !NoCooldown;
			PlayToggleSound(NoCooldown);
			RenderInfo();
			SaveCheatState();
		}
		if (Input.GetKeyDown(KeyCode.I))
		{
			IgnoreArenaTriggers = !IgnoreArenaTriggers;
			PlayToggleSound(IgnoreArenaTriggers);
			RenderInfo();
			SaveCheatState();
		}
		if (Input.GetKeyDown(KeyCode.L))
		{
			PlayToggleSound(!cheatsTeleportPanel.activeSelf);
			if (cheatsTeleportPanel.activeSelf)
			{
				cheatsTeleportPanel.SetActive(value: false);
				MonoSingleton<OptionsManager>.Instance.UnFreeze();
			}
			else
			{
				cheatsTeleportPanel.SetActive(value: true);
				MonoSingleton<OptionsManager>.Instance.Freeze();
			}
		}
		if (Input.GetKeyDown(KeyCode.P))
		{
			if (!spawnerArmSpawned)
			{
				spawnerArmSpawned = true;
				PlayToggleSound(spawnerArmSpawned);
				GameObject gameObject = Object.Instantiate(spawnerArm, gunControl.transform);
				gunControl.slot5.Add(gameObject);
				gunControl.UpdateWeaponList();
				arm = gameObject.GetComponent<DebugArm>();
				arm.cameraCtrl = cameraCtrl;
				arm.gunCtrl = gunControl;
			}
			else
			{
				spawnerArmSpawned = false;
				PlayToggleSound(spawnerArmSpawned);
				if (gunControl.slot5.Contains(arm.gameObject))
				{
					gunControl.slot5.Remove(arm.gameObject);
				}
				Object.Destroy(arm.gameObject);
				gunControl.UpdateWeaponList();
			}
			RenderInfo();
		}
		if (Input.GetKeyDown(KeyCode.O))
		{
			stayEnabled = !stayEnabled;
			PlayToggleSound(stayEnabled);
			PlayerPrefs.SetInt("CheatsStayEnabled", stayEnabled ? 1 : 0);
			RenderInfo();
			SaveCheatState();
		}
		if (infiniteJumps)
		{
			movementComponent.currentWallJumps = 0;
		}
		if (noclip)
		{
			Vector2 vector = Vector2.ClampMagnitude(MonoSingleton<InputManager>.Instance.InputSource.Move.ReadValue<Vector2>(), 1f);
			base.transform.position = base.transform.position + camera.right * vector.x * 40f * Time.deltaTime;
			base.transform.position = base.transform.position + camera.forward * vector.y * 40f * Time.deltaTime;
			if (MonoSingleton<InputManager>.Instance.InputSource.Jump.IsPressed)
			{
				base.transform.position = base.transform.position + new Vector3(0f, 40f, 0f) * 1f * Time.deltaTime;
			}
			if (MonoSingleton<InputManager>.Instance.InputSource.Slide.IsPressed)
			{
				base.transform.position = base.transform.position + new Vector3(0f, -40f, 0f) * 1f * Time.deltaTime;
			}
		}
		if (flight)
		{
			Vector3 zero = Vector3.zero;
			Vector2 vector2 = Vector2.ClampMagnitude(MonoSingleton<InputManager>.Instance.InputSource.Move.ReadValue<Vector2>(), 1f);
			zero += camera.right * vector2.x;
			zero += camera.forward * vector2.y;
			if (MonoSingleton<InputManager>.Instance.InputSource.Jump.IsPressed)
			{
				zero += camera.up;
			}
			if (MonoSingleton<InputManager>.Instance.InputSource.Slide.IsPressed)
			{
				zero -= camera.up;
			}
			rigidbody.velocity = zero * 30f;
		}
	}

	public void ActivateCheats()
	{
		MonoSingleton<AssistController>.Instance.cheatsEnabled = true;
		consentScreen.SetActive(value: false);
		cheatsInfoPanel.SetActive(value: true);
		cheatsEnabledPanel.SetActive(value: true);
		cheatsEnabled = true;
		RenderInfo();
	}

	public void Cancel()
	{
		consentScreen.SetActive(value: false);
	}
}
