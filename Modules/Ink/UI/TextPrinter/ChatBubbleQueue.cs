using System;
using System.Collections.Generic;
using System.Linq;
using ModularFramework;
using UnityEngine;
using UnityTimer;

[RequireComponent(typeof(Marker))]
public class ChatBubbleQueue : TextPrinterBase,IMark,ISavable
{
    public Type[][] RegistryTypes => new[] { new []{typeof(InkUIIntegrationSO)}};
    
    [SerializeField] private int maxBubbles = 5;
    [SerializeField] private TextPrinter[] bubblePrefabs;

    private readonly List<TextPrinter> _bubbles = new();
    private TextPrinter LatestBubble => _bubbles.IsEmpty() ? null : _bubbles[^1];
    private Action _callback;
    [SavableState] private string _historyStr = "";
    private (int,string) _current;
    private List<string> _lines = new();
    

    private TextPrinter CreateChatBubble(TextPrinter prefab)
    {
        return Instantiate(prefab, transform);
    }
    
    private void OnBubbleComplete()
    {
        Done = true;
        _lines.Add(_current.Item1 + "&&" + _current.Item2);
        if (_lines.Count > maxBubbles)
        {
            _lines.RemoveAt(0);
        }
        _historyStr = string.Join("||", _lines);
        
        _callback?.Invoke();
    }


    public override void Print(string text, Action callback, params string[] parameters)
    {
        Done = false;
        int index = parameters.Length == 0 || parameters[0].IsEmpty() ? 0 : int.Parse(parameters[0]);
        TextPrinter printer = CreateChatBubble(bubblePrefabs[index]);
        printer.ReturnEarly = ReturnEarly;
        printer.Print(text, OnBubbleComplete);
        _callback = callback;
        _bubbles.Add(printer);
        _current = (index, text);
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
    }

    public override void Clean()
    {
        foreach (var b in _bubbles)
        {
            Destroy(b);
        }
        
        _historyStr = "";
        _lines.Clear();
        
        ReturnEarly = false;
        Done = false;
    }

        
    #region ISavable
    public string Id => printerName;

    public virtual void Load()
    {
        if (gameObject.activeSelf)
        {
            _lines = _historyStr.Split("||").ToList();
            _lines.ForEach(line =>
            {
                var arr = line.Split("&&");
                TextPrinter printer = CreateChatBubble(bubblePrefabs[int.Parse(arr[0])]);
                printer.Textbox.text = arr[1];
                _bubbles.Add(printer);
            });
        }
    }

    #endregion
}
