using UnityEngine;

namespace ModularFramework
{
    public class CanvasMarker : Marker
    {
        public CanvasMarker()
        {
            registryTypes = new[] { (typeof(UISystem),1)};
        }
        
        public bool alwaysVisible = true;
    }
}