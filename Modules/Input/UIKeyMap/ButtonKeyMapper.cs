using System;
using ModularFramework;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Marker))]
public class ButtonKeyMapper : MonoBehaviour,IMark
{
    public UIKeyMapSystemSO.UIInputKey[] mappedKeys;
    
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    
    public void Raise()
    {
        button.onClick.Invoke();
    }

    public void SetIcon(Sprite keyIcon)
    {
        if (keyIcon == null)
        {
            icon.gameObject.SetActive(false);
            return;
        }
        icon.sprite = keyIcon;
        icon.gameObject.SetActive(true);
    }

    public Type[][] RegistryTypes => new []{new []{typeof(UIKeyMapSystemSO)}};
}
