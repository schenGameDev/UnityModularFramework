using System.Collections.Generic;
using UnityEngine;

namespace ModularFramework
{
    public class PersistentBehaviour : MonoBehaviour
    {
        public static readonly HashSet<PersistentBehaviour> INSTANCES = new();
        private void Awake()
        {
            INSTANCES.Add(this);
            OnSceneLoad(GameBuilder.Instance? GameBuilder.Instance.NextScene : "");
        }

        private void OnDestroy()
        {
            INSTANCES.Remove(this);
        }

        public virtual void OnSceneLoad(string sceneName)
        {
        }
        
        public virtual void OnSceneDestroy(string sceneName)
        {
        }
    }
}