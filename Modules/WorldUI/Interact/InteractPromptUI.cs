using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace UnityModularFramework.Modules.WorldUI
{
    public class InteractPromptUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI prompt;
        [SerializeField] private InteractButton confirmInteractButton;
        public bool isWorldUI;
        
        public void ShowPrompt(string text, UnityAction startInteract)
        {
            prompt.gameObject.SetActive(true);
            prompt.text = text;
            confirmInteractButton.Show(startInteract);
        }

        public void Hide()
        {
            prompt.gameObject.SetActive(false);
            prompt.text = "";
            confirmInteractButton.Hide();
        }
    }
}