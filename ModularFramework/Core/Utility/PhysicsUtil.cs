using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

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

		public static async UniTask Move(Transform tf, Vector3 target, float seconds, CancellationToken token) {
			var currentPos = tf.position;
			var t = 0f;
			while(t <= 1f)
			{
				t += Time.deltaTime / seconds;
				tf.position = Vector3.Lerp(currentPos, target, t);
				await UniTask.NextFrame(cancellationToken:token);
			}
			tf.position = target;
		}
		
		public static async UniTask MoveUI(RectTransform tf, Vector2 targetAnchor, float seconds, CancellationToken token) {
			var currentPos = tf.anchoredPosition;
			var t = 0f;
			while(t <= 1f)
			{
				t += Time.deltaTime / seconds;
				tf.anchoredPosition = Vector2.Lerp(currentPos, targetAnchor, t);
				await UniTask.NextFrame(cancellationToken:token);
			}
			tf.anchoredPosition = targetAnchor;
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
	}
}