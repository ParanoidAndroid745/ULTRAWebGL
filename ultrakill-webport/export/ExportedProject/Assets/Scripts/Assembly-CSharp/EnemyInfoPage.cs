using UnityEngine;
using UnityEngine.UI;

public class EnemyInfoPage : MonoBehaviour
{
	[SerializeField]
	private Text enemyPageTitle;

	[SerializeField]
	private Text enemyPageContent;

	[SerializeField]
	private Transform enemyPreviewWrapper;

	[Space]
	[SerializeField]
	private Transform enemyList;

	[SerializeField]
	private GameObject buttonTemplate;

	[SerializeField]
	private Image buttonTemplateBackground;

	[SerializeField]
	private Image buttonTemplateForeground;

	[SerializeField]
	private Sprite lockedSprite;

	[Space]
	[SerializeField]
	private SpawnableObjectsDatabase objects;

	private void Awake()
	{
		SpawnableObject[] enemies = objects.enemies;
		foreach (SpawnableObject spawnableObject in enemies)
		{
			bool flag = true;
			if (!MonoSingleton<BestiaryData>.Instance.GetEnemy(spawnableObject.enemyType))
			{
				flag = false;
			}
			if (flag)
			{
				buttonTemplateBackground.color = spawnableObject.backgroundColor;
				buttonTemplateForeground.sprite = spawnableObject.gridIcon;
			}
			else
			{
				buttonTemplateBackground.color = Color.gray;
				buttonTemplateForeground.sprite = lockedSprite;
			}
			GameObject gameObject = Object.Instantiate(buttonTemplate, enemyList);
			gameObject.SetActive(value: true);
			if (flag)
			{
				gameObject.GetComponentInChildren<ShopButton>().deactivated = false;
				gameObject.GetComponentInChildren<Button>().onClick.AddListener(() =>
				{
					DisplayInfo(spawnableObject);
				});
			}
			else
			{
				gameObject.GetComponentInChildren<ShopButton>().deactivated = true;
			}
		}
		buttonTemplate.SetActive(value: false);
	}

	private void SwapLayers(Transform target, int layer)
	{
		foreach (Transform item in target)
		{
			item.gameObject.layer = layer;
			if (item.childCount > 0)
			{
				SwapLayers(item, layer);
			}
		}
	}

	private void DisplayInfo(SpawnableObject source)
	{
		enemyPageTitle.text = source.objectName;
		enemyPageContent.text = source.description;
		enemyPageContent.rectTransform.localPosition = new Vector3(enemyPageContent.rectTransform.localPosition.x, 0f, enemyPageContent.rectTransform.localPosition.z);
		for (int i = 0; i < enemyPreviewWrapper.childCount; i++)
		{
			Object.Destroy(enemyPreviewWrapper.GetChild(i).gameObject);
		}
		GameObject gameObject = Object.Instantiate(source.preview, enemyPreviewWrapper);
		int layer = enemyPreviewWrapper.gameObject.layer;
		SwapLayers(gameObject.transform, layer);
		gameObject.layer = layer;
		gameObject.transform.localPosition = source.menuOffset;
		Spin spin = gameObject.AddComponent<Spin>();
		spin.spinDirection = new Vector3(0f, 1f, 0f);
		spin.speed = 10f;
	}
}
