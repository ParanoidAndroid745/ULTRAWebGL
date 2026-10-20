using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "ULTRAKILL/Spawnable Object")]
public class SpawnableObject : ScriptableObject
{
	public string objectName;

	[TextArea]
	public string description;

	public GameObject gameObject;

	public GameObject preview;

	public Sprite gridIcon;

	public Color backgroundColor;

	public EnemyType enemyType;

	[Header("Spawn Arm")]
	public Vector3 armOffset = Vector3.zero;

	[FormerlySerializedAs("rotationOffset")]
	public Vector3 armRotationOffset = Vector3.zero;

	[Header("Menu Preview")]
	public Vector3 menuOffset = Vector3.zero;
}
