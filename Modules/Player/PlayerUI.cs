using UnityEngine;
using UnityEngine.UI;

namespace UnityModularFramework.Modules.Player
{
    public class PlayerUI : MonoBehaviour
    {
        [Header("World UI")]
        [SerializeField] private GameObject worldUICanvas;
        [SerializeField] private Image healthBar;
        [SerializeField] private GameObject highlightMarker;
        
        [Header("HUD")]
        [SerializeField] private GameObject hudCanvas;
        
        public void UpdateHealthBar(float value)
        {
            healthBar.fillAmount = value;
        }
        
        public void ShowHighlightMarker(bool isHighlighted)
        {
            highlightMarker.SetActive(isHighlighted);
        }
    }
}