using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

namespace ModularFramework
{
    /// <summary>
    /// enable and hide Canvas on demand
    /// </summary>
    [CreateAssetMenu(fileName = "UISystem_SO", menuName = "Game Module/UI System")]
    public class UISystem : GameSystem
    {
        [SerializeField] StringBoolEventChannel canvasChannel;
        [RuntimeObject,SerializeField, Rename("Active Canvas")] 
        private List<string> activeCanvasNames = new();
        [RuntimeObject] private readonly List<CanvasMarker> _frontCanvas = new();
        [RuntimeObject] private readonly List<CanvasMarker> _alwaysVisibleCanvas = new();


        private void OnEnable()
        {
            canvasChannel?.AddListener(CanvasChange);
        }

        private void OnDisable()
        {
            canvasChannel?.RemoveListener(CanvasChange);
        }
        
        public void CanvasChange((string, bool) channelMsg)
        {
            string canvasName = channelMsg.Item1;
            bool turnOn = channelMsg.Item2;
            if(turnOn) ActivateCanvas(canvasName);
            else DeactivateCanvas(canvasName);
        }
        
        public bool IsCanvasActive(string canvasName) => activeCanvasNames.Contains(canvasName);
        
        public void ActivateCanvas(string canvasName)
        {
            if(activeCanvasNames.Contains(canvasName)) return;
            if (DictRegistry<string, CanvasMarker>.TryGetValue(canvasName, out CanvasMarker canvasMarker))
            {
                canvasMarker.Show();
                if (canvasMarker.alwaysVisible)
                {
                    _alwaysVisibleCanvas.Add(canvasMarker);
                }
                else
                {
                    if (_frontCanvas.NonEmpty()
                        && (!_frontCanvas[0].compatibleWithOtherCanvas || !canvasMarker.compatibleWithOtherCanvas))
                    {
                        _frontCanvas.ForEach(c =>
                        {
                            c.Hide();
                            activeCanvasNames.Remove(c.name);
                        });
                        _frontCanvas.Clear();
                    }
                    _frontCanvas.Add(canvasMarker);
                }

                activeCanvasNames.Add(canvasName);
            }
        }
        
        public void DeactivateCanvas(string canvasName)
        {
            if (!activeCanvasNames.Contains(canvasName)) return;
            activeCanvasNames.Remove(canvasName);


            _frontCanvas.RemoveWhere(c =>
            {
                if (c.name == canvasName)
                {
                    c.Hide();
                    return true;
                }

                return false;
            });

            _alwaysVisibleCanvas.RemoveWhere(c => 
            {
                if (c.name == canvasName)
                {
                    c.Hide();
                    return true;
                }
                return false;
            });
            
        }

        public void DeactivateAll()
        {
            _frontCanvas.ForEach(c =>c.Hide());
            _alwaysVisibleCanvas.ForEach(c => c.Hide());
            
            _frontCanvas.Clear();
            _alwaysVisibleCanvas.Clear();
        }
        
    }
}