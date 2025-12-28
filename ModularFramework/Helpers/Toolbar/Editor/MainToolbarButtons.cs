using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModularFramework
{
    public class MainToolbarButtons
    {
        [MainToolbarElement("Timescale/Reset", defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement ResetTimeScaleButton() {
            var icon = EditorGUIUtility.IconContent("Refresh").image as Texture2D;
            var content = new MainToolbarContent(icon, "Reset");
            var button = new MainToolbarButton(content, () => {
                Time.timeScale = 1f;
                MainToolbar.Refresh("Timescale/Slider");
            });
        
            MainToolbarElementStyler.StyleElement<UnityEditor.Toolbars.EditorToolbarButton>("Timescale/Reset", element => {
                element.style.paddingLeft = 0f;
                element.style.paddingRight = 0f;
                element.style.marginLeft = 0f;
                element.style.marginRight = 0f;
                element.style.minWidth = 20f;
                element.style.maxWidth = 20f;
            
                var image = element.Q<Image>();
                if (image != null) {
                    image.style.width = 12f;
                    image.style.height = 12f;
                }
            });
        
            return button;
        }
    }
}
