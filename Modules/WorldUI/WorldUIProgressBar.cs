using KBCore.Refs;
using ModularFramework;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace UnityModularFramework.Modules.WorldUI
{
    /// <summary>
    /// The prefab must be a Canvas and has an Image component. <br/>
    /// The Canvas should be set to World Space render mode. <br/>
    /// The Image should have its type set to "Filled" and fill method set to "Horizontal". <br/>
    /// This class controls the fill amount, color, and width of the health bar, as well as enabling and disabling the canvas.
    /// </summary>
    public class WorldUIProgressBar : MonoBehaviour, IPrefabPoolEntry
    {
        [SerializeField] private Color barColor = Color.green;
        [SerializeField] private float barWidth = 13.56f;
        [SerializeField,Child] private Image barImage;
        [SerializeField,Self] private Canvas canvas;

#if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
#endif
        
        public void CleanUp()
        {
            canvas.enabled = false;
            barImage.fillAmount = 0;
            SetColorAndWidth(barColor, barWidth);
        }

        public void Enable()
        {
            canvas.enabled = true;
        }
    
        public void SetColorAndWidth(Color color, float width)
        {
            barImage.color = color;
            barImage.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        public void Fill(float fillAmount)
        {
            barImage.fillAmount = fillAmount;
        }

        #region Pool

        public void RegisterPrefabToPool()
        {
            PrefabPool<WorldUIProgressBar>.Register(this, CreateProgressBarPool);
        }
        
        public void ClearPool()
        {
            PrefabPool<WorldUIProgressBar>.Clear();
        }

        private ObjectPool<WorldUIProgressBar> CreateProgressBarPool(WorldUIProgressBar prefab)
        {
            return new ObjectPool<WorldUIProgressBar>(
                createFunc: () => Instantiate(prefab, Vector3.zero, Quaternion.identity, SingletonRegistry<WorldUIModule>.Instance.parent),
                actionOnGet: bar => bar.Enable(),
                actionOnRelease: bar => bar.CleanUp(),
                actionOnDestroy: bar =>
                {
                    if (bar != null) Destroy(bar.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 50
            );
        }
        #endregion
    }
}