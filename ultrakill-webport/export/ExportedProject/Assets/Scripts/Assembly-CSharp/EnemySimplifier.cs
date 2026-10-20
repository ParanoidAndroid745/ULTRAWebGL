using UnityEngine;

[DefaultExecutionOrder(200)]
public class EnemySimplifier : MonoBehaviour
{
	private Material currentMaterial;

	public Material enragedMaterial;

	[HideInInspector]
	public Material originalMaterial;

	public Material simplifiedMaterial;

	public Material enragedSimplifiedMaterial;

	private Renderer meshrenderer;

	[HideInInspector]
	public bool enraged;

	[HideInInspector]
	public bool sandified;

	private OptionsManager oman;

	private GameObject player;

	[HideInInspector]
	public LayerMask lmask;

	private bool active = true;

	private bool simplify;

	private bool playerDistCheck;

	private EnemyType enemyColorType;

	private EnemyIdentifier eid;

	private void Awake()
	{
		if ((bool)enragedSimplifiedMaterial)
		{
			enragedSimplifiedMaterial = new Material(enragedSimplifiedMaterial);
		}
		if ((bool)simplifiedMaterial)
		{
			simplifiedMaterial = new Material(simplifiedMaterial);
		}
		eid = GetComponentInParent<EnemyIdentifier>();
		if (enemyColorType == EnemyType.Cerberus)
		{
			enemyColorType = eid.enemyType;
		}
	}

	private void Start()
	{
		lmask = (int)lmask | 0x100;
		lmask = (int)lmask | 0x1000000;
		meshrenderer = GetComponent<Renderer>();
		currentMaterial = meshrenderer.material;
		originalMaterial = currentMaterial;
		oman = MonoSingleton<OptionsManager>.Instance;
		player = MonoSingleton<NewMovement>.Instance.gameObject;
		UpdateColors();
	}

	private void OnEnable()
	{
		UpdateColors();
	}

	private void Update()
	{
		if (active)
		{
			SetOutline();
		}
	}

	public void SetOutline()
	{
		playerDistCheck = Vector3.Distance(base.transform.position, player.transform.position) > oman.simplifiedDistance;
		if (oman.simplifyEnemies && simplifiedMaterial != null && playerDistCheck && !oman.outlinesOnly)
		{
			meshrenderer.material = ((enraged && (bool)enragedSimplifiedMaterial) ? enragedSimplifiedMaterial : simplifiedMaterial);
		}
		else
		{
			meshrenderer.material = (enraged ? enragedMaterial : originalMaterial);
		}
		meshrenderer.material.SetFloat("_Outline", (oman.simplifyEnemies && playerDistCheck) ? 1 : 0);
		if ((bool)eid && !eid.dead && eid.sandified && !oman.simplifyEnemies)
		{
			meshrenderer.material.SetFloat("_HasSandBuff", 1f);
		}
	}

	public void UpdateColors()
	{
		if ((bool)simplifiedMaterial)
		{
			simplifiedMaterial.color = MonoSingleton<ColorBlindSettings>.Instance.GetEnemyColor(enemyColorType);
		}
		if ((bool)enragedSimplifiedMaterial)
		{
			enragedSimplifiedMaterial.color = MonoSingleton<ColorBlindSettings>.Instance.enrageColor;
		}
	}

	public void Begone()
	{
		active = false;
		if ((bool)meshrenderer)
		{
			meshrenderer.material.SetFloat("_Outline", 0f);
			meshrenderer.material = originalMaterial;
		}
		Object.Destroy(this);
	}

	public void ChangeMaterial(Material oldMaterial, Material newMaterial)
	{
		bool flag = false;
		if (currentMaterial == oldMaterial)
		{
			flag = true;
		}
		newMaterial = new Material(newMaterial);
		if (oldMaterial == originalMaterial)
		{
			originalMaterial = newMaterial;
		}
		else if (oldMaterial == simplifiedMaterial)
		{
			simplifiedMaterial = newMaterial;
		}
		else if (oldMaterial == enragedMaterial)
		{
			enragedMaterial = newMaterial;
		}
		else if (oldMaterial == enragedSimplifiedMaterial)
		{
			enragedSimplifiedMaterial = newMaterial;
		}
		if (flag)
		{
			meshrenderer.material = newMaterial;
			currentMaterial = newMaterial;
		}
	}
}
