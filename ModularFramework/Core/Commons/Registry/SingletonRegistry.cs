using ModularFramework.Commons;
using UnityEngine;

namespace ModularFramework
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
                Debug.LogWarning("Destroy duplicated Singleton class " + _instance.GetType() + " in scene");
            }
            return false;
        }
        
        public static void Clear()
        {
            if (_instance is MonoBehaviour mono)
            {
                Debug.LogWarning("Destroy Singleton class " + _instance.GetType() + " in scene");
                Object.Destroy(mono.gameObject);
            }
            _instance = null;
        }
        
        public static Optional<T> Get() => _instance;
        
        /// <summary>
        /// Not guaranteed to be non-null
        /// </summary>
        public static T Instance => _instance;

        public static bool TryGet(out T instance)
        {
            instance = _instance;
            return _instance != null && (_instance is not MonoBehaviour mono2 || mono2);
        }
        
        public static T GetOrRegister(T instance)
        {
            if (_instance == null) _instance = instance;
            return _instance;
        }
        
        public static void Replace(T instance)
        {
            _instance = instance;
        }
    }
}