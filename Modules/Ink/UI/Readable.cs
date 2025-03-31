using System;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;

/// <summary>
/// display text somewhere else after clicked
/// </summary>
public class Readable : TranslationText
{
    [SerializeField] private TextPrinter printer;
    // [SerializeField] private bool clickAgainHide = true;
    [SerializeField] private EventChannel interactInputChannel;

    private bool _isShow;


    private void OnEnable()
    {
        interactInputChannel.AddListener(Clean);
    }

    private void OnDisable()
    {
        interactInputChannel.RemoveListener(Clean);
    }

    protected override void Start() { } // stop parent Start
    
    public void Read()
    {
        printer?.Print(text);
        _isShow = true;
    }

    private void Clean()
    {
        if(!printer) return;
        if (_isShow)
        {
            if(printer.Done) printer.Clean();
            else printer.Skip();
        }
    }
}