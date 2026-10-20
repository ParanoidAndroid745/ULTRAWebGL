using UnityEngine;
using UnityEngine.UI;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class PowerUpMeter : MonoSingleton<PowerUpMeter>
{
	public float juice;

	public float latestMaxJuice;

	private Image meter;

	public Image vignette;

	public Color powerUpColor;

	private Color currentColor;

	private void Start()
	{
		meter = GetComponent<Image>();
		meter.fillAmount = 0f;
	}

	private void Update()
	{
		if (juice > 0f)
		{
			juice -= Time.deltaTime;
			meter.fillAmount = juice / latestMaxJuice;
			if (currentColor != powerUpColor)
			{
				currentColor = powerUpColor;
				currentColor.a = juice / latestMaxJuice;
				vignette.color = currentColor;
			}
		}
		else
		{
			latestMaxJuice = 0f;
			meter.fillAmount = 0f;
			if (vignette.color.a != 0f)
			{
				currentColor.a = 0f;
				vignette.color = currentColor;
			}
		}
	}
}
