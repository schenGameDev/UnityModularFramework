using System;
using System.Collections.Generic;
using ModularFramework;
using UnityEngine;
using UnityTimer;

[RequireComponent(typeof(Marker))]
public class ChatBubbleQueue : TextPrinterBase,IMark
{
    public Type[][] RegistryTypes => new[] { new []{typeof(InkUIIntegrationSO)}};
    
    [SerializeField] private int maxBubbles = 5;
    [SerializeField] private float gap = 0.5f;
    [SerializeField] private TextPrinter[] bubblePrefabs;

    private readonly List<TextPrinter> _bubbles = new();
    private TextPrinter LatestBubble => _bubbles.IsEmpty() ? null : _bubbles[^1];
    private Action _callback;
    private Timer _timer;

    protected void Awake()
    {
        if (gap > 0)
        {
            _timer = new CountdownTimer(gap);
            _timer.OnTimerStop+= () =>Done = true;
        }
        
    }

    private TextPrinter CreateChatBubble(TextPrinter prefab)
    {
        return Instantiate(prefab, transform);
    }
    
    private void OnBubbleComplete()
    {
        if (_callback == null || _timer == null)
        {
            Done = true;
            return;
        }
        
        _timer.OnTimerStop+=_callback;
        _timer.Start();
    }


    public override void Print(string text, Action callback, params string[] parameters)
    {
        if (_timer != null && _callback!=null)
        {
            _timer.OnTimerStop-=_callback;
        }
        Done = false;
        int index = parameters.Length == 0 || parameters[0].IsEmpty() ? 0 : int.Parse(parameters[0]);
        TextPrinter printer = CreateChatBubble(bubblePrefabs[index]);
        printer.ReturnEarly = ReturnEarly;
        printer.Print(text, OnBubbleComplete);
        _callback = callback;
        _bubbles.Add(printer);
        if (_bubbles.Count > maxBubbles)
        {
            Destroy(_bubbles.RemoveAtAndReturn(0));
        }
    }

    public override void Skip()
    {
        if (LatestBubble != null && !LatestBubble.Done)
        {
            LatestBubble.Skip();
        }
        else
        {
            _timer?.Stop();
        }
        
    }

    public override void Clean()
    {
        if (_timer != null && _callback!=null)
        {
            _timer.OnTimerStop-=_callback;
        }
        foreach (var b in _bubbles)
        {
            Destroy(b);
        }
        ReturnEarly = false;
        Done = false;
    }

    private void OnDestroy()
    {
        if (_timer != null && _callback!=null)
        {
            _timer.OnTimerStop-=_callback;
        }
    }
}
