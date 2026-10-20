using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class CameraController : MonoSingleton<CameraController>
{
	public bool invert;

	public float minimumX = -89f;

	public float maximumX = 89f;

	public float minimumY = -360f;

	public float maximumY = 360f;

	public OptionsManager opm;

	public float scroll;

	public Vector3 originalPos;

	public Vector3 defaultPos;

	private Vector3 targetPos;

	public GameObject player;

	public NewMovement pm;

	private Camera cam;

	public bool activated;

	public GameObject gun;

	public float rotationY;

	public float rotationX;

	public bool reverseX;

	public bool reverseY;

	public float cameraShaking;

	public float movementHor;

	public float movementVer;

	public int dodgeDirection;

	public float additionalRotationY;

	public float additionalRotationX;

	public float defaultFov;

	private AudioMixer[] audmix;

	private bool mouseUnlocked;

	public bool slide;

	private AssistController asscon;

	private float slowDown = 1f;

	public float timeScale;

	public float timeScaleModifier = 1f;

	private Camera hudCamera;

	private float aspectRatio;

	private bool pixeled;

	private bool tilt;

	private float currentStop;

	private bool zooming;

	private float zoomTarget;

	private LayerMask environmentMask;

	protected override void Awake()
	{
		audmix = new AudioMixer[4]
		{
			MonoSingleton<AudioMixerController>.Instance.allSound,
			MonoSingleton<AudioMixerController>.Instance.goreSound,
			MonoSingleton<AudioMixerController>.Instance.musicSound,
			MonoSingleton<AudioMixerController>.Instance.doorSound
		};
		base.Awake();
		pm = GetComponentInParent<NewMovement>();
		player = pm.gameObject;
	}

	private void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		cam = GetComponent<Camera>();
		if ((bool)MonoSingleton<StatsManager>.Instance)
		{
			asscon = MonoSingleton<AssistController>.Instance;
		}
		if ((bool)asscon && asscon.majorEnabled)
		{
			timeScale = asscon.gameSpeed;
		}
		else
		{
			timeScale = 1f;
		}
		Time.timeScale = timeScale * timeScaleModifier;
		gun = base.transform.GetChild(0).gameObject;
		originalPos = base.transform.localPosition;
		defaultPos = base.transform.localPosition;
		targetPos = new Vector3(defaultPos.x, defaultPos.y - 0.2f, defaultPos.z);
		cam.fieldOfView = PlayerPrefs.GetFloat("FOV", 105f);
		defaultFov = cam.fieldOfView;
		if (opm == null && (bool)MonoSingleton<StatsManager>.Instance && (bool)MonoSingleton<OptionsManager>.Instance)
		{
			opm = MonoSingleton<OptionsManager>.Instance;
		}
		hudCamera = base.transform.Find("HUD Camera").GetComponent<Camera>();
		AudioMixer[] array = audmix;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetFloat("allPitch", 1f);
		}
		CheckAspectRatio();
		CheckTilt();
		environmentMask = (int)environmentMask | 0x100;
		environmentMask = (int)environmentMask | 0x1000000;
	}

	private void Update()
	{
		CheckAspectRatio();
		if ((bool)asscon)
		{
			if (asscon.majorEnabled && timeScale != asscon.gameSpeed)
			{
				timeScale = asscon.gameSpeed;
				Time.timeScale = timeScale * timeScaleModifier;
			}
			else if (!asscon.majorEnabled && timeScale != 1f)
			{
				timeScale = 1f;
				Time.timeScale = timeScale * timeScaleModifier;
			}
		}
		if (player == null)
		{
			player = pm.gameObject;
		}
		scroll = Input.GetAxis("Mouse ScrollWheel");
#if UNITY_WEBGL
		// Si el navegador bloquea el pointer lock, permitimos mirar con las flechas
		// y reintentamos capturar el raton en cada click.
		//
		// IMPORTANTE: solo mientras se esta JUGANDO. Con el menu de pausa (o
		// cualquier menu) abierto el juego suelta el raton a proposito para que
		// se pueda pinchar en los botones; si aqui lo volvemos a bloquear, el
		// navegador congela el puntero, el EventSystem recibe siempre la misma
		// posicion y el menu deja de responder (no se puede seleccionar nada).
		if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked && !IsMenuOpen())
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		if (activated)
		{
			float lookSpeed = 140f * Time.unscaledDeltaTime;
			if (Input.GetKey(KeyCode.LeftArrow))
			{
				rotationY -= lookSpeed;
			}
			if (Input.GetKey(KeyCode.RightArrow))
			{
				rotationY += lookSpeed;
			}
			if (Input.GetKey(KeyCode.UpArrow))
			{
				rotationX -= lookSpeed * 0.7f;
			}
			if (Input.GetKey(KeyCode.DownArrow))
			{
				rotationX += lookSpeed * 0.7f;
			}
		}
#endif
		if (Debug.isDebugBuild && Input.GetKeyDown(KeyCode.F2))
		{
			Transform[] array = Object.FindObjectsOfType<Transform>();
			foreach (Transform transform in array)
			{
				if (Vector3.Distance(Vector3.zero, transform.position) > 2000f && base.gameObject.activeInHierarchy && transform.gameObject.layer != 5)
				{
					Debug.LogError("An object called '" + transform.gameObject.name + "' has gone really far away. Coordinates: " + transform.position);
				}
			}
		}
		if (activated)
		{
			float num = 1f;
			Vector2 vector = MonoSingleton<InputManager>.Instance.InputSource.Look.ReadValue<Vector2>();
			if (zooming)
			{
				num = cam.fieldOfView / defaultFov;
			}
			if (!reverseY)
			{
				rotationX += vector.y * (opm.mouseSensitivity / 10f) * num;
			}
			else
			{
				rotationX -= vector.y * (opm.mouseSensitivity / 10f) * num;
			}
			if (!reverseX)
			{
				rotationY += vector.x * (opm.mouseSensitivity / 10f) * num;
			}
			else
			{
				rotationY -= vector.x * (opm.mouseSensitivity / 10f) * num;
			}
		}
		if (rotationY > 180f)
		{
			rotationY -= 360f;
		}
		else if (rotationY < -180f)
		{
			rotationY += 360f;
		}
		if (additionalRotationY > 180f)
		{
			additionalRotationY -= 360f;
		}
		else if (additionalRotationY < -180f)
		{
			additionalRotationY += 360f;
		}
		rotationX = Mathf.Clamp(rotationX, minimumX + additionalRotationX, maximumX + additionalRotationX);
		if (zooming)
		{
			cam.fieldOfView = Mathf.MoveTowards(cam.fieldOfView, zoomTarget, Time.deltaTime * 300f);
		}
		else if (pm.boost)
		{
			if (dodgeDirection == 0)
			{
				cam.fieldOfView = defaultFov - defaultFov / 20f;
			}
			else if (dodgeDirection == 1)
			{
				cam.fieldOfView = defaultFov + defaultFov / 10f;
			}
		}
		else
		{
			cam.fieldOfView = Mathf.MoveTowards(cam.fieldOfView, defaultFov, Time.deltaTime * 300f);
		}
		if (zooming)
		{
			hudCamera.fieldOfView = Mathf.MoveTowards(hudCamera.fieldOfView, zoomTarget, Time.deltaTime * 300f);
		}
		else if (hudCamera.fieldOfView != 90f)
		{
			hudCamera.fieldOfView = Mathf.MoveTowards(hudCamera.fieldOfView, 90f, Time.deltaTime * 300f);
		}
		if (activated)
		{
			player.transform.localEulerAngles = new Vector3(0f, rotationY + additionalRotationY, 0f);
		}
		float num2 = 0f;
		float num3 = movementHor * -1f;
		float num4 = base.transform.localEulerAngles.z;
		if (num4 > 180f)
		{
			num4 -= 360f;
		}
		num2 = ((!tilt) ? Mathf.MoveTowards(num4, 0f, Time.deltaTime * 25f * (Mathf.Abs(num4) + 0.01f)) : (pm.boost ? Mathf.MoveTowards(num4, num3 * 5f, Time.deltaTime * 100f * (Mathf.Abs(num4 - num3 * 5f) + 0.01f)) : Mathf.MoveTowards(num4, num3, Time.deltaTime * 25f * (Mathf.Abs(num4 - num3) + 0.01f))));
		if (activated)
		{
			base.transform.localEulerAngles = new Vector3(0f - rotationX + additionalRotationX, 0f, num2);
		}
		if (Input.GetKeyDown(KeyCode.F1) && Debug.isDebugBuild)
		{
			if (Cursor.lockState != CursorLockMode.Locked)
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
			else
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}
		if (cameraShaking > 0f)
		{
			_ = defaultPos;
			Vector3 position = base.transform.parent.position;
			if (cameraShaking > 1f)
			{
				targetPos = new Vector3(defaultPos.x + (float)Random.Range(-1, 2), defaultPos.y + (float)Random.Range(-1, 2), defaultPos.z);
			}
			else
			{
				targetPos = new Vector3(defaultPos.x + cameraShaking * Random.Range(-1f, 1f), defaultPos.y + cameraShaking * Random.Range(-1f, 1f), defaultPos.z);
			}
			if (Physics.Raycast(position + defaultPos, targetPos - defaultPos, out var hitInfo, Vector3.Distance(targetPos, defaultPos), environmentMask))
			{
				base.transform.position = hitInfo.point - (targetPos - defaultPos).normalized * 0.5f;
			}
			else
			{
				base.transform.localPosition = targetPos;
			}
			cameraShaking -= Time.deltaTime * 3f;
		}
		else if (pm.walking && pm.standing && !pm.rising)
		{
			base.transform.localPosition = Vector3.MoveTowards(base.transform.localPosition, targetPos, Time.deltaTime * 0.5f);
			if (base.transform.localPosition == targetPos && targetPos != defaultPos)
			{
				targetPos = defaultPos;
			}
			else if (base.transform.localPosition == targetPos && targetPos == defaultPos)
			{
				targetPos = new Vector3(defaultPos.x, defaultPos.y - 0.1f, defaultPos.z);
			}
		}
		else
		{
			base.transform.localPosition = defaultPos;
			targetPos = new Vector3(defaultPos.x, defaultPos.y - 0.1f, defaultPos.z);
		}
	}

	/// <summary>
	/// True si hay un menu abierto (pausa, opciones, tienda, menu principal).
	/// En esos menus el juego suelta el raton a proposito para que se pueda
	/// pinchar en los botones; si se vuelve a bloquear, el navegador congela el
	/// puntero y el EventSystem recibe siempre la misma posicion (el menu deja
	/// de responder).
	/// </summary>
	private static bool IsMenuOpen()
	{
		OptionsManager om = MonoSingleton<OptionsManager>.Instance;
		if (om != null)
		{
			return om.paused || om.mainMenu;
		}
		return Cursor.visible;
	}

	private void FixedUpdate()
	{
		if (slowDown < timeScale * timeScaleModifier)
		{
			slowDown = Mathf.MoveTowards(slowDown, timeScale * timeScaleModifier, 0.02f);
			Time.timeScale = slowDown;
			AudioMixer[] array = audmix;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetFloat("allPitch", slowDown);
			}
		}
	}

	public void CameraShake(float shakeAmount)
	{
		float num = PlayerPrefs.GetFloat("ScrSha", 100f) / 100f;
		if (num != 0f && cameraShaking < shakeAmount * num)
		{
			cameraShaking = shakeAmount * num;
		}
	}

	public void StopShake()
	{
		cameraShaking = 0f;
		base.transform.localPosition = defaultPos;
	}

	public void HitStop(float length)
	{
		if (length > currentStop)
		{
			currentStop = length;
			Time.timeScale = 0f;
			StartCoroutine(TimeIsStopped(length, trueStop: false));
		}
	}

	public void TrueStop(float length)
	{
		if (length > currentStop)
		{
			currentStop = length;
			AudioMixer[] array = audmix;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetFloat("allPitch", 0f);
			}
			Time.timeScale = 0f;
			StartCoroutine(TimeIsStopped(length, trueStop: true));
		}
	}

	private IEnumerator TimeIsStopped(float length, bool trueStop)
	{
		yield return new WaitForSecondsRealtime(length);
		ContinueTime(length, trueStop);
	}

	private void ContinueTime(float length, bool trueStop)
	{
		if (!(length >= currentStop))
		{
			return;
		}
		Time.timeScale = timeScale * timeScaleModifier;
		if (trueStop)
		{
			AudioMixer[] array = audmix;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetFloat("allPitch", 1f);
			}
		}
		currentStop = 0f;
	}

	public void ResetCamera(float degrees, bool instant)
	{
		if (instant)
		{
			rotationY = degrees;
			rotationX = 0f;
		}
	}

	public void Zoom(float amount)
	{
		zooming = true;
		zoomTarget = amount;
	}

	public void StopZoom()
	{
		zooming = false;
	}

	public void ResetToDefaultPos()
	{
		base.transform.localPosition = defaultPos;
		targetPos = new Vector3(defaultPos.x, defaultPos.y - 0.1f, defaultPos.z);
	}

	public Vector3 GetDefaultPos()
	{
		return base.transform.parent.position + defaultPos;
	}

	public void SlowDown(float amount)
	{
		if (amount <= 0f)
		{
			amount = 0.01f;
		}
		slowDown = amount;
	}

	public void CheckAspectRatio()
	{
		if (hudCamera == null)
		{
			hudCamera = base.transform.Find("HUD Camera").GetComponent<Camera>();
		}
		if (!cam)
		{
			cam = GetComponent<Camera>();
		}
		if (aspectRatio != cam.aspect)
		{
			aspectRatio = cam.aspect;
			float x = Mathf.Min(aspectRatio / 1.778f, 1f);
			hudCamera.transform.localScale = new Vector3(x, 1f, 1f);
		}
	}

	public void CheckTilt()
	{
		if (PlayerPrefs.GetInt("CamTil", 1) == 1)
		{
			tilt = true;
		}
		else
		{
			tilt = false;
		}
	}

	public void CheckMouseReverse()
	{
		if (PlayerPrefs.GetInt("RevMouX", 0) == 1)
		{
			reverseX = true;
		}
		else
		{
			reverseX = false;
		}
		if (PlayerPrefs.GetInt("RevMouY", 0) == 1)
		{
			reverseY = true;
		}
		else
		{
			reverseY = false;
		}
	}
}
