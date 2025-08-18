using System;
using EditorAttributes;
using ModularFramework;
using UnityEngine;

[RequireComponent(typeof(Marker))]
public abstract class Playable : MonoBehaviour,IMark,ISavable
{
    [Rename("name")]public string playbaleName;
    public bool disableOnAwake = true;
    public Type[][] RegistryTypes => new[] { new []{typeof(InkUIIntegrationSO)}};
    
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
}
