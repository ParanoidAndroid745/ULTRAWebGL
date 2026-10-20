using System.Collections.Generic;
using NewBlood;
using UnityEngine;
using UnityEngine.InputSystem;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class InputManager : MonoSingleton<InputManager>
{
	private class BindingInfo
	{
		public InputAction Action;

		public string PrefName;

		public string Name;

		public int Offset;

		public KeyCode DefaultKey;
	}

	public Dictionary<string, KeyCode> inputsDictionary = new Dictionary<string, KeyCode>();

	public bool ScrOn;

	public bool ScrWep;

	public bool ScrVar;

	public bool ScrRev;

	private BindingInfo[] bindings;

	public PlayerInput InputSource { get; private set; }

	public Dictionary<string, KeyCode> Inputs => inputsDictionary;

	protected override void Awake()
	{
		base.Awake();
		InputSource = new PlayerInput();
		InputSource.Move.Action.AddCompositeBinding("Dpad").With("Up", "<Keyboard>/w", "Keyboard").With("Down", "<Keyboard>/s", "Keyboard")
			.With("Left", "<Keyboard>/a", "Keyboard")
			.With("Right", "<Keyboard>/d", "Keyboard");
		InputSource.Look.Action.AddBinding("<Mouse>/delta").WithProcessor("scaleVector2(x=.05,y=.05)");
		InputSource.WheelLook.Action.AddBinding("<Mouse>/delta").WithProcessor("scaleVector2(x=.05,y=.05)");
		InputSource.Jump.Action.AddBinding("<Keyboard>/space").WithGroup("Keyboard");
		InputSource.Dodge.Action.AddBinding("<Keyboard>/leftShift").WithGroup("Keyboard");
		InputSource.Slide.Action.AddBinding("<Keyboard>/leftCtrl").WithGroup("Keyboard");
		InputSource.Fire1.Action.AddBinding("<Mouse>/leftButton").WithGroup("Keyboard");
		InputSource.Fire2.Action.AddBinding("<Mouse>/rightButton").WithGroup("Keyboard");
		InputSource.Punch.Action.AddBinding("<Keyboard>/f").WithGroup("Keyboard");
		InputSource.LastWeapon.Action.AddBinding("<Keyboard>/q").WithGroup("Keyboard");
		InputSource.ChangeVariation.Action.AddBinding("<Keyboard>/e").WithGroup("Keyboard");
		InputSource.ChangeFist.Action.AddBinding("<Keyboard>/g").WithGroup("Keyboard");
		InputSource.Pause.Action.AddBinding("<Keyboard>/escape").WithGroup("Keyboard");
		InputSource.Stats.Action.AddBinding("<Keyboard>/tab").WithGroup("Keyboard");
		InputSource.Slot0.Action.AddBinding("<Keyboard>/0").WithGroup("Keyboard");
		InputSource.Slot1.Action.AddBinding("<Keyboard>/1").WithGroup("Keyboard");
		InputSource.Slot2.Action.AddBinding("<Keyboard>/2").WithGroup("Keyboard");
		InputSource.Slot3.Action.AddBinding("<Keyboard>/3").WithGroup("Keyboard");
		InputSource.Slot4.Action.AddBinding("<Keyboard>/4").WithGroup("Keyboard");
		InputSource.Slot5.Action.AddBinding("<Keyboard>/5").WithGroup("Keyboard");
		InputSource.Slot6.Action.AddBinding("<Keyboard>/6").WithGroup("Keyboard");
		InputSource.Slot7.Action.AddBinding("<Keyboard>/7").WithGroup("Keyboard");
		InputSource.Slot8.Action.AddBinding("<Keyboard>/8").WithGroup("Keyboard");
		InputSource.Slot9.Action.AddBinding("<Keyboard>/9").WithGroup("Keyboard");
		InputSource.Enable();
		bindings = new BindingInfo[23]
		{
			new BindingInfo
			{
				Action = InputSource.Move.Action,
				PrefName = "KeyW",
				DefaultKey = KeyCode.W,
				Name = "W"
			},
			new BindingInfo
			{
				Action = InputSource.Move.Action,
				PrefName = "KeyS",
				Offset = 1,
				DefaultKey = KeyCode.S,
				Name = "S"
			},
			new BindingInfo
			{
				Action = InputSource.Move.Action,
				PrefName = "KeyA",
				Offset = 2,
				DefaultKey = KeyCode.A,
				Name = "A"
			},
			new BindingInfo
			{
				Action = InputSource.Move.Action,
				PrefName = "KeyD",
				Offset = 3,
				DefaultKey = KeyCode.D,
				Name = "D"
			},
			new BindingInfo
			{
				Action = InputSource.Jump.Action,
				PrefName = "KeyJump",
				DefaultKey = KeyCode.Space,
				Name = "Jump"
			},
			new BindingInfo
			{
				Action = InputSource.Dodge.Action,
				PrefName = "KeyDodge",
				DefaultKey = KeyCode.LeftShift,
				Name = "Dodge"
			},
			new BindingInfo
			{
				Action = InputSource.Slide.Action,
				PrefName = "KeySlide",
				DefaultKey = KeyCode.LeftControl,
				Name = "Slide"
			},
			new BindingInfo
			{
				Action = InputSource.Fire1.Action,
				PrefName = "KeyFire1",
				DefaultKey = KeyCode.Mouse0,
				Name = "Fire1"
			},
			new BindingInfo
			{
				Action = InputSource.Fire2.Action,
				PrefName = "KeyFire2",
				DefaultKey = KeyCode.Mouse1,
				Name = "Fire2"
			},
			new BindingInfo
			{
				Action = InputSource.Punch.Action,
				PrefName = "KeyPunch",
				DefaultKey = KeyCode.F,
				Name = "Punch"
			},
			new BindingInfo
			{
				Action = InputSource.LastWeapon.Action,
				PrefName = "KeyLastUsedWeapon",
				DefaultKey = KeyCode.Q,
				Name = "LastUsedWeapon"
			},
			new BindingInfo
			{
				Action = InputSource.ChangeVariation.Action,
				PrefName = "KeyChangeVariation",
				DefaultKey = KeyCode.E,
				Name = "ChangeVariation"
			},
			new BindingInfo
			{
				Action = InputSource.ChangeFist.Action,
				PrefName = "KeyChangeFist",
				DefaultKey = KeyCode.G,
				Name = "ChangeFist"
			},
			new BindingInfo
			{
				Action = InputSource.Slot0.Action,
				PrefName = "KeySlot0",
				DefaultKey = KeyCode.Alpha0,
				Name = "Slot0"
			},
			new BindingInfo
			{
				Action = InputSource.Slot1.Action,
				PrefName = "KeySlot1",
				DefaultKey = KeyCode.Alpha1,
				Name = "Slot1"
			},
			new BindingInfo
			{
				Action = InputSource.Slot2.Action,
				PrefName = "KeySlot2",
				DefaultKey = KeyCode.Alpha2,
				Name = "Slot2"
			},
			new BindingInfo
			{
				Action = InputSource.Slot3.Action,
				PrefName = "KeySlot3",
				DefaultKey = KeyCode.Alpha3,
				Name = "Slot3"
			},
			new BindingInfo
			{
				Action = InputSource.Slot4.Action,
				PrefName = "KeySlot4",
				DefaultKey = KeyCode.Alpha4,
				Name = "Slot4"
			},
			new BindingInfo
			{
				Action = InputSource.Slot5.Action,
				PrefName = "KeySlot5",
				DefaultKey = KeyCode.Alpha5,
				Name = "Slot5"
			},
			new BindingInfo
			{
				Action = InputSource.Slot6.Action,
				PrefName = "KeySlot6",
				DefaultKey = KeyCode.Alpha6,
				Name = "Slot6"
			},
			new BindingInfo
			{
				Action = InputSource.Slot7.Action,
				PrefName = "KeySlot7",
				DefaultKey = KeyCode.Alpha7,
				Name = "Slot7"
			},
			new BindingInfo
			{
				Action = InputSource.Slot8.Action,
				PrefName = "KeySlot8",
				DefaultKey = KeyCode.Alpha8,
				Name = "Slot8"
			},
			new BindingInfo
			{
				Action = InputSource.Slot9.Action,
				PrefName = "KeySlot9",
				DefaultKey = KeyCode.Alpha9,
				Name = "Slot9"
			}
		};
		if (PlayerPrefs.GetInt("ScrOn", 1) == 1)
		{
			ScrOn = true;
		}
		else
		{
			ScrOn = false;
		}
		if (PlayerPrefs.GetInt("ScrWep", 1) == 1)
		{
			ScrWep = true;
		}
		else
		{
			ScrWep = false;
		}
		if (PlayerPrefs.GetInt("ScrVar", 0) == 1)
		{
			ScrVar = true;
		}
		else
		{
			ScrVar = false;
		}
		if (PlayerPrefs.GetInt("ScrRev", 0) == 1)
		{
			ScrRev = true;
		}
		else
		{
			ScrRev = false;
		}
		UpdateBindings();
	}

	public void UpdateBindings()
	{
		BindingInfo[] array = bindings;
		foreach (BindingInfo bindingInfo in array)
		{
			InputBinding bindingMask = InputBinding.MaskByGroup("Keyboard");
			int bindingIndex = bindingInfo.Action.GetBindingIndex(bindingMask);
			Inputs[bindingInfo.Name] = (KeyCode)PlayerPrefs.GetInt(bindingInfo.PrefName, (int)bindingInfo.DefaultKey);
			if (bindingIndex != -1 && PlayerPrefs.HasKey(bindingInfo.PrefName) && LegacyInput.current.TryGetButton((KeyCode)PlayerPrefs.GetInt(bindingInfo.PrefName), out var button))
			{
				bindingInfo.Action.ChangeBinding(bindingIndex + bindingInfo.Offset).WithPath(button.path);
			}
		}
	}
}
