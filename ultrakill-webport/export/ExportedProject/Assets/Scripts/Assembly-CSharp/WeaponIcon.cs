using UnityEngine;

public class WeaponIcon : MonoBehaviour
{
	public Sprite weaponIcon;

	public Sprite glowIcon;

	public int variationColor;

	private void OnEnable()
	{
		UpdateIcon();
	}

	public void UpdateIcon()
	{
		if ((bool)MonoSingleton<WeaponHUD>.Instance)
		{
			MonoSingleton<WeaponHUD>.Instance.UpdateImage(weaponIcon, glowIcon, variationColor);
		}
	}
}
