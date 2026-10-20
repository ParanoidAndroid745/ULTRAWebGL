using UnityEngine;

public class ItemPlaceZone : MonoBehaviour
{
	public ItemType acceptedItemType;

	public GameObject[] activateOnSuccess;

	public GameObject[] deactivateOnSuccess;

	public GameObject[] activateOnFailure;

	public Door[] doors;

	public Door[] reverseDoors;

	public ArenaStatus[] arenaStatuses;

	public ArenaStatus[] reverseArenaStatuses;

	private Collider col;

	private void Start()
	{
		col = GetComponent<Collider>();
		CheckItem(prelim: true);
		ColorDoors(doors);
		ColorDoors(reverseDoors);
	}

	private void ColorDoors(Door[] drs)
	{
		foreach (Door door in drs)
		{
			switch (acceptedItemType)
			{
			case ItemType.SkullBlue:
				door.defaultLightsColor = new Color(0f, 0.75f, 1f);
				break;
			case ItemType.SkullRed:
				door.defaultLightsColor = new Color(1f, 0.2f, 0.2f);
				break;
			case ItemType.SkullGreen:
				door.defaultLightsColor = new Color(0.25f, 1f, 0.25f);
				break;
			}
			if (!door.noPass || !door.noPass.activeSelf)
			{
				door.ChangeColor(door.defaultLightsColor);
			}
			door.AltarControlled();
		}
	}

	public void CheckItem(bool prelim = false)
	{
		ItemIdentifier componentInChildren = GetComponentInChildren<ItemIdentifier>();
		GameObject[] array;
		Door[] array2;
		if (componentInChildren != null)
		{
			if (componentInChildren.itemType == acceptedItemType)
			{
				array = activateOnSuccess;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].SetActive(value: true);
				}
				array = deactivateOnSuccess;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].SetActive(value: false);
				}
				array2 = doors;
				for (int i = 0; i < array2.Length; i++)
				{
					array2[i].Open(enemy: false, skull: true);
				}
				array2 = reverseDoors;
				for (int i = 0; i < array2.Length; i++)
				{
					array2[i].Close();
				}
				if (!prelim)
				{
					ArenaStatus[] array3 = arenaStatuses;
					for (int i = 0; i < array3.Length; i++)
					{
						array3[i].currentStatus++;
					}
					array3 = reverseArenaStatuses;
					for (int i = 0; i < array3.Length; i++)
					{
						array3[i].currentStatus--;
					}
				}
			}
			else
			{
				array = activateOnFailure;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].SetActive(value: true);
				}
			}
			if ((bool)col)
			{
				col.enabled = false;
			}
			return;
		}
		array = activateOnSuccess;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		array = activateOnFailure;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		array = deactivateOnSuccess;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: true);
		}
		array2 = doors;
		foreach (Door door in array2)
		{
			if (door.doorType != DoorType.Normal || door.transform.localPosition != door.closedPos)
			{
				door.Close();
			}
		}
		array2 = reverseDoors;
		foreach (Door door2 in array2)
		{
			if (door2.doorType != DoorType.Normal || door2.transform.localPosition != door2.closedPos + door2.openPos)
			{
				door2.Open(enemy: false, skull: true);
			}
		}
		if (!prelim)
		{
			ArenaStatus[] array3 = arenaStatuses;
			for (int i = 0; i < array3.Length; i++)
			{
				array3[i].currentStatus--;
			}
			array3 = reverseArenaStatuses;
			for (int i = 0; i < array3.Length; i++)
			{
				array3[i].currentStatus++;
			}
		}
		if ((bool)col)
		{
			col.enabled = true;
		}
	}
}
