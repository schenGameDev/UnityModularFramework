using EditorAttributes;
using UnityEngine;

namespace ModularFramework
{
    public class ShowBounds : MonoBehaviour
    {
        [Button]
        private void RecenterToLocalOrigin()
        {
            Renderer rendererComponent = GetComponent<Renderer>();
            if (rendererComponent == null)
            {
                Debug.LogWarning("No renderer component attached");
            }
            else if (!transform.parent)
            {
                Debug.LogWarning("transform doesn't have a parent transform");
            }
            else
            {
                // Move the object so that its bounds are centered at the local origin
                Vector3 offset = rendererComponent.bounds.center - transform.parent.transform.position;
                transform.localPosition -= offset;
            }
        }
        
        [Button]
        private void PrintBoundsExtents()
        {
            Renderer rendererComponent = GetComponent<Renderer>();
            if (rendererComponent != null)
            {
                Debug.Log(rendererComponent.bounds.extents);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            var rendererComponents = GetComponentsInChildren<Renderer>();
            if (rendererComponents.Length == 0) return;
            foreach (var rendererComponent in rendererComponents)
            {
                // Draw a wireframe cube around the bounds of the selected object in the Scene view
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(rendererComponent.bounds.center, rendererComponent.bounds.size);
            }
        }
    }
}