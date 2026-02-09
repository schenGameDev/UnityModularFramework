using EditorAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace ModularFramework
{
    public class ColorPreset : MonoBehaviour
    {
        [SerializeField] private Color color;
#if UNITY_EDITOR
        [Button]
        public void ChangeColor()
        {
            FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).ForEach(i=>i.color = color);
        }
#endif
    }
}
