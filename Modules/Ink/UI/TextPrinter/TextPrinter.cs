using System;
using System.Collections.Generic;
using ModularFramework;
using TMPro;
using UnityEngine;

public class TextPrinter : TextPrinterBase
{
    public static readonly Dictionary<string,TextPrinter> INSTANCES = new ();
    
    [SerializeField] private bool staticPrinter;
    [SerializeField] public GameObject endIndicator;
    
    [SerializeField] protected string soundName;

    [SerializeField] private PrintStyleBase printStyle;
    
    private PrintStyleBase _printStyleInstance;
    
    protected Autowire<SoundManagerSO> SoundManager = new();
    public TextMeshProUGUI Textbox { get; private set; }
    private Action _callback;

    protected void Awake()
    {
        Textbox = GetComponentInChildren<TextMeshProUGUI>(true);
        if (!Textbox)
        {
            throw new MissingComponentException("Missing TextMeshProUGUI");
        }

        _printStyleInstance = printStyle ? Instantiate(printStyle) : ScriptableObject.CreateInstance<NoPrintStyle>();
        _printStyleInstance.Printer = this;
        if(endIndicator) endIndicator.SetActive(false);

        if (staticPrinter)
        {
            INSTANCES.Add(printerName, this);
            gameObject.SetActive(false);
        }
    }

    public override void Skip()
    {
        if (Done)
        {
            _callback?.Invoke();
            return;
        }
        _printStyleInstance.OnSkip();
    }

    public override void Clean() // click again to hide
    {
        if(hideWhenNotUsed) gameObject.SetActive(false);
        else if(!_printStyleInstance.noClearText) Textbox.text = "";
        ReturnEarly = false;
        Done = false;
        _callback = null;
    }

    public override void Print(string text, Action callback, params string[] parameters)
    {
        gameObject.SetActive(true);
        _callback = callback;
        _printStyleInstance.ReturnEarly = ReturnEarly;
        _printStyleInstance.OnPrint(text, callback);
    }

    private void OnDestroy()
    {
        _printStyleInstance.OnDestroy();
        INSTANCES.Remove(printerName);
    }

    public SoundPlayer GetSoundPlayer() => SoundManager.Get()?.PlayLoopSound(soundName);
}