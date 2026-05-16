using System;
using System.Collections.Generic;
using EditorAttributes;
using KBCore.Refs;
using UnityEngine;

namespace ModularFramework.Modules.Ink
{
    [RequireComponent(typeof(Marker))]
    public class ChoiceCard : MonoBehaviour, ISelectableGroup
    {
        [SerializeField, Child, ReadOnly] private Selectable[] selectables;
        [ReadOnly] public bool hasSelected;
        private Dictionary<int, int> _choiceIndexMap = new();

        public string ChoiceGroupName => "LineCard";
        public bool EnableOnAwake => true;
        public void Activate(InkChoice choiceInfo, bool showHiddenChoice)
        {
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

        public void Select(int index)
        {
            if (hasSelected) return;
            hasSelected = true;
            OnSelect?.Invoke(_choiceIndexMap[index]);
        }

        public Action<int> OnSelect { get; set; }
        
        public void ResetState()
        { 
        }
    }
}