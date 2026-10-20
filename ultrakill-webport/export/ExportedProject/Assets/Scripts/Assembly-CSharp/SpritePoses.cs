using UnityEngine;
using UnityEngine.UI;

public class SpritePoses : MonoBehaviour
{
	public Sprite[] poses;

	private Image img;

	public void ChangePose(int target)
	{
		if (!img)
		{
			img = GetComponent<Image>();
		}
		if (target < poses.Length)
		{
			img.sprite = poses[target];
		}
		else if (poses.Length != 0)
		{
			img.sprite = poses[0];
		}
	}
}
