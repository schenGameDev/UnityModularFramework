using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ModularFramework;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityUtils;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextPrinter : Marker
{
    public enum WordEffect {NONE, TYPE, FADE_IN}
    
    [SerializeField] private WordEffect wordEffect;
    [SerializeField] private float timeGapBetweenLetters = 0.05f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private bool hideWhenNotUsed;
    [SerializeField] private EventChannel<string> eventChannel;
        
    private TextMeshProUGUI _textbox;

    public TextPrinter()
    {
        registryTypes = new[] { (typeof(InkUIIntegrationSO),1)};
    }
    
    private void Awake()
    {
        _textbox = GetComponent<TextMeshProUGUI>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        eventChannel?.AddListener(Print);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        eventChannel?.RemoveListener(Print);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public void Skip()
    {
        _cts.Cancel();
    }

    public void Clean() // click again to hide
    {
        if(hideWhenNotUsed) gameObject.SetActive(false);
        else _textbox.text = "";
    }
    
    public bool Done { get; private set; }

    private CancellationTokenSource _cts;
    
    /// <summary>
    /// override preset parameters when print text
    /// </summary>
    /// <param name="text"></param>
    /// <param name="effect"></param>
    /// <param name="parameter"></param>
    public void Print(string text, WordEffect effect, string parameter)
    {
        Done = false;
        if (effect == WordEffect.NONE)
        {
            _textbox.text = text;
            Done = true;
            return;
        }
        
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        _cts = new CancellationTokenSource();
        
        if (effect == WordEffect.FADE_IN)
        {
            _textbox.text = text;
            FadeIn(_cts.Token).Forget();
        } 
        else if (effect == WordEffect.TYPE)
        {
            _textbox.text = string.Empty;
            PrintTask(text, timeGapBetweenLetters, _cts.Token).Forget();
        }
    }
    
    public void Print(string text) => Print(text, wordEffect, "");

    private async UniTaskVoid PrintTask(string text, float timeGap, CancellationToken token)
    {
        gameObject.SetActive(true);
        foreach (var ch in text)
        {
            var isCanceled = await UniTask.WaitForSeconds(timeGap, cancellationToken:token).SuppressCancellationThrow();
            if (isCanceled)
            {
                if(_cts==null) {
                    _textbox.text = text; // canceled and no new print task
                    Done = true;
                }
                return;
            }
            _textbox.text += ch;
        }
        Done = true;
    }

    private async UniTaskVoid FadeIn(CancellationToken token)
    {
        float t = 0;
        bool isCancelled = false;
        while(t< fadeInDuration && !isCancelled) 
        {
            _textbox.color.SetAlpha(math.min(1,t/fadeInDuration));
            t+=Time.deltaTime;
            isCancelled = await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
        }
        
        _textbox.color.SetAlpha(1);
        Done = true;
    }
}