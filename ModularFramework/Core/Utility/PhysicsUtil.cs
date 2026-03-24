using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ModularFramework.Utility {
	public static class PhysicsUtil {
		public static Vector3 FindGroundPosition(Vector3 target) {
			Vector3 targetSky = new (target.x,50,target.z);
			Ray ray = new (targetSky, Vector3.down);
			var landPos = target;

			if(Physics.Raycast(ray,out RaycastHit hitInfo, 100, LayerMask.GetMask(EnvironmentConstants.LAYER_GROUND))) {
				landPos = hitInfo.point;
			}
			return landPos;
		}
		
		public static int Choose(float[] choices) {
			int i = 0;
			float total = 0;
			float rand = Random.value;
			foreach(float poss in choices) {
				total+=poss;
				if(rand <= total) {
					return i;
				}
				i++;
			}
			return i;

		}

		public static Mesh CombineMesh(List<MeshFilter> meshes) {
			if(meshes.Count==1) return meshes[0].mesh;
			CombineInstance[] combine = new CombineInstance[meshes.Count];
			int i = 0;
			while (i < meshes.Count)
			{
				combine[i].mesh = meshes[i].sharedMesh;
				combine[i].transform = meshes[i].transform.localToWorldMatrix;
				i++;
			}

			Mesh mesh = new Mesh();
			mesh.CombineMeshes(combine);

			return mesh;
		}

		/// <summary>
		/// Calculates the world-space size of a BoxCollider, accounting for scale.
		/// </summary>
		/// <param name="collider">The BoxCollider to measure.</param>
		/// <returns>The absolute world-space dimensions of the collider as a Vector3.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 GetColliderSize(BoxCollider collider)
		{
			return Vector3.Scale(collider.size, collider.transform.lossyScale)
				.Abs();
		}
		
		/// <summary>
		/// Gets the world-space center position of a BoxCollider.
		/// </summary>
		/// <param name="collider">The BoxCollider to query.</param>
		/// <returns>The world-space position of the collider's center point.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 GetColliderCenter(BoxCollider collider)
		{
			return collider.transform.TransformPoint(collider.center);
		}
		
	}
}