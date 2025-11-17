using System.Collections.Generic;
using UnityEngine;

namespace ModularFramework
{
    public abstract class PersistentBehaviour<T> : PersistentBehaviour where T : PersistentBehaviour<T>
    {
        public override void LoadScene(string sceneName)
        {
            ((T)this).OnSceneLoad(sceneName);
        }

        public override void DestroyScene(string sceneName)
        {
            ((T)this).OnSceneDestroy(sceneName);
        }

        protected abstract void OnSceneLoad(string sceneName);
        protected abstract void OnSceneDestroy(string sceneName);
    }
    
    public abstract class PersistentBehaviour : MonoBehaviour
    {
        public static readonly HashSet<PersistentBehaviour> INSTANCES = new();
        private void Awake()
        {
            INSTANCES.Add(this);
            LoadScene(GameBuilder.Instance? GameBuilder.Instance.NextScene : "");
        }

        private void OnDestroy()
        {
            INSTANCES.Remove(this);
        }

        public abstract void LoadScene(string sceneName);
        public abstract void DestroyScene(string sceneName);
    }
}