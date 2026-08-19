using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
using UnityEngine.UI;
using UnityTimer;

namespace ModularFramework.Modules.Input
{
    [RequireComponent(typeof(Marker))]
    public class ButtonKeyMapper : MonoBehaviour, IMark
    {
        public UIKeyMapSystemSO.UIInputKey[] mappedKeys;

        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Image progressBar;
        [SerializeField, ShowField(nameof(progressBar))] private float holdTime;
        
        private CountdownTimer _holdTimer;

        private void Awake()
        {
            if (progressBar && holdTime > 0)
            {
                _holdTimer = new CountdownTimer(holdTime);
                _holdTimer.OnTick += () => progressBar.fillAmount = _holdTimer.Progress;
                _holdTimer.OnTimerStart += () => progressBar.fillAmount = 0f;
                
            }
        }

        private void OnDestroy()
        {
            _holdTimer?.Dispose();
        }

        public void Raise(bool pressFinished)
        {
            if (!gameObject.activeSelf || !button.interactable)
            {
                // inactive
                if (_holdTimer is { IsRunning: true })
                {
                    _holdTimer.Stop();
                }
                return;
            }
            if (pressFinished)
            {
                if (_holdTimer == null || _holdTimer.IsFinished)
                {
                    button.onClick.Invoke();
                }
                
                _holdTimer?.Stop();
                progressBar.fillAmount = 0f;
            }
            else
            {
                _holdTimer.Start();
            }
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
            if (alreadyRegisteredTypes.Contains(typeof(UIKeyMapSystemSO))) return new();
            SingletonRegistry<UIKeyMapSystemSO>.Instance?.Register(transform);
            return new() { typeof(UIKeyMapSystemSO) };
        }

        public void UnregisterSelf()
        {
            SingletonRegistry<UIKeyMapSystemSO>.Instance?.Unregister(transform);
        }

        #endregion
    }
}
