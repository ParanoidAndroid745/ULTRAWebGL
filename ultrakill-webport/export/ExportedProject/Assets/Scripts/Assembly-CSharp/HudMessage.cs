using UnityEngine;
using UnityEngine.UI;

public class HudMessage : MonoBehaviour
{
	private HudMessageReceiver messageHud;

	public bool timed;

	public bool deactivating;

	public bool notOneTime;

	public bool dontActivateOnTriggerEnter;

	private bool activated;

	public string message;

	public string input;

	public string message2;

	private Image img;

	private Text text;

	public string playerPref;

	private bool colliderless;

	private void Start()
	{
		if (GetComponent<Collider>() == null)
		{
			colliderless = true;
			if (playerPref == "")
			{
				PlayMessage();
			}
			else if (PlayerPrefs.GetInt(playerPref, 0) == 0)
			{
				PlayerPrefs.SetInt(playerPref, 1);
				PlayMessage();
			}
		}
	}

	private void OnEnable()
	{
		if (colliderless && (!activated || notOneTime))
		{
			if (playerPref == "")
			{
				PlayMessage();
			}
			else if (PlayerPrefs.GetInt(playerPref, 0) == 0)
			{
				PlayerPrefs.SetInt(playerPref, 1);
				PlayMessage();
			}
		}
	}

	private void Update()
	{
		if (activated && timed)
		{
			img.enabled = true;
			text.enabled = true;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!dontActivateOnTriggerEnter && other.gameObject.CompareTag("Player") && (!activated || notOneTime))
		{
			if (playerPref == "")
			{
				PlayMessage();
			}
			else if (PlayerPrefs.GetInt(playerPref, 0) == 0)
			{
				PlayerPrefs.SetInt(playerPref, 1);
				PlayMessage();
			}
		}
	}

	private void Done()
	{
		img.enabled = false;
		text.enabled = false;
		activated = false;
		Begone();
	}

	private void Begone()
	{
		if (!notOneTime)
		{
			Object.Destroy(this);
		}
	}

	public void PlayMessage()
	{
		activated = true;
		messageHud = MonoSingleton<HudMessageReceiver>.Instance;
		this.text = messageHud.text;
		if (input == "")
		{
			this.text.text = message;
		}
		else
		{
			string text = "";
			KeyCode keyCode = MonoSingleton<InputManager>.Instance.Inputs[input];
			switch (keyCode)
			{
			case KeyCode.Mouse0:
				text = "Left Mouse Button";
				break;
			case KeyCode.Mouse1:
				text = "Right Mouse Button";
				break;
			case KeyCode.Mouse2:
				text = "Middle Mouse Button";
				break;
			default:
				text = keyCode.ToString();
				break;
			}
			this.text.text = message + text + message2;
		}
		this.text.text = this.text.text.Replace('$', '\n');
		this.text.enabled = true;
		img = messageHud.GetComponent<Image>();
		img.enabled = true;
		if (deactivating)
		{
			Done();
		}
		else
		{
			messageHud.GetComponent<AudioSource>().Play();
		}
		if (timed && notOneTime)
		{
			CancelInvoke("Done");
			Invoke("Done", 5f);
		}
		else if (timed)
		{
			Invoke("Done", 5f);
		}
		else
		{
			Invoke("Begone", 1f);
		}
		messageHud.GetComponent<HudOpenEffect>().Force();
	}
}
