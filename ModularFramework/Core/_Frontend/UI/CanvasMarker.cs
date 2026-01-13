using ModularFramework.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace ModularFramework
{
    [RequireComponent(typeof(Canvas)),DisallowMultipleComponent]
    public class CanvasMarker : Marker
    {
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

        public override void RegisterAll()
        {
            SingletonRegistry<UISystem>.Instance?.Register(transform);
        }

        protected override void UnregisterAll()
        {
            SingletonRegistry<UISystem>.Instance?.Unregister(transform);
        }
    }
}