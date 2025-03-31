using ModularFramework.Utility;
using UnityEngine;

/// <summary>
/// display text somewhere else after clicked
/// </summary>
public class Readable : TranslationText
{
    [SerializeField] private TextPrinter printer;
    

    protected override void Start() { } // stop parent Start

    public void Read()
    {
        printer?.Print(text);
    }

}