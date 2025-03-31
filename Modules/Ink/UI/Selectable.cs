using System;
using ModularFramework;
using TMPro;
using UnityEngine;

public class Selectable : Marker, ILive
{

    public string choiceGroupName;
    public int index;
    [field: SerializeField] public bool Live { get; set; }
    public string text;
    [SerializeField] private EventChannel<int> choiceEventChannel;
    public bool hasSelected;

    private TextMeshProUGUI _tmp;

    public Selectable()
    {
        registryTypes = new[] { (typeof(InkUIIntegrationSO),1)};
    }
    
    private void Awake()
    {
        _tmp = GetComponentInChildren<TextMeshProUGUI>();
    }
    
    public virtual void Select() {
        if(Live) {
            choiceEventChannel?.Raise(index);
            hasSelected = true;
            //todo grey out selected option
        }
    }

    public virtual void Activate (string txt = null) {
        Live = true;
        if (txt != null)
        {
            text = txt;
            if(_tmp) _tmp.text = text;
        }
        
        gameObject.SetActive(true);
    }

    public virtual void Deactivate() {
        Live = false;
        gameObject.SetActive(false);
    }
    
    public virtual void Hover() {}
}
