using UnityEngine;
using UnityEngine.Events;

namespace ModularFramework
{
    [RequireComponent(typeof(Canvas))]
    public class CanvasMarker : Marker
    {
        public CanvasMarker()
        {
            RegistryTypes = new[] { new[] {typeof(UISystem)}};
        }
        
        public bool alwaysVisible = false;
        [SerializeField] public bool disableWhenHide = true;
        public bool compatibleWithOtherCanvas = false;

        public void Show()
        {
            gameObject.SetActive(true);
            onVisible?.Invoke();
        }

        public void Hide()
        {
            if(disableWhenHide) gameObject.SetActive(false);
            onHide?.Invoke();
        }
        
        [SerializeField] private UnityEvent onVisible;
        [SerializeField] private UnityEvent onHide;
    }
}