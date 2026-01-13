using ModularFramework.Commons;
using UnityEngine;

namespace ModularFramework.Utility
{
    public static class SingletonRegistry<T> where T : class
    {
        private static T _instance;

        public static bool TryRegister(T instance)
        {
            if (_instance == null)
            {
                _instance = instance;
                return true;
            } 
            
            if (instance is MonoBehaviour mono2)
            {
                Object.Destroy(mono2.gameObject);
                Debug.LogWarning("Destroy duplicated Singleton class " + nameof(T) + " in scene");
            }
            return false;
        }
        
        public static void Clear()
        {
            if (_instance is MonoBehaviour mono)
            {
                Object.Destroy(mono.gameObject);
                Debug.LogWarning("Destroy Singleton class " + nameof(T) + " in scene");
            }
            _instance = null;
        }
        
        public static Optional<T> Get() => _instance;
        
        public static T Instance => _instance;

        public static bool TryGet(out T instance)
        {
            instance = _instance;
            return _instance != null;
        } 
    }
}