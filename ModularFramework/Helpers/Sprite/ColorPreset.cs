using System.Linq;
using EditorAttributes;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ModularFramework
{
    public class ColorPreset : MonoBehaviour
    {
        [SerializeField] private Color color;
        [SerializeField] private Image[] ignores;
#if UNITY_EDITOR
        [Button]
        public void ChangeColor()
        {
            FindObjectsByType<Image>(FindObjectsInactive.Include)
                .Where(i => ignores == null || !ignores.Contains(i))
                .ForEach(i=>
                {
                    i.color = color;
                    EditorUtility.SetDirty(i);
                });
        }
#endif
    }
}
