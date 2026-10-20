using UnityEngine.UI;

[ConfigureSingleton(SingletonFlags.NoAutoInstance)]
public class CutsceneSkipText : MonoSingleton<CutsceneSkipText>
{
	private Text txt;

	private void Start()
	{
		txt = GetComponent<Text>();
		txt.text = "";
	}

	public void Show()
	{
		txt.text = "Press <color=orange>ESC</color> to skip";
	}

	public void Hide()
	{
		txt.text = "";
	}
}
