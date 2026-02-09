using System;
using KBCore.Refs;
using TMPro;
using UnityEngine;

public class ConfirmationGroup : MonoBehaviour
{
    public static ConfirmationGroup Instance;
    public const string DEFAULT_CONFIRMATION_GROUP = "confirmation";

    [field:SerializeField] public string ChoiceGroupName { get; private set; }
    [SerializeField,Child] private TextMeshProUGUI confirmText;
    
    private Action<bool> _callback;

#if UNITY_EDITOR
    private void OnValidate() => this.ValidateRefs();
#endif
    
    private void Awake()
    {
        if (DEFAULT_CONFIRMATION_GROUP == ChoiceGroupName)
        {
            Instance = this;
        }
        Deactivate();
    }

    public void Activate(string text, Action<bool> callback)
    {
        gameObject.SetActive(true);
        confirmText.text = text;
        _callback = callback;
    }

    public void Deactivate()
    {
        _callback = null;
        gameObject.SetActive(false);
    }

    public void Select(bool yes)
    {
        _callback.Invoke(yes);
    }
    
    
}