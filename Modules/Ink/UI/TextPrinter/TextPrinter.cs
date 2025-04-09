using System;
using EditorAttributes;
using ModularFramework;
using TMPro;
using UnityEngine;

public class TextPrinter : TextPrinterBase
{
    [SerializeField] public GameObject endIndicator;
    [SerializeField] private bool hideWhenNotUsed;
    
    [SerializeField] protected string soundName;
    
    //[SerializeField,TypeFilter(typeof(PrintStyleBase))] 
    //private SerializableType printStyle;
    [SerializeField] private PrintStyleBase printStyle;
    
    protected SoundManagerSO SoundManager;
    public TextMeshProUGUI Textbox { get; private set; }

    protected void Awake()
    {
        Textbox = GetComponentInChildren<TextMeshProUGUI>();
        if (!Textbox)
        {
            throw new MissingComponentException("Missing TextMeshProUGUI");
        }
        if (soundName.NonEmpty())
        {
            SoundManager = GameRunner.Instance.GetModule<SoundManagerSO>().OrElse(null);
        }

        if (printStyle == null) printStyle = ScriptableObject.CreateInstance<NoPrintStyle>();
        
    }

    public override void Skip()
    {
        printStyle.OnSkip();
    }

    public override void Clean() // click again to hide
    {
        if(hideWhenNotUsed) gameObject.SetActive(false);
        else Textbox.text = "";
    }

    public override void Print(string text, Action callback, params string[] parameters)
    {
        printStyle.OnPrint(text, callback);
    }

    private void OnDestroy()
    {
        printStyle.OnDestroy();
    }

    public SoundPlayer GetSoundPlayer() => SoundManager?.PlayLoopSound(soundName);
}