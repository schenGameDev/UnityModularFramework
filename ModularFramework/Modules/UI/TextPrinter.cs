using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ModularFramework;
using TMPro;
using UnityEngine;

public class TextPrinter : MonoBehaviour
{
    [SerializeField] private float timeGapBetweenLetters = 0.05f;
    [SerializeField] private int maxTextLength = 300;
    [SerializeField] private EventChannel<string> eventChannel;
        
    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        eventChannel?.AddListener(Print);
    }

    private void OnDisable()
    {
        eventChannel?.RemoveListener(Print);
    }

    private CancellationTokenSource _cts;
        
    public void Print(string text)
    {
        _text.text = string.Empty;
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
                _text.text = string.Empty;
            }
            var page = text.Substring(i, maxLen);
            foreach (var ch in page)
            {
                await UniTask.WaitForSeconds(timeGap, cancellationToken:token);
                _text.text += ch;
            }
        }
    }
}