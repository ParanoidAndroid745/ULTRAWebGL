using System.Collections.Generic;
using UnityEngine;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class BloodsplatterManager : MonoSingleton<BloodsplatterManager>
{
	public GameObject head;

	public GameObject limb;

	public GameObject body;

	public GameObject small;

	public GameObject splatter;

	public GameObject underwater;

	public GameObject sand;

	public GameObject brainChunk;

	public GameObject skullChunk;

	public GameObject eyeball;

	public GameObject jawChunk;

	public GameObject[] gib;

	private List<GameObject> heads = new List<GameObject>();

	private List<GameObject> limbs = new List<GameObject>();

	private List<GameObject> bodies = new List<GameObject>();

	private List<GameObject> smalls = new List<GameObject>();

	private List<GameObject> splatters = new List<GameObject>();

	private List<GameObject> underwaters = new List<GameObject>();

	private List<GameObject> sands = new List<GameObject>();

	private List<GameObject> brainChunks = new List<GameObject>();

	private List<GameObject> skullChunks = new List<GameObject>();

	private List<GameObject> eyeballs = new List<GameObject>();

	private List<GameObject> jawChunks = new List<GameObject>();

	private List<GameObject> gibs = new List<GameObject>();

	private int order;

	private Transform goreStore;

	private void Start()
	{
		goreStore = base.transform.GetChild(0);
		Invoke("Refresh", 0.1f);
	}

	private void Refresh()
	{
		switch (order)
		{
		case 0:
			CheckList(heads, head);
			break;
		case 1:
			CheckList(limbs, limb);
			break;
		case 2:
			CheckList(bodies, body);
			break;
		case 3:
			CheckList(smalls, small);
			CheckList(splatters, splatter);
			break;
		case 4:
			CheckList(underwaters, underwater);
			break;
		case 5:
			CheckList(sands, sand);
			break;
		case 6:
			CheckList(brainChunks, brainChunk);
			CheckList(skullChunks, skullChunk);
			break;
		case 7:
			CheckList(eyeballs, eyeball);
			CheckList(jawChunks, jawChunk);
			break;
		case 8:
			CheckListArray(gibs, gib);
			break;
		}
		order = (order + 1) % 9;
		Invoke("Refresh", 0.1f);
	}

	private void CheckList(List<GameObject> list, GameObject bs)
	{
		if (list.Count <= 20)
		{
			int num = list.Count + 10;
			for (int i = list.Count; i < num; i++)
			{
				GameObject item = Object.Instantiate(bs, goreStore);
				list.Add(item);
			}
		}
		else if (list.Count < 30)
		{
			for (int j = list.Count; j < 30; j++)
			{
				GameObject item2 = Object.Instantiate(bs, goreStore);
				list.Add(item2);
			}
		}
	}

	private void CheckListArray(List<GameObject> list, GameObject[] bs)
	{
		if (list.Count <= 20)
		{
			int num = list.Count + 10;
			for (int i = list.Count; i < num; i++)
			{
				GameObject item = Object.Instantiate(bs[Random.Range(0, bs.Length)], goreStore);
				list.Add(item);
			}
		}
		else if (list.Count < 30)
		{
			for (int j = list.Count; j < 30; j++)
			{
				GameObject item2 = Object.Instantiate(bs[Random.Range(0, bs.Length)], goreStore);
				list.Add(item2);
			}
		}
	}

	public GameObject GetGore(GoreType got, bool isUnderwater = false, bool isSandified = false)
	{
		if (isSandified)
		{
			if (sands.Count == 0)
			{
				CheckList(sands, sand);
			}
			if (sands.Count > 0)
			{
				GameObject gameObject = sands[0];
				sands.Remove(gameObject);
				if (got == GoreType.Head)
				{
					return gameObject;
				}
				AudioSource component = gameObject.GetComponent<AudioSource>();
				AudioSource component2 = gameObject.transform.GetChild(0).GetComponent<AudioSource>();
				AudioSource audioSource = null;
				switch (got)
				{
				case GoreType.Limb:
					audioSource = limb.GetComponent<AudioSource>();
					break;
				case GoreType.Body:
					audioSource = body.GetComponent<AudioSource>();
					break;
				case GoreType.Small:
					audioSource = small.GetComponent<AudioSource>();
					break;
				}
				if ((bool)audioSource)
				{
					component.clip = audioSource.clip;
					component.volume = audioSource.volume - 0.35f;
					component2.volume = audioSource.volume - 0.2f;
				}
				return gameObject;
			}
		}
		switch (got)
		{
		case GoreType.Head:
			if (isUnderwater)
			{
				if (underwaters.Count == 0)
				{
					CheckList(underwaters, underwater);
				}
				if (underwaters.Count > 0)
				{
					GameObject gameObject = underwaters[0];
					underwaters.Remove(gameObject);
					return gameObject;
				}
			}
			else
			{
				if (heads.Count == 0)
				{
					CheckList(heads, head);
				}
				if (heads.Count > 0)
				{
					GameObject gameObject = heads[0];
					heads.Remove(gameObject);
					return gameObject;
				}
			}
			break;
		case GoreType.Limb:
			if (isUnderwater)
			{
				if (underwaters.Count == 0)
				{
					CheckList(underwaters, underwater);
				}
				if (underwaters.Count > 0)
				{
					GameObject gameObject = underwaters[0];
					underwaters.Remove(gameObject);
					gameObject.transform.localScale *= 0.75f;
					gameObject.GetComponent<Bloodsplatter>().hpAmount = 20;
					AudioSource component7 = gameObject.GetComponent<AudioSource>();
					AudioSource component8 = limb.GetComponent<AudioSource>();
					component7.clip = component8.clip;
					component7.volume = component8.volume;
					return gameObject;
				}
			}
			else
			{
				if (limbs.Count == 0)
				{
					CheckList(limbs, limb);
				}
				if (limbs.Count > 0)
				{
					GameObject gameObject = limbs[0];
					limbs.Remove(gameObject);
					return gameObject;
				}
			}
			break;
		case GoreType.Body:
			if (isUnderwater)
			{
				if (underwaters.Count == 0)
				{
					CheckList(underwaters, underwater);
				}
				if (underwaters.Count > 0)
				{
					GameObject gameObject = underwaters[0];
					underwaters.Remove(gameObject);
					gameObject.transform.localScale *= 0.5f;
					gameObject.GetComponent<Bloodsplatter>().hpAmount = 10;
					AudioSource component5 = gameObject.GetComponent<AudioSource>();
					AudioSource component6 = body.GetComponent<AudioSource>();
					component5.clip = component6.clip;
					component5.volume = component6.volume;
					return gameObject;
				}
			}
			else
			{
				if (bodies.Count == 0)
				{
					CheckList(bodies, body);
				}
				if (bodies.Count > 0)
				{
					GameObject gameObject = bodies[0];
					bodies.Remove(gameObject);
					return gameObject;
				}
			}
			break;
		case GoreType.Small:
			if (isUnderwater)
			{
				if (underwaters.Count == 0)
				{
					CheckList(underwaters, underwater);
				}
				if (underwaters.Count > 0)
				{
					GameObject gameObject = underwaters[0];
					underwaters.Remove(gameObject);
					gameObject.transform.localScale *= 0.25f;
					gameObject.GetComponent<Bloodsplatter>().hpAmount = 10;
					AudioSource component9 = gameObject.GetComponent<AudioSource>();
					AudioSource component10 = small.GetComponent<AudioSource>();
					component9.clip = component10.clip;
					component9.volume = component10.volume;
					return gameObject;
				}
			}
			else
			{
				if (smalls.Count == 0)
				{
					CheckList(smalls, small);
				}
				if (smalls.Count > 0)
				{
					GameObject gameObject = smalls[0];
					smalls.Remove(gameObject);
					return gameObject;
				}
			}
			break;
		case GoreType.Splatter:
			if (isUnderwater)
			{
				if (underwaters.Count == 0)
				{
					CheckList(underwaters, underwater);
				}
				if (underwaters.Count > 0)
				{
					GameObject gameObject = underwaters[0];
					underwaters.Remove(gameObject);
					AudioSource component3 = gameObject.GetComponent<AudioSource>();
					AudioSource component4 = splatter.GetComponent<AudioSource>();
					component3.clip = component4.clip;
					component3.volume = component4.volume;
					return gameObject;
				}
			}
			else
			{
				if (splatters.Count == 0)
				{
					CheckList(splatters, splatter);
				}
				if (splatters.Count > 0)
				{
					GameObject gameObject = splatters[0];
					splatters.Remove(gameObject);
					return gameObject;
				}
			}
			break;
		}
		return null;
	}

	public GameObject GetGib(GibType got)
	{
		List<GameObject> list = gibs;
		switch (got)
		{
		case GibType.Brain:
			list = brainChunks;
			break;
		case GibType.Skull:
			list = skullChunks;
			break;
		case GibType.Eye:
			list = eyeballs;
			break;
		case GibType.Jaw:
			list = jawChunks;
			break;
		}
		if (list.Count > 0)
		{
			GameObject gameObject = list[0];
			list.Remove(gameObject);
			return gameObject;
		}
		return null;
	}
}
