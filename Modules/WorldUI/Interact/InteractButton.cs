using KBCore.Refs;
using ModularFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UnityModularFramework.Modules.WorldUI
{
    public class InteractButton : MonoBehaviour, IResetable
    {
        [SerializeField, Self] private Button button;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            this.ValidateRefs();
        }
#endif
        
        public void ResetState()
        {
            button.interactable = false;
            button.onClick.RemoveAllListeners();
        }

        public void Show(UnityAction action)
        {
            gameObject.SetActive(true);
            button.onClick.AddListener(action);
            button.interactable = true;
                
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
            ResetState();
        }
    }
}