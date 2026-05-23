using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.UI;

namespace ModularFramework.Modules.Ink
{
    [RequireComponent(typeof(Marker))]
    public class ChoiceCard : MonoBehaviour, ISelectableGroup
    {
        [SerializeField] private float optionHeight = 35f;
        [SerializeField, Child, ReadOnly] private Selectable[] selectables;
        [ReadOnly] public bool hasSelected;
        private Dictionary<int, int> _choiceIndexMap = new();
        private const float EXPAND_CHOICE_TIME = 0.3f;
        
        public RectTransform ParentQueue { get; set; }
        public string ChoiceGroupName => "LineCard";
        public bool EnableOnAwake => true;
        
        private RectTransform _rect;
        private InkChoice _choiceInfo;
        private bool _showHiddenChoice;
        private float _targetHeight;
        private int _optionCount;
        
        public void Activate(InkChoice choiceInfo, bool showHiddenChoice)
        {
            _rect = GetComponent<RectTransform>();
            _choiceInfo = choiceInfo;
            _showHiddenChoice = showHiddenChoice;
            _optionCount = _choiceInfo.choices.Count(c => _showHiddenChoice || !c.hide);
            if (_optionCount > 1)
            {
                _targetHeight = _optionCount * optionHeight + (_optionCount - 1) * GetComponent<VerticalLayoutGroup>().spacing;
            }
        }
        
        private float _t = 0;
        private float _timeSpan = 0;
        public void ExpandHeight()
        {
            if (_rect.rect.height + 0.0001f  < _targetHeight)
            {
                if (_t == 0)
                {
                    // start
                    _timeSpan = _optionCount * EXPAND_CHOICE_TIME;
                }
                _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 
                    Mathf.Lerp(_rect.rect.height, _targetHeight, _t / _timeSpan));
                LayoutRebuilder.ForceRebuildLayoutImmediate(ParentQueue);
                var expandDone = _rect.rect.height + 0.0001f >= _targetHeight;
                if (expandDone)
                {
                    _t = 0;
                    DisplayOptions();
                }
                else
                {
                    _t += Time.deltaTime;
                    // Debug.Log(_t + " " + _timeSpan);
                }
            }
        }
        
        private void DisplayOptions()
        {
            int i = 0;
            int choiceIndex = -1;
            List<InkLine> choices = _choiceInfo.choices;
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

                if (!_showHiddenChoice && choice.hide)
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