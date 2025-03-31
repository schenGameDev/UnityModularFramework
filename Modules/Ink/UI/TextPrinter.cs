using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using EditorAttributes;
using ModularFramework;
using ModularFramework.Commons;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextPrinter : Marker
{
    public enum PrintEffect {NONE, TYPE, FADE_IN}
    private enum CursorEffect {NONE, NOISE, SYMBOL}
    
    [SerializeField] private PrintEffect defaultPrintEffect = PrintEffect.NONE;
    [SerializeField] private GameObject endIndicator;
    [Header("Type out")]
    [SerializeField] private float timeGapBetweenLetters = 0.05f;

    [SerializeField] private CursorEffect cursorEffect = CursorEffect.NONE;
    [SerializeField, ShowField(nameof(cursorEffect), CursorEffect.SYMBOL)] private string cursorSymbol = "|";
    [SerializeField, ShowField(nameof(cursorEffect), CursorEffect.SYMBOL)] private float blinkTime = 0.1f;
    [Header("Fade In")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private bool hideWhenNotUsed;
    [SerializeField] private EventChannel<string> eventChannel;
    [Header("Sound")]
    [SerializeField] private string soundName;
    private SoundManagerSO _soundManager;
        
    private TextMeshProUGUI _textbox;

    public TextPrinter()
    {
        registryTypes = new[] { (typeof(InkUIIntegrationSO),1)};
    }
    
    private void Awake()
    {
        _textbox = GetComponentInChildren<TextMeshProUGUI>();
    }

    protected override void Start()
    {
        base.Start();
        if (soundName.NonEmpty())
        {
            _soundManager = GameRunner.Instance.GetModule<SoundManagerSO>().OrElse(null);
        }
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
    /// <param name="callback"></param>
    public void Print(string text, PrintEffect effect, Action callback = null)
    {
        Done = false;
        endIndicator?.SetActive(false);
        if (effect == PrintEffect.NONE)
        {
            _textbox.text = text;
            Done = true;
            callback?.Invoke();
            return;
        }
        
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        _cts = new CancellationTokenSource();
        
        if (effect == PrintEffect.FADE_IN)
        {
            _textbox.text = text;
            FadeIn(callback,_cts.Token).Forget();
        } 
        else if (effect == PrintEffect.TYPE)
        {
            _textbox.text = string.Empty;
            if(cursorEffect == CursorEffect.NOISE) PrintTaskNoise(text, callback,_cts.Token).Forget(); 
            else if(cursorEffect==CursorEffect.SYMBOL) PrintTaskCursor(text, callback, _cts.Token).Forget();
            else  PrintTask(text, callback, _cts.Token).Forget();
            
        }
    }
    
    public void Print(string text) => Print(text, defaultPrintEffect);
    
    public void Print(string text, Action callback) => Print(text, defaultPrintEffect, callback);

    private async UniTaskVoid PrintTask(string text, Action callback,CancellationToken token)
    {
        gameObject.SetActive(true);
        bool lastCharIsPunctuation = false;
        SoundPlayer soundPlayer = _soundManager?.PlayLoopSound(soundName);
        foreach (var ch in text)
        {
            bool punctuation = char.IsPunctuation(ch);
            bool wait = !lastCharIsPunctuation && punctuation;
            float t;
            if (wait)
            {
                soundPlayer?.SetVolume(0);
                t = timeGapBetweenLetters * 5;
            }
            else
            {
                t = timeGapBetweenLetters;
                soundPlayer?.ResetVolume();
            }

            lastCharIsPunctuation = punctuation;
            bool isCanceled= await UniTask.WaitForSeconds(t, cancellationToken:token).SuppressCancellationThrow();
            if (isCanceled)
            {
                if(_cts==null) {
                    _textbox.text = text; // canceled and no new print task
                    soundPlayer?.Stop();
                    Done = true;
                    callback?.Invoke();
                    endIndicator?.SetActive(true);
                }
                return;
            }
            _textbox.text += ch;
        }
        soundPlayer?.Stop();
        Done = true;
        callback?.Invoke();
        endIndicator?.SetActive(true);
    }
    
    private async UniTaskVoid PrintTaskCursor(string text, Action callback, CancellationToken token)
    {
        gameObject.SetActive(true);
        bool lastCharIsPunctuation = false;
        SoundPlayer soundPlayer = _soundManager?.PlayLoopSound(soundName);
        Flip flip = new Flip();
        bool printCursor = false;
        float b = blinkTime;
        
        foreach (var ch in text)
        {
            bool punctuation = char.IsPunctuation(ch);
            bool wait = !lastCharIsPunctuation && punctuation;
            lastCharIsPunctuation = punctuation;
            float gap;
            if (wait)
            {
                soundPlayer?.SetVolume(0);
                gap = timeGapBetweenLetters * 5;
            }
            else
            {
                gap = timeGapBetweenLetters;
                soundPlayer?.ResetVolume();
            }
            
            float t = gap;
            string txt = _textbox.text;
            
            while (t > 0)
            {
                if (b >= blinkTime)
                {
                    printCursor = flip;
                    b = 0;
                }

                b += Time.deltaTime;
                
                if(printCursor) _textbox.text = txt + cursorSymbol;
                else _textbox.text = txt;
                
                t-=Time.deltaTime;
                bool isCanceled = await UniTask.NextFrame(cancellationToken:token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    if(_cts==null) {
                        _textbox.text = text; // canceled and no new print task
                        Done = true;
                        callback?.Invoke();
                        endIndicator?.SetActive(true);
                        soundPlayer?.Stop();
                    }
                    return;
                }
            }

            _textbox.text = txt + ch;

        }
        soundPlayer?.Stop();
        Done = true;
        callback?.Invoke();
        endIndicator?.SetActive(true);
    }
    
    private async UniTaskVoid PrintTaskNoise(string text, Action callback, CancellationToken token)
    {
        gameObject.SetActive(true);
        SoundPlayer soundPlayer = _soundManager?.PlayLoopSound(soundName);
        foreach (var ch in text)
        {
            float t = timeGapBetweenLetters;
            string txt = _textbox.text;
            while (t > 0)
            {
                _textbox.text = txt + RandomChar();
                t-=Time.deltaTime;
                bool isCanceled= await UniTask.NextFrame(cancellationToken:token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    if(_cts==null) {
                        _textbox.text = text; // canceled and no new print task
                        Done = true;
                        callback?.Invoke();
                        endIndicator?.SetActive(true);
                        soundPlayer?.Stop();
                    }
                    return;
                }
            }
            _textbox.text = txt + ch;
        }
        Done = true;
        callback?.Invoke();
        endIndicator?.SetActive(true);
        soundPlayer?.Stop();
    }

    private async UniTaskVoid FadeIn(Action callback, CancellationToken token)
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
        callback?.Invoke();
        Done = true;
    }
    
    private string RandomChar()
    {
        byte value = (byte)UnityEngine.Random.Range(41f,128f);

        string c = Encoding.ASCII.GetString(new byte[]{value});

        return c;

    }
}