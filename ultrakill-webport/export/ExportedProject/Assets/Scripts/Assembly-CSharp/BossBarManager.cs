using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class BossBarManager : MonoSingleton<BossBarManager>
{
	[Serializable]
	public class SliderLayer
	{
		public Color color;

		public Color afterImageColor;
	}

	[Serializable]
	public class HealthLayer
	{
		public float health;
	}

	[SerializeField]
	private BossHealthBarTemplate template;

	[SerializeField]
	private SliderLayer[] layers;

	public void CreateBossBar(string bossName, HealthLayer[] healthLayers, ref Slider[] hpSliders, ref Slider[] hpAfterImages, ref GameObject bossBar)
	{
		Debug.Log("Creating boss bar for " + bossName);
		List<Slider> list = new List<Slider>();
		List<Slider> list2 = new List<Slider>();
		BossHealthBarTemplate bossHealthBarTemplate = UnityEngine.Object.Instantiate(template, template.transform.parent);
		bossHealthBarTemplate.gameObject.SetActive(value: true);
		bossHealthBarTemplate.bossNameText.text = bossName;
		bossBar = bossHealthBarTemplate.gameObject;
		float num = 0f;
		for (int i = 0; i < healthLayers.Length; i++)
		{
			BossHealthSliderTemplate bossHealthSliderTemplate = UnityEngine.Object.Instantiate(bossHealthBarTemplate.sliderTemplate, bossHealthBarTemplate.sliderTemplate.transform.parent);
			list2.Add(bossHealthSliderTemplate.slider);
			bossHealthSliderTemplate.slider.minValue = num;
			bossHealthSliderTemplate.slider.maxValue = num + healthLayers[i].health;
			bossHealthSliderTemplate.gameObject.SetActive(value: true);
			bossHealthSliderTemplate.background.SetActive(i == 0);
			bossHealthSliderTemplate.fill.color = layers[i].afterImageColor;
			BossHealthSliderTemplate bossHealthSliderTemplate2 = UnityEngine.Object.Instantiate(bossHealthBarTemplate.sliderTemplate, bossHealthBarTemplate.sliderTemplate.transform.parent);
			list.Add(bossHealthSliderTemplate2.slider);
			bossHealthSliderTemplate2.slider.minValue = num;
			bossHealthSliderTemplate2.slider.maxValue = num + healthLayers[i].health;
			bossHealthSliderTemplate2.gameObject.SetActive(value: true);
			bossHealthSliderTemplate2.background.SetActive(value: false);
			bossHealthSliderTemplate2.fill.color = layers[i].color;
			num += healthLayers[i].health;
		}
		hpSliders = list.ToArray();
		hpAfterImages = list2.ToArray();
		template.sliderTemplate.gameObject.SetActive(value: false);
		template.gameObject.SetActive(value: false);
		bossBar.SetActive(value: false);
	}
}
