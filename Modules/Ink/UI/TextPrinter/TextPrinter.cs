using System;
using System.Collections.Generic;
using KBCore.Refs;
using ModularFramework.Modules.Sound;
using TMPro;
using UnityEngine;

namespace ModularFramework.Modules.Ink
{
    public class TextPrinter : TextPrinterBase
    {
        public static readonly Dictionary<string, TextPrinter> INSTANCES = new();

        [SerializeField] private bool staticPrinter;
        [SerializeField] public GameObject endIndicator;

        [SerializeField, Tooltip("sound effect for one key strike")] 
        protected string soundName;

        [SerializeField] private PrintStyleBase printStyle;

        protected PrintStyleBase printStyleInstance;
        protected Autowire<SoundManagerSO> SoundManager = new();

        [SerializeField, Child(Flag.IncludeInactive)]
        public TextMeshProUGUI textbox;

        private Action _callback;

        protected virtual void Awake()
        {
            printStyleInstance =
                printStyle ? Instantiate(printStyle) : ScriptableObject.CreateInstance<NoPrintStyle>();
            printStyleInstance.Printer = this;
            if (endIndicator) endIndicator.SetActive(false);

            if (staticPrinter)
            {
                INSTANCES.Add(printerName, this);
                gameObject.SetActive(false);
            }
        }

        public override void Skip()
        {
            if (Done)
            {
                _callback?.Invoke();
                return;
            }

            printStyleInstance.Skip();
        }

        public override void Clean() // click again to hide
        {
            if (hideWhenNotUsed) gameObject.SetActive(false);
            else if (!printStyleInstance.noClearText) textbox.text = "";
            ReturnEarly = false;
            Done = false;
            _callback = null;
        }

        public override void Print(string text, Action callback, params string[] parameters)
        {
            gameObject.SetActive(true);
            _callback = callback;
            printStyleInstance.ReturnEarly = ReturnEarly;
            printStyleInstance.Print(text);
            if (callback != null)
            {
                printStyleInstance.OnPrintComplete = callback;
            }
            if (!string.IsNullOrEmpty(soundName))
            {
                printStyleInstance.OnTextChanged = PlaySoundEffect;
            }
        }

        private void OnDestroy()
        {
            printStyleInstance.Destroy();
            INSTANCES.Remove(printerName);
        }
        
        private void PlaySoundEffect()
        {
            SoundManager.Get()?.PlaySound(soundName);
        }

#if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
#endif
    }
}