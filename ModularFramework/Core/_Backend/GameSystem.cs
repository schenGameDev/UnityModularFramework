using System;
using System.Collections.Generic;
using System.Reflection;
using ModularFramework.Commons;
using UnityEngine;

namespace ModularFramework
{
    /// <summary>
    /// Backend system independent of unity lifecycle. <br/>
    /// If not added to GameBuilder, Start(), Destroy() will not invoke.<br/>
    /// Start() called on GameBuilder<br/>
    /// Awake() called on GameRunner<br/>
    /// Destroy() called when game exits
    /// </summary>
    public abstract class GameSystem<T> : GameSystem where T : GameSystem<T>
    {
        public override void Awake()
        {
            base.Awake();
            ((T)this).OnAwake();
        }
        public override void Start()
        {
            base.Start();
            ((T)this).Start();
        }

        public override void Destroy()
        {
            ((T)this).OnDestroy();
            base.Destroy();
        }
        /// <summary>
        /// Called in GameRunner when a new scene loads
        /// </summary>
        protected abstract void OnAwake();
        protected abstract void OnStart();
        protected abstract void OnDestroy();
    }

    public abstract class GameSystem : ScriptableObject
    {
        public virtual void Awake()
        {
            SceneRef.InjectSceneReferences(this);
            SceneFlag.InjectSceneFlags(this);
        }

        public virtual void Start()
        {
            RuntimeObject.InitializeRuntimeVars(this);
        }

        public virtual void Destroy()
        {
            RuntimeObject.CleanRuntimeVars(this);
        }
    }
}