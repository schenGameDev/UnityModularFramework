using System;
using System.Collections.Generic;
using KBCore.Refs;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;

[RequireComponent(typeof(Marker), typeof(CanvasGroup))]
public class SingletonSelectableGroup : Selectable, ISelectableGroup
{
    [field:SerializeField] public string ChoiceGroupName { get; private set; }
    [field:SerializeField] public bool EnableOnAwake { get; private set; }
    public Action<int> OnSelect { get; set; }
    
#if UNITY_EDITOR
    private void OnValidate() => this.ValidateRefs();
#endif

    protected override void Awake()
    {
        if (!tmp)
        {
            DebugUtil.Error("TMPro not found in children");
        }
    }

    public void Activate(InkChoice choiceInfo, bool showHiddenChoice)
    {
        if (choiceInfo.choices.Count > 1)
        {
            DebugUtil.Error("Multiple choices present in SingletonSelectableGroup");
            return;
        }

        var choice = choiceInfo.choices[0];
        string text = choice.text;
        string subtext = "";
#if UNITY_EDITOR
        subtext = $"({choice.subText})";
#endif

        if (!showHiddenChoice && choice.hide)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        Live = !choice.hide;
        SetUp(choice.hide? $"<color=\"grey\">{text}</color> <color=\"red\">{subtext}</color>" : text);
    }
    
    public void ResetState()
    {
        gameObject.SetActive(false);
    }
    
    public void Select(int index)
    {
        OnSelect?.Invoke(index);
    }


    protected override void ConfirmSelection(bool confirmed)
    {
        confirmation?.Deactivate();
        Hide(false);
        if (confirmed)
        {
            hasSelected = true;
            Select(index);
        }
    }
    
    [SerializeField,Self(Flag.Optional)] private CanvasGroup cg;
    protected override void Hide(bool hide)
    {
        cg.interactable = !hide;
        cg.alpha = hide? 0 : 1;
        cg.blocksRaycasts = !hide;
    }
    
    public void Activate(string text, Action onSelect)
    {
        SetUp(text);
        gameObject.SetActive(true);
        Live = true;
        OnSelect += _ => onSelect();
    }
    
    #region IRegistrySO
    public List<Type> RegisterSelf(HashSet<Type> alreadyRegisteredTypes)
    {
        if (alreadyRegisteredTypes.Contains(typeof(InkUIIntegrationSO))) return new ();
        SingletonRegistry<InkUIIntegrationSO>.Instance?.Register(transform);
        return new () {typeof(InkUIIntegrationSO)};
    }

    public void UnregisterSelf()
    {
        SingletonRegistry<InkUIIntegrationSO>.Instance?.Unregister(transform);
    }
    #endregion
}