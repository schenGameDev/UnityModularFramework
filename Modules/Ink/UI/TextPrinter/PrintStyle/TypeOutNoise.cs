using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "TypeOutNoise_SO", menuName = "Game Module/Ink/Print Style/TypeOutNoise")]
public class TypeOutNoise : PrintStyleBase
{

    [Header("Config")]
    [SerializeField] private float timeGapBetweenLetters = 0.05f;
        
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
        PrintTaskNoise(text, callback,_cts.Token).Forget(); 
    }

    private async UniTaskVoid PrintTaskNoise(string text, Action callback, CancellationToken token)
    {
        Printer.gameObject.SetActive(true);
        SoundPlayer soundPlayer = Printer.GetSoundPlayer();
        bool isTextTag = false;
        foreach (var ch in text)
        {
            if(!isTextTag) isTextTag = ch=='<';
            if (isTextTag)
            {
                Printer.Textbox.text += ch;
                if (ch == '>') isTextTag = false;
                continue;
            }
           
            float t = timeGapBetweenLetters;
            string txt = Printer.Textbox.text;
            while (t > 0)
            {
                Printer.Textbox.text = txt + RandomChar();
                t-=Time.deltaTime;
                bool isCanceled= await UniTask.NextFrame(cancellationToken:token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    Finish(text); // canceled and no new print task
                    callback?.Invoke();
                    if(Printer.endIndicator) Printer.endIndicator.SetActive(true);
                    soundPlayer?.Stop();
                    return;
                }
            }
            Printer.Textbox.text = txt + ch;
            if (ReturnEarly && text.Length - Printer.Textbox.text.Length == 2)
            {
                callback?.Invoke();
                ReturnedEarly = true;
            } 
        }
        
        Finish();
        if(!ReturnedEarly) callback?.Invoke();
        if(Printer.endIndicator) Printer.endIndicator.SetActive(true);
        soundPlayer?.Stop();
    }
    
    private string RandomChar()
    {
        byte value = (byte)Random.Range(41f,128f);

        string c = Encoding.ASCII.GetString(new byte[]{value});

        return c;

    }
}