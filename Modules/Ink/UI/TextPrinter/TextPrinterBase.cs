using System;
using ModularFramework;
using UnityEngine;
[RequireComponent(typeof(Marker)),DisallowMultipleComponent]
public abstract class TextPrinterBase : MonoBehaviour
{
    /// <summary>
    /// override preset parameters when print text
    /// </summary>
    /// <param name="text"></param>
    /// <param name="callback"></param>
    /// <param name="parameters"/>
    public abstract void Print(string text, Action callback, params string[] parameters);

    public void Print(string text) => Print(text, null);

    public abstract void Skip();
    
    public abstract void Clean();
    
    public bool Done { get; set; }
}