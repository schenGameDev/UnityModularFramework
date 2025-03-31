using System;
using ModularFramework;
using TMPro;
using UnityEngine;

public class TextPrinter : Marker
{
    
    [SerializeField] protected GameObject endIndicator;
    [SerializeField] private bool hideWhenNotUsed;
    [SerializeField] private EventChannel<string> eventChannel;
    [SerializeField] protected string soundName;
    
    protected SoundManagerSO SoundManager;
    protected TextMeshProUGUI Textbox;

    public TextPrinter()
    {
        registryTypes = new[] { (typeof(InkUIIntegrationSO),1)};
    }
    
    private void Awake()
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
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        eventChannel?.AddListener(Print);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        eventChannel?.RemoveListener(Print);
    }

    public virtual void Skip() { }

    public void Clean() // click again to hide
    {
        if(hideWhenNotUsed) gameObject.SetActive(false);
        else Textbox.text = "";
    }
    
    public bool Done { get; protected set; }
    
    
    /// <summary>
    /// override preset parameters when print text
    /// </summary>
    /// <param name="text"></param>
    /// <param name="effect"></param>
    /// <param name="callback"></param>
    public virtual void Print(string text, Action callback)
    {
        Done = false;
        endIndicator?.SetActive(false);
        Textbox.text = text;
        Done = true;
        callback?.Invoke();
    }

    public void Print(string text) => Print(text, null);
    
    
}