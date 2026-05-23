using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModularFramework.Modules.Ink
{
    [RequireComponent(typeof(Marker))]
    public class LineCard : TextPrinter
    {
        [SerializeField] private float lineGap = 0.5f;
        private const float EXPAND_LINE_TIME = 0.3f;
        
        public string PrefabKey { get; set; } = "default";
        public RectTransform ParentQueue { get; set; }
        
        private float _targetHeight;
        private int _lineNumbers = 1;
        private int _lastLineNumbers = 0;
        private RectTransform _rect;


        public override void Print(string text, Action callback, params string[] parameters)
        {
            _rect = GetComponent<RectTransform>();
            InitializeTextbox(text);
            base.Print(text, callback, parameters);
            printStyleInstance.OnTextChanged += CheckTextHeight;
            printStyleInstance.OnPrintComplete += CheckTextHeight;
        }
        
        private void InitializeTextbox(string text)
        {
            // Check if text is made up entirely by "<br>" tags
            string trimmed = text.Trim();
            const string brTag = "<br>";
            if (trimmed.Length > 0 && trimmed.Length % brTag.Length == 0)
            {
                bool allBr = true;
                for (int i = 0; i < trimmed.Length; i += brTag.Length)
                {
                    if (string.Compare(trimmed, i, brTag, 0, brTag.Length, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        allBr = false;
                        break;
                    }
                }
                if (allBr)
                {
                    _lineNumbers = trimmed.Length / brTag.Length;
                    
                }
            }
            _targetHeight = textbox.fontSize * (1 + lineGap) * _lineNumbers;
            printStyleInstance.Paused = true;
        }
        
        public void CheckTextHeight()
        {
            int newLineNumbers = GetLineNumbers(textbox.text);
            if (newLineNumbers <= _lineNumbers) return;
            _lastLineNumbers = _lineNumbers;
            _lineNumbers = newLineNumbers;
            _targetHeight = textbox.fontSize * (1 + lineGap) * newLineNumbers;
        }
        

        private int GetLineNumbers(string text)
        {
            var size = textbox.GetPreferredValues(text);
            return Mathf.CeilToInt(size.x / ParentQueue.rect.width);
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
                    _timeSpan = (_lineNumbers - _lastLineNumbers) * EXPAND_LINE_TIME;
                }
                if (Done)
                {
                    _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _targetHeight);
                }
                else
                {
                    printStyleInstance.Paused = true;
                    _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 
                        Mathf.Lerp(_rect.rect.height, _targetHeight, _t / _timeSpan));
                    LayoutRebuilder.ForceRebuildLayoutImmediate(ParentQueue);
                    var expandDone = _rect.rect.height + 0.0001f >= _targetHeight;
                    if (expandDone)
                    {
                        printStyleInstance.Paused = false;
                        _t = 0;
                    }
                    else
                    {
                        _t += Time.deltaTime;
                        // Debug.Log(_t + " " + _timeSpan);
                    }
                }
                
               
            }
        }
    }
}