using ModularFramework.Utility;
using UnityEngine;

namespace ModularFramework
{
    /// <summary>
    /// MonoBehaviour that lives in the Builder.unity Scene. It persists across scene loads and provides type-safe scene load/destroy callbacks
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
        private void Awake()
        {
            Registry<PersistentBehaviour>.TryAdd(this);
            SingletonRegistry<GameBuilder>.Get()
                .Do(builder => LoadScene(builder.NextScene)).
                OrElseDo(() => LoadScene(""));
        }

        private void OnDestroy()
        {
            Registry<PersistentBehaviour>.Remove(this);
        }

        public abstract void LoadScene(string sceneName);
        public abstract void DestroyScene(string sceneName);
    }
}