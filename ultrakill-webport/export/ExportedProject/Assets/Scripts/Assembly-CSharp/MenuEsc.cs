using UnityEngine;

public class MenuEsc : MonoBehaviour
{
	public GameObject previousPage;

	private void Update()
	{
		if (MonoSingleton<InputManager>.Instance.InputSource.Pause.WasPerformedThisFrame)
		{
			previousPage.SetActive(value: true);
			base.gameObject.SetActive(value: false);
		}
	}
}
