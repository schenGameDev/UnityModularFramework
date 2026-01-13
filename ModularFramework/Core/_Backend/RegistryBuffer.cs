using ModularFramework.Utility;

namespace ModularFramework
{
    public static class RegistryBuffer
    {
        public static void Register(Marker marker)
        {
            Registry<Marker>.TryAdd(marker);
            marker.gameObject.SetActive(false);
        }

        public static void Unregister(Marker marker)
        {
            Registry<Marker>.Remove(marker);
        }

        public static void InjectAll()
        {
            Registry<Marker>.All.ForEach(marker =>
            {
                //if (!marker) return;
                marker.RegisterAll();
            });
        }
    }
}