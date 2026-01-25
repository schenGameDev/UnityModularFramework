using System;
using ModularFramework.Utility;
using UnityEngine;

namespace ModularFramework {
    [Obsolete("Use ModularFramework.Utility.SingletonRegistry<T> instead")]
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual void Awake() {
            if(Instance == null) Instance = this as T;
            else {
                DebugUtil.Error("Duplicated Singleton class " + Instance.GetType() + " in scene");
                Destroy(this.gameObject);
            }
        }
    }
}
