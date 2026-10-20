using System.Collections;
using UnityEngine;

public class DebugArm : MonoBehaviour
{
	[HideInInspector]
	public GunControl gunCtrl;

	[HideInInspector]
	public CameraController cameraCtrl;

	[SerializeField]
	private LayerMask raycastLayers;

	[SerializeField]
	private Transform holder;

	[SerializeField]
	private Animator armAnimator;

	[SerializeField]
	private AudioSource jabSound;

	[SerializeField]
	private AudioSource selectSound;

	private SpawnMenu menu;

	private GameObject currentPreview;

	private SpawnableObject currentObject;

	private GoreZone goreZone;

	public static GoreZone debugZone;

	private static readonly int Holding = Animator.StringToHash("Holding");

	private static readonly int Punch = Animator.StringToHash("Punch");

	private void Awake()
	{
		menu = Object.FindObjectOfType<HUDOptions>().GetComponentInChildren<SpawnMenu>(includeInactive: true);
		if ((bool)menu.arm)
		{
			Object.Destroy(base.gameObject);
		}
		menu.arm = this;
	}

	private GoreZone GetGoreZone()
	{
		if (!goreZone)
		{
			goreZone = new GameObject("Debug Gore Zone").AddComponent<GoreZone>();
			debugZone = goreZone;
		}
		return goreZone;
	}

	public void PreviewObject(SpawnableObject obj)
	{
		selectSound.Play();
		currentObject = obj;
		GameObject gameObject = Object.Instantiate(obj.preview, holder, worldPositionStays: false);
		gameObject.transform.localPosition += obj.armOffset;
		gameObject.transform.Rotate(obj.armRotationOffset);
		menu.gameObject.SetActive(value: false);
		MonoSingleton<OptionsManager>.Instance.UnFreeze();
		if ((bool)currentPreview)
		{
			Object.Destroy(currentPreview);
		}
		currentPreview = gameObject;
	}

	private void OnEnable()
	{
		armAnimator.SetBool(Holding, value: true);
		holder.gameObject.SetActive(value: true);
	}

	private IEnumerator HandClosedAnimationThing()
	{
		holder.gameObject.SetActive(value: false);
		yield return new WaitForSeconds(0.85f);
		holder.gameObject.SetActive(value: true);
	}

	private void Update()
	{
		if (Time.timeScale == 0f)
		{
			return;
		}
		if (MonoSingleton<InputManager>.Instance.InputSource.Fire1.WasPerformedThisFrame)
		{
			if (menu.gameObject.activeSelf || currentObject == null)
			{
				return;
			}
			StopAllCoroutines();
			StartCoroutine(HandClosedAnimationThing());
			jabSound.Play();
			armAnimator.SetTrigger(Punch);
			if (Physics.Raycast(cameraCtrl.transform.position, cameraCtrl.transform.forward, out var hitInfo, 50f, raycastLayers))
			{
				Object.Instantiate(currentObject.gameObject, hitInfo.point, Quaternion.identity, GetGoreZone().transform);
			}
		}
		if (MonoSingleton<InputManager>.Instance.InputSource.Fire2.WasPerformedThisFrame)
		{
			menu.gameObject.SetActive(value: true);
			MonoSingleton<OptionsManager>.Instance.Freeze();
		}
	}

	public void ActiveStart()
	{
	}

	public void ActiveEnd()
	{
	}

	public void ReadyToPunch()
	{
	}

	public void PunchEnd()
	{
	}
}
