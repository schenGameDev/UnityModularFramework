using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EditorAttributes;
using ModularFramework.Commons;
using UnityEngine;

namespace ModularFramework.Modules.Ink
{
    [CreateAssetMenu(fileName = "TypeOut_SO", menuName = "Game Module/Ink/Print Style/TypeOut")]
    public class TypeOut : PrintStyleBase
    {
        
        [Header("Config")] 
        [SerializeField,Suffix("s")] 
        private float timeGapBetweenLetters = 0.05f;
        
        [SerializeField,ToggleGroup("Wait At Punctuation", nameof(timeGapBetweenPunctuation), nameof(customizedPunctuations), nameof(punctuations))]
        private bool waitAtPunctuation;
        
        [SerializeField,Suffix("s"),HideInInspector]
        private float timeGapBetweenPunctuation = 0.25f;
        [SerializeField, HideInInspector] private bool customizedPunctuations;
        [SerializeField,HideInInspector,ShowField(nameof(customizedPunctuations))]
        [HelpBox("Be aware of language specific punctuations.", MessageMode.Warning)] 
        private string punctuations = ":;,.!?";
        
        [SerializeField,ToggleGroup("Cursor", nameof(cursorSymbol), nameof(blinkTime))]
        private bool cursor;
        [SerializeField, HideInInspector]
        private string cursorSymbol = "|";
        [SerializeField, HideInInspector]
        private float blinkTime = 0.1f;

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
            if (cursor) PrintTaskCursor(text, OnPrintComplete, _cts.Token).Forget();
            else PrintTask(text, OnPrintComplete, _cts.Token).Forget();
        }

        private async UniTaskVoid PrintTask(string text, Action callback, CancellationToken token)
        {
            Printer.gameObject.SetActive(true);
            bool lastCharIsPunctuation = false;
            bool isTextTag = false;
            
            var isCanceled = await UniTask.WaitUntil(() => !Paused, cancellationToken: token).SuppressCancellationThrow();
            if (isCanceled)
            {
                OnCancel(text);
                return;
            }
            
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

                isCanceled = await UniTask.WaitUntil(() => !Paused, cancellationToken: token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    OnCancel(text);
                    return;
                }
                
                bool punctuation = IsPunctuation(ch);
                bool wait = !lastCharIsPunctuation && punctuation;
                float t;
                if (wait)
                {
                    t = timeGapBetweenPunctuation;
                }
                else
                {
                    t = timeGapBetweenLetters;
                }

                lastCharIsPunctuation = punctuation;
                isCanceled = await UniTask.WaitForSeconds(t, cancellationToken: token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    OnCancel(text);
                    return;
                }

                Printer.textbox.text += ch;
                OnTextChanged?.Invoke();
                
                if (ReturnEarly && text.Length - Printer.textbox.text.Length == 2)
                {
                    ReturnedEarly = true;
                    callback?.Invoke();
                }
            }
            
            Finish();
            if (!ReturnedEarly) callback?.Invoke();
            if (Printer.endIndicator) Printer.endIndicator.SetActive(true);
        }

        private bool IsPunctuation(char ch)
        {
            return waitAtPunctuation && (customizedPunctuations ? punctuations.Contains(ch) : char.IsPunctuation(ch));
        }

        private async UniTaskVoid PrintTaskCursor(string text, Action callback, CancellationToken token)
        {
            Printer.gameObject.SetActive(true);
            bool lastCharIsPunctuation = false;
            
            Flip flip = new Flip();
            bool printCursor = false;
            float b = blinkTime;
            bool isTextTag = false;
            
            var isCanceled = await UniTask.WaitUntil(() => !Paused, cancellationToken: token).SuppressCancellationThrow();
            if (isCanceled)
            {
                OnCancel(text);
                return;
            }
            
            foreach (var ch in text)
            {
                if (!isTextTag) isTextTag = ch == '<';
                if (isTextTag)
                {
                    Printer.textbox.text += ch;
                    if (ch == '>') isTextTag = false;
                    continue;
                }

                isCanceled = await UniTask.WaitUntil(() => !Paused, cancellationToken: token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    OnCancel(text);
                    return;
                }
                
                bool punctuation = IsPunctuation(ch);
                bool wait = !lastCharIsPunctuation && punctuation;
                lastCharIsPunctuation = punctuation;
                float gap;
                if (wait)
                {
                    gap = timeGapBetweenPunctuation;
                }
                else
                {
                    gap = timeGapBetweenLetters;
                }

                float t = gap;
                string txt = Printer.textbox.text;

                while (t > 0)
                {
                    if (b >= blinkTime)
                    {
                        printCursor = flip;
                        b = 0;
                    }

                    b += Time.deltaTime;

                    if (printCursor) Printer.textbox.text = txt + cursorSymbol;
                    else Printer.textbox.text = txt;

                    t -= Time.deltaTime;
                    isCanceled = await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
                    if (isCanceled)
                    {
                        OnCancel(text);
                        return;
                    }
                }
                
                isCanceled = await UniTask.WaitUntil(() => !Paused, cancellationToken: token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    OnCancel(text);
                    return;
                }

                Printer.textbox.text = txt + ch;
                OnTextChanged?.Invoke();
                
                if (ReturnEarly && text.Length - Printer.textbox.text.Length == 2)
                {
                    ReturnedEarly = true;
                    callback?.Invoke();
                }
            }
            
            Finish();
            if (!ReturnedEarly) callback?.Invoke();
            if (Printer.endIndicator) Printer.endIndicator.SetActive(true);
        }
        
        private void OnCancel(string text)
        {
            Finish(text); // canceled and no new print task
            OnPrintComplete?.Invoke();
            if (Printer.endIndicator) Printer.endIndicator.SetActive(true);
        }

    }
}