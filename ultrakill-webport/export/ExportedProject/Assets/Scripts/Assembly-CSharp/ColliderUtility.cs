using System.Collections.Generic;
using UnityEngine;

public static class ColliderUtility
{
	private static readonly List<Vector3> s_Vertices = new List<Vector3>();

	private static readonly List<int> s_Triangles = new List<int>();

	private static Plane GetWorldTriangle(Transform collider, int index)
	{
		Vector3 position = s_Vertices[s_Triangles[3 * index]];
		Vector3 position2 = s_Vertices[s_Triangles[3 * index + 1]];
		Vector3 position3 = s_Vertices[s_Triangles[3 * index + 2]];
		return new Plane(collider.TransformPoint(position), collider.TransformPoint(position2), collider.TransformPoint(position3));
	}

	public static Vector3 FindClosestPoint(Collider collider, Vector3 position)
	{
		return FindClosestPoint(collider, position, ignoreVerticalTriangles: false);
	}

	public static Vector3 FindClosestPoint(Collider collider, Vector3 position, bool ignoreVerticalTriangles)
	{
		if (collider is MeshCollider meshCollider && !meshCollider.convex)
		{
			Mesh sharedMesh = meshCollider.sharedMesh;
			sharedMesh.GetVertices(s_Vertices);
			Plane plane = default;
			float num = float.PositiveInfinity;
			for (int i = 0; i < sharedMesh.subMeshCount; i++)
			{
				sharedMesh.GetTriangles(s_Triangles, i);
				int j = 0;
				for (int num2 = s_Triangles.Count / 3; j < num2; j++)
				{
					Plane worldTriangle = GetWorldTriangle(meshCollider.transform, j);
					float num3 = Mathf.Abs(worldTriangle.GetDistanceToPoint(position));
					if ((!ignoreVerticalTriangles || (!(worldTriangle.normal == Vector3.up) && !(worldTriangle.normal == Vector3.down))) && ((i == 0 && j == 0) || num3 < num))
					{
						plane = worldTriangle;
						num = num3;
					}
				}
			}
			return plane.ClosestPointOnPlane(position);
		}
		return collider.ClosestPoint(position);
	}
}
