using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScrollingTexture : MonoBehaviour
{
	private static MaterialPropertyBlock _propertyBlock;

	public float scrollSpeedX;

	public float scrollSpeedY;

	private Dictionary<int, int[]> texturePropertyStNames;

	private Material[] sharedMaterials;

	private MeshRenderer mr;

	private Vector2 offset;

	private void Start()
	{
		if (_propertyBlock == null)
		{
			_propertyBlock = new MaterialPropertyBlock();
		}
		mr = GetComponent<MeshRenderer>();
		texturePropertyStNames = new Dictionary<int, int[]>();
		sharedMaterials = mr.sharedMaterials;
		for (int i = 0; i < sharedMaterials.Length; i++)
		{
			Material material = sharedMaterials[i];
			List<string> list = material.GetTexturePropertyNames().ToList();
			for (int j = 0; j < list.Count; j++)
			{
				list[j] += "_ST";
			}
			for (int num = list.Count - 1; num >= 0; num--)
			{
				if (!material.HasProperty(list[num]))
				{
					list.RemoveAt(num);
				}
			}
			int[] array = new int[list.Count];
			for (int k = 0; k < array.Length; k++)
			{
				array[k] = Shader.PropertyToID(list[k]);
			}
			texturePropertyStNames[i] = array;
		}
	}

	private void Update()
	{
		offset += new Vector2(scrollSpeedX * Time.deltaTime, scrollSpeedY * Time.deltaTime);
		for (int i = 0; i < sharedMaterials.Length; i++)
		{
			mr.GetPropertyBlock(_propertyBlock, i);
			int[] array = texturePropertyStNames[i];
			Material material = sharedMaterials[i];
			for (int j = 0; j < array.Length; j++)
			{
				Vector4 vector = material.GetVector(array[j]);
				vector.z = offset.x;
				vector.w = offset.y;
				_propertyBlock.SetVector(array[j], vector);
			}
			mr.SetPropertyBlock(_propertyBlock, i);
		}
	}
}
