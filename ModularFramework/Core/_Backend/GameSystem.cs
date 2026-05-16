using UnityEngine;

namespace ModularFramework
{
    /// <summary>
    /// Backend system independent of unity lifecycle. <br/>
    /// If not added to GameBuilder, Start(), Destroy() will not invoke.<br/>
    /// Start() called on GameBuilder<br/>
    /// SceneAwake() called on GameRunner<br/>
    /// Destroy() called when game exits
    /// </summary>
    public abstract class GameSystem<T> : GameSystem where T : GameSystem<T>
    {
        public override void SceneAwake()
        {
            base.SceneAwake();
            ((T)this).OnAwake();
        }
        
        public override void Start()
        {
            base.Start();
            ((T)this).OnStart();
        }

        public override void Destroy()
        {
            ((T)this).OnSceneDestroy();
            base.Destroy();
        }
        
        public override void InjectRegistry()
        {
            SingletonRegistry<T>.Replace((T)this);
            Registry<GameSystem>.TryAdd(this);
        }

        public override void ClearRegistry()
        {
            SingletonRegistry<T>.Clear();
        }
        
        /// <summary>
        /// Called in GameRunner when a new scene loads
        /// </summary>
        protected abstract void OnAwake();
        /// <summary>
        /// Called in GameBuilder once game starts
        /// </summary>
        protected abstract void OnStart();
        protected abstract void OnSceneDestroy();
    }

    public abstract class GameSystem : ScriptableObject
    {
        /// <summary>
        /// Called on GameRunner at scene start
        /// </summary>
        public virtual void SceneAwake()
        {
            SceneRef.InjectSceneReferences(this);
            SceneFlag.InjectSceneFlags(this);
        }
        /// <summary>
        /// Called on GameBuilder once at game start
        /// </summary>
        public virtual void Start()
        {
            RuntimeObject.InitializeRuntimeVars(this);
        }
        /// <summary>
        /// Called when game exits
        /// </summary>
        public virtual void Destroy()
        {
            RuntimeObject.CleanRuntimeVars(this);
        }
        
        public abstract void InjectRegistry();

        public abstract void ClearRegistry();
    }
}