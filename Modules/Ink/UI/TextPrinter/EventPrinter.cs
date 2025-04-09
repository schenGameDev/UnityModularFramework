using ModularFramework;
using UnityEngine;

public class EventPrinter : TextPrinter
{
    [SerializeField] private EventChannel<string> eventChannel;
    
    private void OnEnable()
    {
        eventChannel?.AddListener(Print);
    }

    private void OnDisable()
    {
        eventChannel?.RemoveListener(Print);
    }
}