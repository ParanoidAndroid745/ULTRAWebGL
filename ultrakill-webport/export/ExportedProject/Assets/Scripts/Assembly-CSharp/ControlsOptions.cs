using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ControlsOptions : MonoBehaviour
{
	private InputManager inman;

	public OptionsManager opm;

	private List<Text> allTexts = new List<Text>();

	public Text wText;

	public Text sText;

	public Text aText;

	public Text dText;

	public Text jumpText;

	public Text dodgeText;

	public Text slideText;

	public Text fire1Text;

	public Text fire2Text;

	public Text punchText;

	public Text lastUsedWeaponText;

	public Text changeVariation;

	public Text changeFist;

	public Text slot1text;

	public Text slot2text;

	public Text slot3text;

	public Text slot4text;

	public Toggle scrollWheel;

	public GameObject scrollSettings;

	public Dropdown variationWheel;

	public Toggle reverseWheel;

	private GameObject currentKey;

	public Color normalColor;

	public Color pressedColor;

	private bool canUnpause;

	private void Start()
	{
		inman = MonoSingleton<InputManager>.Instance;
		opm = MonoSingleton<OptionsManager>.Instance;
		wText.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeyW", 119));
		sText.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeyS", 115));
		aText.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeyA", 97));
		dText.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeyD", 100));
		jumpText.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeyJump", 32));
		dodgeText.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeyDodge", 304));
		slideText.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeySlide", 306));
		fire1Text.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeyFire1", 323));
		fire2Text.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeyFire2", 324));
		punchText.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeyPunch", 102));
		lastUsedWeaponText.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeyLastUsedWeapon", 113));
		changeVariation.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeyChangeVariation", 101));
		changeFist.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeyChangeFist", 103));
		slot1text.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeySlot1", 49));
		slot2text.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeySlot2", 50));
		slot3text.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeySlot3", 51));
		slot4text.text = GetKeyName((KeyCode)PlayerPrefs.GetInt("KeySlot4", 52));
		allTexts.Add(wText);
		allTexts.Add(sText);
		allTexts.Add(aText);
		allTexts.Add(dText);
		allTexts.Add(jumpText);
		allTexts.Add(dodgeText);
		allTexts.Add(slideText);
		allTexts.Add(fire1Text);
		allTexts.Add(fire2Text);
		allTexts.Add(punchText);
		allTexts.Add(lastUsedWeaponText);
		allTexts.Add(changeVariation);
		allTexts.Add(changeFist);
		allTexts.Add(slot1text);
		allTexts.Add(slot2text);
		allTexts.Add(slot3text);
		allTexts.Add(slot4text);
		if (PlayerPrefs.GetInt("ScrOn", 1) == 0)
		{
			scrollWheel.isOn = false;
			scrollSettings.SetActive(value: false);
		}
		int num = PlayerPrefs.GetInt("ScrVar", 0);
		int num2 = PlayerPrefs.GetInt("ScrWep", 1);
		if (num == 1 && num2 == 1)
		{
			variationWheel.value = 2;
		}
		else if (num == 1 && num2 == 0)
		{
			variationWheel.value = 1;
		}
		else
		{
			variationWheel.value = 0;
		}
		if (PlayerPrefs.GetInt("ScrRev", 0) == 1)
		{
			reverseWheel.isOn = true;
		}
	}

	private void OnDisable()
	{
		if (currentKey != null)
		{
			if (opm == null)
			{
				opm = MonoSingleton<OptionsManager>.Instance;
			}
			currentKey.GetComponent<Image>().color = normalColor;
			currentKey = null;
			opm.dontUnpause = false;
		}
	}

	private void LateUpdate()
	{
		if (canUnpause)
		{
			if (opm == null)
			{
				opm = MonoSingleton<OptionsManager>.Instance;
			}
			canUnpause = false;
			opm.dontUnpause = false;
		}
	}

	private void OnGUI()
	{
		if (!(currentKey != null))
		{
			return;
		}
		Event current = Event.current;
		if (current.keyCode == KeyCode.Escape)
		{
			currentKey.GetComponent<Image>().color = normalColor;
			currentKey = null;
			canUnpause = true;
		}
		// Con Ctrl/Alt/Meta no se reasigna ninguna tecla: si el navegador no
		// tiene el teclado bloqueado, una combinacion como Ctrl+D abriria el
		// dialogo de marcadores de Chrome Y ademas se guardaria como tecla del
		// juego. (Event.command es Ctrl en Windows.)
		if (current.control || current.alt || current.command)
		{
			return;
		}
		if (current.isKey || current.isMouse || current.button > 2 || current.shift)
		{
			KeyCode keyCode = KeyCode.Alpha0;
			if (current.isKey)
			{
				keyCode = current.keyCode;
			}
			else if (Input.GetKey(KeyCode.LeftShift))
			{
				keyCode = KeyCode.LeftShift;
			}
			else if (Input.GetKey(KeyCode.RightShift))
			{
				keyCode = KeyCode.RightShift;
			}
			else
			{
				if (current.button > 6)
				{
					currentKey.GetComponent<Image>().color = normalColor;
					currentKey = null;
					canUnpause = true;
					return;
				}
				keyCode = (KeyCode)(323 + current.button);
			}
			if (inman == null)
			{
				inman = MonoSingleton<InputManager>.Instance;
			}
			inman.Inputs[currentKey.name] = keyCode;
			currentKey.GetComponentInChildren<Text>().text = GetKeyName(keyCode);
			PlayerPrefs.SetInt("Key" + currentKey.name, (int)keyCode);
			inman.UpdateBindings();
			currentKey.GetComponent<Image>().color = normalColor;
			currentKey = null;
			canUnpause = true;
		}
		else if (Input.GetKey(KeyCode.Mouse3) || Input.GetKey(KeyCode.Mouse4) || Input.GetKey(KeyCode.Mouse5) || Input.GetKey(KeyCode.Mouse6))
		{
			KeyCode keyCode2 = KeyCode.Mouse3;
			if (Input.GetKey(KeyCode.Mouse4))
			{
				keyCode2 = KeyCode.Mouse4;
			}
			else if (Input.GetKey(KeyCode.Mouse5))
			{
				keyCode2 = KeyCode.Mouse5;
			}
			else if (Input.GetKey(KeyCode.Mouse6))
			{
				keyCode2 = KeyCode.Mouse6;
			}
			if (inman == null)
			{
				inman = MonoSingleton<InputManager>.Instance;
			}
			inman.Inputs[currentKey.name] = keyCode2;
			currentKey.GetComponentInChildren<Text>().text = GetKeyName(keyCode2);
			PlayerPrefs.SetInt("Key" + currentKey.name, (int)keyCode2);
			inman.UpdateBindings();
			currentKey.GetComponent<Image>().color = normalColor;
			currentKey = null;
			canUnpause = true;
		}
		else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
		{
			KeyCode keyCode3 = KeyCode.LeftShift;
			if (Input.GetKey(KeyCode.RightShift))
			{
				keyCode3 = KeyCode.RightShift;
			}
			inman.Inputs[currentKey.name] = keyCode3;
			currentKey.GetComponentInChildren<Text>().text = GetKeyName(keyCode3);
			PlayerPrefs.SetInt("Key" + currentKey.name, (int)keyCode3);
			inman.UpdateBindings();
			currentKey.GetComponent<Image>().color = normalColor;
			currentKey = null;
			canUnpause = true;
		}
	}

	public void ChangeKey(GameObject stuff)
	{
		if (opm == null)
		{
			opm = MonoSingleton<OptionsManager>.Instance;
		}
		opm.dontUnpause = true;
		currentKey = stuff;
		stuff.GetComponent<Image>().color = pressedColor;
	}

	public void ResetKeys()
	{
		if (inman == null)
		{
			inman = MonoSingleton<InputManager>.Instance;
		}
		for (int i = 0; i < allTexts.Count; i++)
		{
			switch (i)
			{
			case 0:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.W;
				break;
			case 1:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.S;
				break;
			case 2:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.A;
				break;
			case 3:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.D;
				break;
			case 4:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.Space;
				break;
			case 5:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.LeftShift;
				break;
			case 6:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.LeftControl;
				break;
			case 7:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.Mouse0;
				break;
			case 8:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.Mouse1;
				break;
			case 9:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.F;
				break;
			case 10:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.Q;
				break;
			case 11:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.E;
				break;
			case 12:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.G;
				break;
			case 13:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.Alpha1;
				break;
			case 14:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.Alpha2;
				break;
			case 15:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.Alpha3;
				break;
			case 16:
				inman.Inputs[allTexts[i].transform.parent.name] = KeyCode.Alpha4;
				break;
			}
			PlayerPrefs.SetInt("Key" + allTexts[i].transform.parent.name, (int)inman.Inputs[allTexts[i].transform.parent.name]);
			allTexts[i].text = GetKeyName((KeyCode)PlayerPrefs.GetInt("Key" + allTexts[i].transform.parent.name));
			inman.UpdateBindings();
		}
	}

	private string GetKeyName(KeyCode key)
	{
		switch (key)
		{
		case KeyCode.Alpha1:
			return "1";
		case KeyCode.Alpha2:
			return "2";
		case KeyCode.Alpha3:
			return "3";
		case KeyCode.Alpha4:
			return "4";
		case KeyCode.Alpha5:
			return "5";
		case KeyCode.Alpha6:
			return "6";
		case KeyCode.Alpha7:
			return "7";
		case KeyCode.Alpha8:
			return "8";
		case KeyCode.Alpha9:
			return "9";
		case KeyCode.Alpha0:
			return "0";
		case KeyCode.Mouse0:
			return "Left Mouse Button";
		case KeyCode.Mouse1:
			return "Right Mouse Button";
		case KeyCode.Mouse2:
			return "Middle Mouse Button";
		case KeyCode.Mouse3:
			return "Mouse4";
		case KeyCode.Mouse4:
			return "Mouse5";
		case KeyCode.Mouse5:
			return "Mouse6";
		case KeyCode.Mouse6:
			return "Mouse7";
		case KeyCode.LeftShift:
			return "Left Shift";
		case KeyCode.RightShift:
			return "Right Shift";
		case KeyCode.LeftControl:
			return "Left Control";
		case KeyCode.RightControl:
			return "Right Control";
		case KeyCode.LeftAlt:
			return "Left Alt";
		case KeyCode.RightAlt:
			return "Right Alt";
		default:
			return key.ToString();
		}
	}

	public void ScrollOn(bool stuff)
	{
		if (inman == null)
		{
			inman = MonoSingleton<InputManager>.Instance;
		}
		if (stuff)
		{
			PlayerPrefs.SetInt("ScrOn", 1);
			inman.ScrOn = true;
			scrollSettings.SetActive(value: true);
		}
		else
		{
			PlayerPrefs.SetInt("ScrOn", 0);
			inman.ScrOn = false;
			scrollSettings.SetActive(value: false);
		}
	}

	public void ScrollVariations(int stuff)
	{
		if (inman == null)
		{
			inman = MonoSingleton<InputManager>.Instance;
		}
		switch (stuff)
		{
		case 0:
			PlayerPrefs.SetInt("ScrWep", 1);
			PlayerPrefs.SetInt("ScrVar", 0);
			inman.ScrWep = true;
			inman.ScrVar = false;
			break;
		case 1:
			PlayerPrefs.SetInt("ScrWep", 0);
			PlayerPrefs.SetInt("ScrVar", 1);
			inman.ScrWep = false;
			inman.ScrVar = true;
			break;
		default:
			PlayerPrefs.SetInt("ScrWep", 1);
			PlayerPrefs.SetInt("ScrVar", 1);
			inman.ScrWep = true;
			inman.ScrVar = true;
			break;
		}
	}

	public void ScrollReverse(bool stuff)
	{
		if (inman == null)
		{
			inman = MonoSingleton<InputManager>.Instance;
		}
		if (stuff)
		{
			PlayerPrefs.SetInt("ScrRev", 1);
			inman.ScrRev = true;
		}
		else
		{
			PlayerPrefs.SetInt("ScrRev", 0);
			inman.ScrRev = false;
		}
	}
}
