using UnityEngine;
using ModularFramework.Utility;

namespace ModularFramework {
    public class PersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual void Awake() {
            if(Instance == null) {
                Instance = this as T;
                DontDestroyOnLoad(gameObject);
            }else {
                DebugUtil.Error("Duplicated Singleton class " + nameof(T) + " in scene");
                Destroy(this.gameObject);
            }
        }
    }
}
