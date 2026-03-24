using UnityEngine;
using UnityEngine.Events;

namespace ModularFramework
{
    [DisallowMultipleComponent]
    public class ColliderEventListener : MonoBehaviour
    {
        [SerializeField] private UnityEvent<Transform> onCollisionEnter;
        [SerializeField] private UnityEvent<Transform> onCollisionExit;

        private void OnCollisionEnter(Collision other)
        {
            onCollisionEnter?.Invoke(other.transform);
        }

        private void OnCollisionExit(Collision other)
        {
            onCollisionExit?.Invoke(other.transform);
        }
    }
}