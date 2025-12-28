using UnityEngine.UIElements;

namespace ModularFramework
{
    public static class VisualElementExtension
    {
        static VisualElement FindElement(this VisualElement element, System.Func<VisualElement, bool> predicate) {
            if (predicate(element)) {
                return element;
            }
            return element.Query<VisualElement>().Where(predicate).First();
        }
        
        public static VisualElement FindElementByName(this VisualElement element, string name) {
            return element.FindElement(e => e.name == name);
        }
        
        public static VisualElement FindElementByTooltip(this VisualElement element, string tooltip) {
            return element.FindElement(e => e.tooltip == tooltip);
        }
    }
}