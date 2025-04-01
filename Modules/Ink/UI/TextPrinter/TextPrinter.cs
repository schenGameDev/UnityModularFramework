using System;
using EditorAttributes;
using ModularFramework;
using TMPro;
using UnityEngine;

public class TextPrinter : Marker
{
    protected enum Type {NONE, EVENT_LISTENER, INK_TASK_LISTENER}
    [SerializeField] protected GameObject endIndicator;
    [SerializeField] private bool hideWhenNotUsed;
    [SerializeField] protected Type type;
    [SerializeField, ShowField(nameof(type), Type.EVENT_LISTENER)] private EventChannel<string> eventChannel;
    [SerializeField, ShowField(nameof(type), Type.INK_TASK_LISTENER)] private EventChannel<(string,string,Action<string>)> inkTaskChannel;

    [SerializeField, ShowField(nameof(type), Type.INK_TASK_LISTENER)] private string taskName;
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
        if(type == Type.EVENT_LISTENER) eventChannel?.AddListener(Print);
        else if(type == Type.INK_TASK_LISTENER) inkTaskChannel?.AddListener(Print);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        eventChannel?.RemoveListener(Print);
        inkTaskChannel?.RemoveListener(Print);
    }

    protected override void RegisterAll()
    {
        if(type == Type.NONE) base.RegisterAll();
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

    public void Print((string,string,Action<string>) inkTask)
    {
        if(inkTask.Item1!=taskName) return;
        Print(inkTask.Item2, () => inkTask.Item3(inkTask.Item1));
    }
}