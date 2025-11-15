using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using ModularFramework;
using UnityEngine;

[RequireComponent(typeof(Marker), typeof(CanvasGroup))]
public class SelectableGroup : MonoBehaviour, ISelectableGroup
{
    [field: SerializeField] public string ChoiceGroupName { get; private set; }
    [field:SerializeField] public bool EnableOnAwake { get; private set; }

    public Type[][] RegistryTypes => new[] { new[] { typeof(InkUIIntegrationSO) } };

    private List<Selectable> _selectables;
    [ReadOnly] public bool hasSelected;
    protected int lastIndex;
    private Dictionary<int,int> _choiceIndexMap = new ();

    private void Awake()
    {
        _selectables = GetComponentsInChildren<Selectable>().OrderBy(s => s.index).ToList();
        Reset();
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

            var b = _selectables[i];

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

        _selectables.ForEachOrdered((j, s) =>
        {
            if (!usedChoices.Contains(j)) s.gameObject.SetActive(false);
        });
    }

    public virtual void Reset()
    {
        _selectables.ForEach(s =>
        {
            s.hasSelected = false;
            s.Live = false;
            s.gameObject.SetActive(false);
        });
        hasSelected = false;
        Hide(true);
    }

    private CanvasGroup _cg;

    public void Hide(bool hide)
    {
        if (!_cg) _cg = GetComponent<CanvasGroup>();
        _cg.interactable = !hide;
        _cg.alpha = hide ? 0 : 1;
        _cg.blocksRaycasts = !hide;
    }
}