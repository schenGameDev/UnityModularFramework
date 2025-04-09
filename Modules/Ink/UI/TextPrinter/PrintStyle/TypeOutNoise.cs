using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "TypeOutNoise_SO", menuName = "Game Module/Ink/Print Style/TypeOutNoise")]
public class TypeOutNoise : PrintStyleBase
{

    [Header("Config")]
    [SerializeField] private float timeGapBetweenLetters = 0.05f;
        
    private CancellationTokenSource _cts;
    
    public override void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public override void OnSkip()
    {
        _cts.Cancel();
    }

    public override void OnPrint(string text, Action callback=null)
    {
        Printer.Done = false;
        Printer.endIndicator?.SetActive(false);
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        _cts = new CancellationTokenSource();
        
        Printer.Textbox.text = string.Empty; 
        PrintTaskNoise(text, callback,_cts.Token).Forget(); 
    }

    private async UniTaskVoid PrintTaskNoise(string text, Action callback, CancellationToken token)
    {
        Printer.gameObject.SetActive(true);
        SoundPlayer soundPlayer = Printer.GetSoundPlayer();
        foreach (var ch in text)
        {
            float t = timeGapBetweenLetters;
            string txt = Printer.Textbox.text;
            while (t > 0)
            {
                Printer.Textbox.text = txt + RandomChar();
                t-=Time.deltaTime;
                bool isCanceled= await UniTask.NextFrame(cancellationToken:token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    if(_cts==null) {
                        Printer.Textbox.text = text; // canceled and no new print task
                        Printer.Done = true;
                        callback?.Invoke();
                        Printer.endIndicator?.SetActive(true);
                        soundPlayer?.Stop();
                    }
                    return;
                }
            }
            Printer.Textbox.text = txt + ch;
        }
        Printer.Done = true;
        callback?.Invoke();
        Printer.endIndicator?.SetActive(true);
        soundPlayer?.Stop();
    }
    
    private string RandomChar()
    {
        byte value = (byte)UnityEngine.Random.Range(41f,128f);

        string c = Encoding.ASCII.GetString(new byte[]{value});

        return c;

    }
}