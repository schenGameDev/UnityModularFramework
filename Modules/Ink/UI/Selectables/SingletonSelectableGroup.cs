using System;
using ModularFramework;
using ModularFramework.Utility;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Marker), typeof(CanvasGroup))]
public class SingletonSelectableGroup : Selectable, ISelectableGroup
{
    [field:SerializeField] public string ChoiceGroupName { get; private set; }
    [field:SerializeField] public bool EnableOnAwake { get; private set; }
    public Action<int> OnSelect { get; set; }

    protected override void Awake()
    {
        TMP = GetComponentInChildren<TextMeshProUGUI>(true);
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
    
    public Type[][] RegistryTypes => new[] { new[] {typeof(InkUIIntegrationSO)}};
    public void Reset()
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
    
    private CanvasGroup _cg;
    protected override void Hide(bool hide)
    {
        if(!_cg) _cg = GetComponent<CanvasGroup>();
        _cg.interactable = !hide;
        _cg.alpha = hide? 0 : 1;
        _cg.blocksRaycasts = !hide;
    }
    
    public void Activate(string text, Action onSelect)
    {
        SetUp(text);
        gameObject.SetActive(true);
        Live = true;
        OnSelect += _ => onSelect();
    }
    
}