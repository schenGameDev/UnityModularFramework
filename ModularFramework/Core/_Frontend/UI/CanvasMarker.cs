using UnityEngine;

namespace ModularFramework
{
    [RequireComponent(typeof(Canvas))]
    public class CanvasMarker : Marker
    {
        public CanvasMarker()
        {
            RegistryTypes = new[] { new[] {typeof(UISystem)}};
        }
        
        public bool alwaysVisible = true;
    }
}