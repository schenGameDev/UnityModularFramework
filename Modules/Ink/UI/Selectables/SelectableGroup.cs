using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using KBCore.Refs;
using ModularFramework;
using UnityEngine;

[RequireComponent(typeof(Marker), typeof(CanvasGroup))]
public class SelectableGroup : MonoBehaviour, ISelectableGroup
{
    [field: SerializeField] public string ChoiceGroupName { get; private set; }
    [field:SerializeField] public bool EnableOnAwake { get; private set; }

    [SerializeField,Child] private Selectable[] selectables;
    [ReadOnly] public bool hasSelected;
    protected int lastIndex;
    private Dictionary<int,int> _choiceIndexMap = new ();
    
#if UNITY_EDITOR
    private void OnValidate() => this.ValidateRefs();
#endif

    private void Awake()
    {
        ResetState();
    }

    public Action<int> OnSelect {get; set;}

    public void Select(int index)
    {
        if (hasSelected) return;
        hasSelected = true;
        OnSelect?.Invoke(_choiceIndexMap[index]);
    }

    public virtual void Activate(InkChoice choiceInfo, bool showHiddenChoice)
    {
        gameObject.SetActive(true);
        Hide(false);
        int i = 0;
        int choiceIndex = -1;
        List<InkLine> choices = choiceInfo.choices;
        HashSet<int> usedChoices = new HashSet<int>();
        foreach (var choice in choices)
        {
            string text = choice.text;
            string subtext = "";
            choiceIndex += 1;
#if UNITY_EDITOR
            subtext = $"({choice.subText})";
#endif
            if (choice.IsIndexOverriden) i = choice.index;

            var b = selectables[i];

            if (!showHiddenChoice && choice.hide)
            {
                i++;
                b.gameObject.SetActive(false);
                continue;
            }
            
            _choiceIndexMap[i] = choiceIndex;
            usedChoices.Add(i);
            lastIndex = i;
            i++;

            b.gameObject.SetActive(true);
            b.Live = !choice.hide;
            b.SetUp(choice.hide ? $"<color=\"grey\">{text}</color> <color=\"red\">{subtext}</color>" : text);

        }

        selectables.ForEachOrdered((j, s) =>
        {
            if (!usedChoices.Contains(j)) s.gameObject.SetActive(false);
        });
    }

    public virtual void ResetState()
    {
        selectables = selectables
            .Peek(s =>
            {
                s.hasSelected = false;
                s.Live = false;
                s.gameObject.SetActive(false);
            })
            .OrderBy(s => s.index)
            .ToArray();
        hasSelected = false;
        Hide(true);
    }

    [SerializeField,Self(Flag.Optional)]private CanvasGroup cg;

    public void Hide(bool hide)
    {
        cg.interactable = !hide;
        cg.alpha = hide ? 0 : 1;
        cg.blocksRaycasts = !hide;
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