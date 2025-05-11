using System;

public class NoPrintStyle : PrintStyleBase
{
    public override void OnPrint(string text, Action callback)
    {
        Prepare();
        if(Printer.endIndicator) Printer.endIndicator.SetActive(false);
        Finish(text);
        callback?.Invoke();
    }

    public override void OnSkip(){}
    
    public override void OnDestroy(){}

}