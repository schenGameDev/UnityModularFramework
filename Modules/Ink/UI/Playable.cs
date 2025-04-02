using System;
using ModularFramework;
using UnityEngine;

public class Playable  : Marker, ILive
{
    [field: SerializeField] public bool Live { get; set; }
    
    public Playable()
    {
        registryTypes = new[] { (typeof(InkUIIntegrationSO),1)};
    }
    
    private Action<string> _onTaskComplete;
    public void Play(Action<string> callback = null) {
        Live = true;
        gameObject.SetActive(true);
        _onTaskComplete = callback;
    }

    public void End() {
        Live = false;
        gameObject.SetActive(false);
        _onTaskComplete?.Invoke(InkConstants.TASK_PLAY_CG);
    }
}
