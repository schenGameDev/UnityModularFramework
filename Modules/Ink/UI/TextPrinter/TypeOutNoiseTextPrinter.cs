using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TypeOutNoiseTextPrinter : TextPrinter
{
    [Header("Config")]
    [SerializeField] private float timeGapBetweenLetters = 0.05f;
        
    private CancellationTokenSource _cts;
    
    protected void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public override void Skip()
    {
        _cts.Cancel();
    }

    public override void Print(string text, Action callback)
    {
        Done = false;
        endIndicator?.SetActive(false);
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        _cts = new CancellationTokenSource();
        
        Textbox.text = string.Empty; 
        PrintTaskNoise(text, callback,_cts.Token).Forget(); 
    }

    private async UniTaskVoid PrintTaskNoise(string text, Action callback, CancellationToken token)
    {
        gameObject.SetActive(true);
        SoundPlayer soundPlayer = SoundManager?.PlayLoopSound(soundName);
        foreach (var ch in text)
        {
            float t = timeGapBetweenLetters;
            string txt = Textbox.text;
            while (t > 0)
            {
                Textbox.text = txt + RandomChar();
                t-=Time.deltaTime;
                bool isCanceled= await UniTask.NextFrame(cancellationToken:token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    if(_cts==null) {
                        Textbox.text = text; // canceled and no new print task
                        Done = true;
                        callback?.Invoke();
                        endIndicator?.SetActive(true);
                        soundPlayer?.Stop();
                    }
                    return;
                }
            }
            Textbox.text = txt + ch;
        }
        Done = true;
        callback?.Invoke();
        endIndicator?.SetActive(true);
        soundPlayer?.Stop();
    }
    
    private string RandomChar()
    {
        byte value = (byte)UnityEngine.Random.Range(41f,128f);

        string c = Encoding.ASCII.GetString(new byte[]{value});

        return c;

    }
}