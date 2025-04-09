using System;

public class NoPrintStyle : PrintStyleBase
{
    public override void OnPrint(string text, Action callback)
    {
        Printer.Done = false;
        Printer.endIndicator?.SetActive(false);
        Printer.Textbox.text = text;
        Printer.Done = true;
        callback?.Invoke();
    }

    public override void OnSkip(){}
    
    public override void OnDestroy(){}

}