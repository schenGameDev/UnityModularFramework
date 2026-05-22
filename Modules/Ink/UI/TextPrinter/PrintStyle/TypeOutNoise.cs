using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ModularFramework.Modules.Ink
{
    [CreateAssetMenu(fileName = "TypeOutNoise_SO", menuName = "Game Module/Ink/Print Style/TypeOutNoise")]
    public class TypeOutNoise : PrintStyleBase
    {

        [Header("Config")] [SerializeField] private float timeGapBetweenLetters = 0.05f;

        private CancellationTokenSource _cts;

        public override void Destroy()
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

        public override void Skip()
        {
            _cts.Cancel();
        }

        public override void Print(string text)
        {
            if (Printer.endIndicator) Printer.endIndicator.SetActive(false);
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            _cts = new CancellationTokenSource();

            text = Prepare(text);
            PrintTaskNoise(text, _cts.Token).Forget();
        }

        private async UniTaskVoid PrintTaskNoise(string text, CancellationToken token)
        {
            Printer.gameObject.SetActive(true);
            bool isTextTag = false;
            foreach (var ch in text)
            {
                if (!isTextTag) isTextTag = ch == '<';
                if (isTextTag)
                {
                    Printer.textbox.text += ch;
                    OnTextChanged?.Invoke();
                    if (ch == '>') isTextTag = false;
                    continue;
                }

                float t = timeGapBetweenLetters;
                string txt = Printer.textbox.text;
                while (t > 0)
                {
                    Printer.textbox.text = txt + RandomChar();
                    t -= Time.deltaTime;
                    bool isCanceled = await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
                    if (isCanceled)
                    {
                        Finish(text); // canceled and no new print task
                        OnPrintComplete?.Invoke();
                        if (Printer.endIndicator) Printer.endIndicator.SetActive(true);
                        return;
                    }
                }

                Printer.textbox.text = txt + ch;
                if (ReturnEarly && text.Length - Printer.textbox.text.Length == 2)
                {
                    OnPrintComplete?.Invoke();
                    ReturnedEarly = true;
                }
            }

            Finish();
            if (!ReturnedEarly) OnPrintComplete?.Invoke();
            if (Printer.endIndicator) Printer.endIndicator.SetActive(true);
        }

        private string RandomChar()
        {
            byte value = (byte)Random.Range(41f, 128f);

            string c = Encoding.ASCII.GetString(new byte[] { value });

            return c;

        }
    }
}