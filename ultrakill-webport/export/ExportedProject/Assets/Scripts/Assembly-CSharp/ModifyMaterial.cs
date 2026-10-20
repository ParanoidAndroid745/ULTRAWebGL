using UnityEngine;

public class ModifyMaterial : MonoBehaviour
{
	private Renderer rend;

	private MaterialPropertyBlock block;

	private void Start()
	{
		block = new MaterialPropertyBlock();
		rend = GetComponent<Renderer>();
	}

	public void ChangeEmissionIntensity(float value)
	{
		for (int i = 0; i < rend.materials.Length; i++)
		{
			rend.GetPropertyBlock(block, i);
			block.SetFloat(UKShaderProperties.EmissiveIntensity, value);
			rend.SetPropertyBlock(block, i);
		}
	}

	public void ChangeEmissionColor(Color clr)
	{
		for (int i = 0; i < rend.materials.Length; i++)
		{
			rend.GetPropertyBlock(block, i);
			block.SetColor(UKShaderProperties.EmissiveColor, clr);
			rend.SetPropertyBlock(block, i);
		}
	}
}
