using System;
using TMPro;
using UnityEngine;

public class ConfirmationGroup : MonoBehaviour
{
    public static ConfirmationGroup Instance;
    public const string DEFAULT_CONFIRMATION_GROUP = "confirmation";

    [field:SerializeField] public string ChoiceGroupName { get; private set; }
    [SerializeField] private TextMeshProUGUI confirmText;
    
    private Action<bool> _callback;

    private void Awake()
    {
        if (DEFAULT_CONFIRMATION_GROUP == ChoiceGroupName)
        {
            Instance = this;
        }
        confirmText = GetComponentInChildren<TextMeshProUGUI>();
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