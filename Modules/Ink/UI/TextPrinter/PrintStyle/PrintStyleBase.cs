using System;
using EditorAttributes;
using UnityEngine;

namespace ModularFramework.Modules.Ink
{
    public abstract class PrintStyleBase : ScriptableObject
    {

        [Rename("Don't Clear Text"),Tooltip("Append new text as new lines")] 
        public bool noClearText;

        protected string cachedText;
        public TextPrinter Printer { get; set; }

        public bool ReturnEarly { get; set; }

        protected bool ReturnedEarly { get; set; }

        public abstract void OnPrint(string text, Action callback = null);

        public abstract void OnSkip();

        public abstract void OnDestroy();

        protected string Prepare(string text)
        {
            if (text != null && text.StartsWith(InkConstants.VAR_APPEND))
            {
                return text[InkConstants.VAR_APPEND.Length..];
            }
            if (noClearText)
            {
                Printer.textbox.text += "\n";
            }
            else
            {
                Printer.textbox.text = string.Empty;
            }

            cachedText = Printer.textbox.text;
            Printer.Done = false;
            ReturnedEarly = false;
            return text;
        }

        protected void Finish(string text = null)
        {
            if (text != null)
            {
                Printer.textbox.text = GetFinalText(text);
            }

            Printer.Done = true;
            if (ReturnedEarly)
            {
                // clean after self
                Printer.Clean();
            }
        }



        protected string GetFinalText(string text) => noClearText ? cachedText + text : text;

    }
}