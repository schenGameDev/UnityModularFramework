using System;
using UnityEngine;

public abstract class PrintStyleBase : ScriptableObject
{
    protected TextPrinter Printer;

    public abstract void OnPrint(string text, Action callback = null);

    public abstract void OnSkip();

    public abstract void OnDestroy();
}