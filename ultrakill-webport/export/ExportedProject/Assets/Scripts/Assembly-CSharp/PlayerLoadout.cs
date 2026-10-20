using UnityEngine;

public class PlayerLoadout : MonoBehaviour, IPlaceholdableComponent
{
	public bool forceLoadout;

	public ForcedLoadout loadout;

	public void WillReplace(GameObject oldObject, GameObject newObject, bool isSelfBeingReplaced)
	{
		if (isSelfBeingReplaced && forceLoadout)
		{
			PlayerLoadoutTarget component = newObject.GetComponent<PlayerLoadoutTarget>();
			if ((bool)component)
			{
				component.CommitLoadout(loadout);
			}
		}
	}
}
