using System;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadFailMessage : MonoSingleton<SaveLoadFailMessage>
{
	public enum SaveLoadError
	{
		Generic = 0,
		TempValidation = 1
	}

	[SerializeField]
	private GameObject errorGeneric;

	[SerializeField]
	private GameObject errorTempValidation;

	[SerializeField]
	private Text tempErrorCode;

	private SaveLoadError currentError;

	private bool beenActivated;

	private Action potentialSaveRedo;

	public void ShowError(SaveLoadError error, string errorCode, Action saveRedo)
	{
		currentError = error;
		switch (error)
		{
		case SaveLoadError.Generic:
			errorGeneric.SetActive(value: true);
			break;
		case SaveLoadError.TempValidation:
			potentialSaveRedo = saveRedo;
			errorTempValidation.SetActive(value: true);
			if (!string.IsNullOrEmpty(errorCode))
			{
				tempErrorCode.text = errorCode;
				tempErrorCode.gameObject.SetActive(value: true);
			}
			break;
		}
		beenActivated = true;
	}

	private void HideAll()
	{
		foreach (Transform item in base.transform)
		{
			item.gameObject.SetActive(value: false);
		}
		beenActivated = false;
	}

	private void Update()
	{
		if (!beenActivated)
		{
			return;
		}
		switch (currentError)
		{
		case SaveLoadError.Generic:
			if (Input.GetKeyDown(KeyCode.Y))
			{
				HideAll();
			}
			if (Input.GetKeyDown(KeyCode.N))
			{
				Application.Quit();
			}
			break;
		case SaveLoadError.TempValidation:
			if (Input.GetKeyDown(KeyCode.Y))
			{
				if (potentialSaveRedo == null)
				{
					break;
				}
				HideAll();
				potentialSaveRedo();
			}
			if (Input.GetKeyDown(KeyCode.N))
			{
				HideAll();
				MonoSingleton<ProgressChecker>.Instance.DisableSaving();
			}
			break;
		}
	}
}
