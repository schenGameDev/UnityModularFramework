using KBCore.Refs;
using UnityEngine;
using UnityEngine.Events;

namespace ModularFramework
{
    /// <summary>
    /// Attach to Canvas GameObject, keep the gameObject active but enable/disable the Canvas component to show/hide the UI, so it won't redraw the whole canvas mesh and cause performance spike.
    /// </summary>
    [RequireComponent(typeof(Canvas)),DisallowMultipleComponent]
    public class CanvasMarker : MonoBehaviour, IUniqueIdentifiable
    {
        public bool alwaysVisible = false;
        [SerializeField] public bool disableWhenHide = true;
        public bool compatibleWithOtherCanvas = false;
        [SerializeField] private UnityEvent onVisible;
        [SerializeField] private UnityEvent onHide;
        
        [Self,SerializeField] private Canvas canvas;

        public void Show()
        {
            canvas.enabled = true;
            onVisible?.Invoke();
        }

        public void Hide()
        {
            if(disableWhenHide) canvas.enabled = false;
            onHide?.Invoke();
        }

        public string UniqueId  => transform.name;

        private void Awake()
        {
            if (!DictRegistry<string, CanvasMarker>.TryAdd(UniqueId, this))
            {
                Debug.LogWarning($"Canvas Marker {UniqueId} is already registered");
            }
            canvas.enabled = false;
        }

        private void OnDisable()
        {
            DictRegistry<string,CanvasMarker>.Remove(UniqueId);
        }

        private void OnDestroy()
        {
            DictRegistry<string,CanvasMarker>.Remove(UniqueId);
        }

#if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
#endif
    }
}