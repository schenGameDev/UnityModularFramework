using TMPro;
using UnityEngine;

namespace ModularFramework
{
    /// <summary>
    /// A text message appear around at given world position
    /// </summary>
    public class TooltipText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textbox;
        private readonly Autowire<GameBuilder> _gameBuilder = new ();

        public void Show(string text, Vector3 worldPosition)
        {
            if (textbox == null) return;

            textbox.text = text;

            var cam = _gameBuilder.Get().MainCamera;
            if (cam == null)
            {
                cam = Camera.main;
            }
            if (cam == null) return;
            
            // Convert world position to screen position for UI placement.
            transform.position = cam.WorldToScreenPoint(worldPosition);
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            SingletonRegistry<TooltipText>.Replace(this);
        }

        private void OnDisable()
        {
            SingletonRegistry<TooltipText>.Unregister(this);
        }
    }
}