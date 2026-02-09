using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EditorAttributes;
using ModularFramework.Commons;
using UnityEngine;

[CreateAssetMenu(fileName = "TypeOut_SO", menuName = "Game Module/Ink/Print Style/TypeOut")]
public class TypeOut : PrintStyleBase
{

    [Header("Config")]
    [SerializeField] private float timeGapBetweenLetters = 0.05f;
    [SerializeField] private bool cursor;
    [SerializeField, ShowField(nameof(cursor))] private string cursorSymbol = "|";
    [SerializeField, ShowField(nameof(cursor))] private float blinkTime = 0.1f;
        
    private CancellationTokenSource _cts;
    
    public override void OnDestroy()
    {
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // nothing
            
        }
    }

    public override void OnSkip()
    {
        _cts.Cancel();
    }

    public override void OnPrint(string text, Action callback=null)
    {
        if(Printer.endIndicator) Printer.endIndicator.SetActive(false);
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        _cts = new CancellationTokenSource();
        
        Prepare(); 
        if(cursor) PrintTaskCursor(text, callback, _cts.Token).Forget();
        else  PrintTask(text, callback, _cts.Token).Forget();
    }

    private async UniTaskVoid PrintTask(string text, Action callback,CancellationToken token)
    {
        Printer.gameObject.SetActive(true);
        bool lastCharIsPunctuation = false;
        SoundPlayer soundPlayer = Printer.GetSoundPlayer();
        bool isTextTag = false;
        foreach (var ch in text)
        {
            if(!isTextTag) isTextTag = ch=='<';
            if (isTextTag)
            {
                Printer.textbox.text += ch;
                if (ch == '>') isTextTag = false;
                continue;
            }
            
            bool punctuation = char.IsPunctuation(ch);
            bool wait = !lastCharIsPunctuation && punctuation;
            float t;
            if (wait)
            {
                if(soundPlayer) soundPlayer.SetVolume(0);
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
                Finish(text); // canceled and no new print task
                soundPlayer?.Stop();
                callback?.Invoke();
                if(Printer.endIndicator) Printer.endIndicator.SetActive(true);
                return;
            }
            Printer.textbox.text += ch;
            if (ReturnEarly && text.Length - Printer.textbox.text.Length == 2)
            {
                ReturnedEarly = true;
                callback?.Invoke();
            } 
        }
        soundPlayer?.Stop();
        Finish();
        if(!ReturnedEarly) callback?.Invoke();
        if(Printer.endIndicator) Printer.endIndicator.SetActive(true);
    }
    
    private async UniTaskVoid PrintTaskCursor(string text, Action callback, CancellationToken token)
    {
        Printer.gameObject.SetActive(true);
        bool lastCharIsPunctuation = false;
        SoundPlayer soundPlayer =  Printer.GetSoundPlayer();
        Flip flip = new Flip();
        bool printCursor = false;
        float b = blinkTime;
        bool isTextTag = false;
        foreach (var ch in text)
        {
            if(!isTextTag) isTextTag = ch=='<';
            if (isTextTag)
            {
                Printer.textbox.text += ch;
                if (ch == '>') isTextTag = false;
                continue;
            }
            
            bool punctuation = char.IsPunctuation(ch);
            bool wait = !lastCharIsPunctuation && punctuation;
            lastCharIsPunctuation = punctuation;
            float gap;
            if (wait)
            {
                if(soundPlayer) soundPlayer.SetVolume(0);
                gap = timeGapBetweenLetters * 5;
            }
            else
            {
                gap = timeGapBetweenLetters;
                soundPlayer?.ResetVolume();
            }
            
            float t = gap;
            string txt =  Printer.textbox.text;
            
            while (t > 0)
            {
                if (b >= blinkTime)
                {
                    printCursor = flip;
                    b = 0;
                }

                b += Time.deltaTime;
                
                if(printCursor)  Printer.textbox.text = txt + cursorSymbol;
                else  Printer.textbox.text = txt;
                
                t-=Time.deltaTime;
                bool isCanceled = await UniTask.NextFrame(cancellationToken:token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    Finish(text); // canceled and no new print task
                    callback?.Invoke();
                    if(Printer.endIndicator) Printer.endIndicator.SetActive(true);
                    soundPlayer?.Stop();
                    return;
                }
            }

            Printer.textbox.text = txt + ch;
            if (ReturnEarly && text.Length - Printer.textbox.text.Length == 2)
            {
                ReturnedEarly = true;
                callback?.Invoke();
            } 
        }
        soundPlayer?.Stop();
        Finish(); 
        if(!ReturnedEarly) callback?.Invoke();
        if(Printer.endIndicator) Printer.endIndicator.SetActive(true);
    }

}