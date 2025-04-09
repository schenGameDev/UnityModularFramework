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
    private TextPrinterBase _iTextPrinter;

    private void OnEnable()
    {
        interactInputChannel.AddListener(Clean);
    }

    private void OnDisable()
    {
        interactInputChannel.RemoveListener(Clean);
    }

    protected override void Start()
    {
        _iTextPrinter = printer;
    } // stop parent Start
    
    public void Read()
    {
        if(_iTextPrinter==null) return;
        _iTextPrinter.Print(text);
        _isShow = true;
    }

    private void Clean()
    {
        if(_iTextPrinter==null) return;
        if (_isShow)
        {
            if(printer.Done) printer.Clean();
            else printer.Skip();
        }
    }
}