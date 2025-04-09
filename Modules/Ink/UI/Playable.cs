using System;
using ModularFramework;
using UnityEngine;

public class Playable  : Marker
{
    public Playable()
    {
        RegistryTypes = new[] { new []{typeof(InkUIIntegrationSO)}};
    }
    
    private Action<string> _onTaskComplete;
    public void Play(Action<string> callback = null) {
        gameObject.SetActive(true);
        _onTaskComplete = callback;
    }

    public void End() {
        gameObject.SetActive(false);
        _onTaskComplete?.Invoke(InkConstants.TASK_PLAY_CG);
    }
}
