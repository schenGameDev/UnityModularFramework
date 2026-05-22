using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModularFramework.Modules.Ink
{
    [RequireComponent(typeof(Marker))]
    public class LineCard : TextPrinter
    {
        public string PrefabKey { get; set; } = "default";
        public RectTransform ParentQueue { get; set; }

        public override void Print(string text, Action callback, params string[] parameters)
        {
            base.Print(text, callback, parameters);
            printStyleInstance.OnTextChanged += CheckTextHeight;
        }
        
        private float _height = 0;

        private void CheckTextHeight()
        {
            var newHeight = textbox.GetPreferredValues(textbox.text).y;
            if (!Mathf.Approximately(newHeight, _height))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(ParentQueue);
            }
            _height = newHeight;
        }
    }
}