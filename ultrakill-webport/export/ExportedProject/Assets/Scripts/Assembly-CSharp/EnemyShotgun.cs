using UnityEngine;

public class EnemyShotgun : MonoBehaviour
{
	public EnemyType safeEnemyType;

	private AudioSource gunAud;

	public AudioClip shootSound;

	public AudioClip clickSound;

	public AudioClip smackSound;

	private AudioSource heatSinkAud;

	public int variation;

	public GameObject bullet;

	public GameObject grenade;

	public float spread;

	private Animator anim;

	public bool gunReady = true;

	public Transform shootPoint;

	public GameObject muzzleFlash;

	private ParticleSystem[] parts;

	private bool charging;

	private AudioSource chargeSound;

	private float chargeAmount;

	public GameObject warningFlash;

	private int difficulty;

	private void Start()
	{
		gunAud = GetComponent<AudioSource>();
		anim = GetComponentInChildren<Animator>();
		parts = GetComponentsInChildren<ParticleSystem>();
		heatSinkAud = shootPoint.GetComponent<AudioSource>();
		chargeSound = base.transform.GetChild(0).GetComponent<AudioSource>();
		difficulty = PlayerPrefs.GetInt("Diff", 2);
		if (difficulty == 1)
		{
			spread *= 0.75f;
		}
		else if (difficulty == 0)
		{
			spread *= 0.5f;
		}
	}

	private void Update()
	{
		if (charging)
		{
			float num = 2f;
			if (difficulty == 1)
			{
				num = 1.5f;
			}
			if (difficulty == 0)
			{
				num = 1f;
			}
			chargeAmount = Mathf.MoveTowards(chargeAmount, 1f, Time.deltaTime * num);
			chargeSound.pitch = chargeAmount * 1.25f;
		}
	}

	public void Fire()
	{
		gunReady = false;
		int num = 12;
		anim.SetTrigger("Shoot");
		for (int i = 0; i < num; i++)
		{
			GameObject gameObject = Object.Instantiate(bullet, shootPoint.transform.position, shootPoint.transform.rotation);
			if (i > 0)
			{
				gameObject.transform.Rotate(Random.Range(0f - spread, spread), Random.Range(0f - spread, spread), Random.Range(0f - spread, spread));
			}
			Projectile component = gameObject.GetComponent<Projectile>();
			if ((bool)component)
			{
				component.safeEnemyType = safeEnemyType;
				if (difficulty == 1)
				{
					component.speed *= 0.75f;
				}
				else if (difficulty == 0)
				{
					component.speed *= 0.5f;
				}
			}
		}
		gunAud.clip = shootSound;
		gunAud.volume = 0.35f;
		gunAud.panStereo = 0f;
		gunAud.pitch = Random.Range(0.95f, 1.05f);
		gunAud.Play();
		Object.Instantiate(muzzleFlash, shootPoint.transform.position, shootPoint.transform.rotation);
	}

	public void AltFire()
	{
		gunReady = false;
		float num = 70f;
		if (difficulty == 1)
		{
			num = 50f;
		}
		else if (difficulty == 0)
		{
			num = 30f;
		}
		GameObject obj = Object.Instantiate(grenade, shootPoint.transform.position, Random.rotation);
		obj.GetComponent<Rigidbody>().AddForce(shootPoint.transform.forward * num, ForceMode.VelocityChange);
		Grenade componentInChildren = obj.GetComponentInChildren<Grenade>();
		if (componentInChildren != null)
		{
			componentInChildren.enemy = true;
		}
		anim.SetTrigger("Secondary Fire");
		gunAud.clip = shootSound;
		gunAud.volume = 0.35f;
		gunAud.panStereo = 0f;
		gunAud.pitch = Random.Range(0.75f, 0.85f);
		gunAud.Play();
		Object.Instantiate(muzzleFlash, shootPoint.transform.position, shootPoint.transform.rotation);
		charging = false;
		chargeAmount = 0f;
		chargeSound.pitch = 0f;
	}

	public void PrepareFire()
	{
		if (heatSinkAud == null)
		{
			heatSinkAud = shootPoint.GetComponent<AudioSource>();
		}
		heatSinkAud.Play();
		Object.Instantiate(warningFlash, shootPoint.transform.position, shootPoint.transform.rotation).transform.localScale *= 2f;
	}

	public void PrepareAltFire()
	{
		charging = true;
		chargeAmount = 0f;
		chargeSound.pitch = 0f;
	}

	public void ReleaseHeat()
	{
		ParticleSystem[] array = parts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Play();
		}
	}

	public void ClickSound()
	{
		gunAud.clip = clickSound;
		gunAud.volume = 0.5f;
		gunAud.pitch = Random.Range(0.95f, 1.05f);
		gunAud.Play();
	}

	public void ReadyGun()
	{
		gunReady = true;
	}

	public void Smack()
	{
		gunAud.clip = smackSound;
		gunAud.volume = 0.75f;
		gunAud.pitch = Random.Range(2f, 2.2f);
		gunAud.Play();
	}
}
