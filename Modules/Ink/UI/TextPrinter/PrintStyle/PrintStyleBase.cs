using System;
using EditorAttributes;
using UnityEngine;

public abstract class PrintStyleBase : ScriptableObject
{
    
    [Rename("Don't Clear Text")] public bool noClearText;
    protected string CachedText;
    public TextPrinter Printer { get; set; }
    
    public bool ReturnEarly { get; set; }
    
    protected bool ReturnedEarly { get; set; }

    public abstract void OnPrint(string text, Action callback = null);

    public abstract void OnSkip();

    public abstract void OnDestroy();

    protected void Prepare()
    {
        if (noClearText)
        {
            Printer.textbox.text += "\n";
        }
        else
        {
            Printer.textbox.text = string.Empty; 
        }
        CachedText = Printer.textbox.text;
        Printer.Done = false;
        ReturnedEarly = false;
    }

    protected void Finish(string text=null)
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
    
    

    protected string GetFinalText(string text) => noClearText ? CachedText + text : text;

}