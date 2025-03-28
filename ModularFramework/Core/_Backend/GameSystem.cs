using UnityEngine;

namespace ModularFramework
{
    /// <summary>
    /// Backend system independent of unity lifecycle. <br/>
    /// If used on its own, OnStart(), OnDestroy() will not invoke.<br/>
    /// OnStart() called on GameBuilder
    /// OnDestroy() called when game exits
    /// </summary>
    public abstract class GameSystem :ScriptableObject
    {

        public virtual void OnStart()
        {
            RuntimeObject.InitializeRuntimeVars(this);
        }

        public virtual void OnDestroy()
        {
            RuntimeObject.CleanRuntimeVars(this);
        }
        
        

    }
}