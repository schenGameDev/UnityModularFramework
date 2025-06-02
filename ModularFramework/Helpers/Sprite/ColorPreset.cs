using EditorAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace ModularFramework
{
    public class ColorPreset : MonoBehaviour
    {
        [SerializeField] private Color color;

        [Button]
        public void ChangeColor()
        {
            FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).ForEach(i=>i.color = color);
        }
    }
}
