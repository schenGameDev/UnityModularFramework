using System;
using System.Collections.Generic;
using EditorAttributes;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;

[RequireComponent(typeof(Marker))]
public abstract class Playable : MonoBehaviour,IMark,ISavable
{
    [Rename("name")]public string playbaleName;
    public bool disableOnAwake = true;
    
    protected Action<string> OnTaskComplete;
    
    public virtual void Play(Action<string> callback = null, string parameter = null) {
        gameObject.SetActive(true);
        OnTaskComplete = callback;
    }

    public virtual void End()
    {
        OnTaskComplete?.Invoke(InkConstants.TASK_PLAY_CG);
    }
    
    #region ISavable
    public string Id => playbaleName;

    public virtual void Load()
    {
        if(gameObject.activeSelf) Play();
    }

    #endregion
    
    #region IRegistrySO
    public List<Type> RegisterSelf(HashSet<Type> alreadyRegisteredTypes)
    {
        if (alreadyRegisteredTypes.Contains(typeof(InkUIIntegrationSO))) return new ();
        SingletonRegistry<InkUIIntegrationSO>.Instance?.Register(transform);
        return new () {typeof(InkUIIntegrationSO)};
    }

    public void UnregisterSelf()
    {
        SingletonRegistry<InkUIIntegrationSO>.Instance?.Unregister(transform);
    }
    #endregion
}
