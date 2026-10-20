using UnityEngine;

public class TimeBomb : MonoBehaviour
{
	public bool dontStartOnAwake;

	private bool activated;

	public float timer;

	private AudioSource aud;

	public GameObject beepLight;

	public float beeperSize;

	[HideInInspector]
	public float beeperSizeMultiplier = 1f;

	private GameObject beeper;

	private Vector3 origScale;

	public Color beeperColor = Color.white;

	private SpriteRenderer beeperSpriteRenderer;

	public float beeperPitch = 0.65f;

	public GameObject explosion;

	public bool dontExplode;

	private void Start()
	{
		if (!dontStartOnAwake)
		{
			StartCountdown();
		}
	}

	private void OnDestroy()
	{
		if (!dontExplode && explosion != null)
		{
			Object.Instantiate(explosion, base.transform.position, base.transform.rotation);
		}
	}

	private void Update()
	{
		if (activated)
		{
			timer = Mathf.MoveTowards(timer, 0f, Time.deltaTime);
			if ((bool)beeperSpriteRenderer)
			{
				beeperSpriteRenderer.color = beeperColor;
			}
			if ((bool)aud)
			{
				aud.pitch = beeperPitch;
			}
			if (timer != 0f)
			{
				beeper.transform.localScale = Vector3.Lerp(beeper.transform.localScale, Vector3.zero, Time.deltaTime * 5f);
			}
			else
			{
				Object.Destroy(base.gameObject);
			}
		}
	}

	public void StartCountdown()
	{
		if (!activated)
		{
			activated = true;
			Beep();
		}
	}

	private void Beep()
	{
		if (beeper == null)
		{
			beeper = Object.Instantiate(beepLight, base.transform.position, base.transform.rotation);
			beeper.transform.SetParent(base.transform, worldPositionStays: true);
			aud = beeper.GetComponent<AudioSource>();
			origScale = new Vector3(beeperSize, beeperSize, 1f);
			beeperSpriteRenderer = beeper.GetComponent<SpriteRenderer>();
			aud.pitch = beeperPitch;
		}
		aud.Play();
		beeper.transform.localScale = origScale * beeperSizeMultiplier;
		Invoke("Beep", timer / 6f);
	}
}
