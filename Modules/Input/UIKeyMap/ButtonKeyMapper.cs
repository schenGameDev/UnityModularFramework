using System;
using System.Collections.Generic;
using ModularFramework;
using ModularFramework.Utility;
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
    
    #region IRegistrySO
    public List<Type> RegisterSelf(HashSet<Type> alreadyRegisteredTypes)
    {
        if (alreadyRegisteredTypes.Contains(typeof(UIKeyMapSystemSO))) return new ();
        SingletonRegistry<UIKeyMapSystemSO>.Instance?.Register(transform);
        return  new () {typeof(UIKeyMapSystemSO)};
    }

    public void UnregisterSelf()
    {
        SingletonRegistry<UIKeyMapSystemSO>.Instance?.Unregister(transform);
    }
    #endregion
}
