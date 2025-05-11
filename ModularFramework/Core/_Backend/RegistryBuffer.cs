using System.Collections.Generic;

namespace ModularFramework
{
    public static class RegistryBuffer
    {
        private static readonly List<Marker> MARKERS = new List<Marker>();
        
        public static void Register(Marker marker)
        {
            MARKERS.Add(marker);
            marker.gameObject.SetActive(false);
        }

        public static void Unregister(Marker marker)
        {
            MARKERS.Remove(marker);
        }

        public static void InjectAll()
        {
            MARKERS.ForEach(marker =>
            {
                //if (!marker) return;
                marker.RegisterAll();
            });
        }
    }
}