using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ModularFramework;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextPrinter : Marker
{
    [SerializeField] private float timeGapBetweenLetters = 0.05f;
    [SerializeField] private int maxTextLength = 300;
    [SerializeField] private EventChannel<string> eventChannel;
        
    private TextMeshProUGUI _textbox;

    public TextPrinter()
    {
        registryTypes = new[] { (typeof(InkUIIntegration),1)};
    }
    
    private void Awake()
    {
        _textbox = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        eventChannel?.AddListener(Print);
    }

    private void OnDisable()
    {
        eventChannel?.RemoveListener(Print);
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public void Skip()
    {
        _cts?.Cancel();
    }

    private CancellationTokenSource _cts;
        
    public void Print(string text)
    {
        _textbox.text = string.Empty;
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        _cts = new CancellationTokenSource();
        PrintTask(text, timeGapBetweenLetters, maxTextLength, _cts.Token).Forget();
    }

    async UniTaskVoid PrintTask(string text, float timeGap, int maxLen, CancellationToken token)
    {
        for (int i = 0; i < text.Length; i += maxLen)
        {
            if (i > 0)
            {
                _textbox.text = string.Empty;
            }
            var page = text.Substring(i, maxLen);
            bool isCanceled = false;
            foreach (var ch in page)
            {
                isCanceled = await UniTask.WaitForSeconds(timeGap, cancellationToken:token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    _textbox.text = text;
                    break;
                }
                _textbox.text += ch;
            }
        }
    }
}