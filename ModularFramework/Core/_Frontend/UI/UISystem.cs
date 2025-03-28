using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

namespace ModularFramework
{
    public class UISystem : GameSystem,IRegistrySO
    {
        [RuntimeObject] private readonly Dictionary<string, Transform> _canvasDict = new(); 
        [RuntimeObject,SerializeField, Rename("Active Canvas")] private List<string> activeCanvasNames = new();
        [RuntimeObject] private Transform _frontCanvas;
        [RuntimeObject] private readonly List<Transform> _alwaysVisibleCanvas = new();
        
        public void ActivateCanvas(string canvasName)
        {
            if(activeCanvasNames.Contains(canvasName)) return;
            if (_canvasDict.TryGetValue(canvasName, out Transform canvas))
            {
                canvas.gameObject.SetActive(true);
                if (canvas.GetComponent<CanvasMarker>().alwaysVisible)
                {
                    _alwaysVisibleCanvas.Add(canvas);
                }
                else
                {
                    if (!_frontCanvas)
                    {
                        _frontCanvas.gameObject.SetActive(false);
                        activeCanvasNames.Remove(_frontCanvas.name);
                    }
                    _frontCanvas = canvas;
                }

                activeCanvasNames.Add(canvasName);
            }
        }
        
        public void DeactivateCanvas(string canvasName)
        {
            if (!activeCanvasNames.Contains(canvasName)) return;
            activeCanvasNames.Remove(canvasName);
            
            if(_frontCanvas.name == canvasName)
            {
                _frontCanvas?.gameObject.SetActive(false);
                _frontCanvas = null;
                return;
            }

            _alwaysVisibleCanvas.RemoveWhere(tf => 
            {
                if (tf.name == canvasName)
                {
                    tf.gameObject.SetActive(false);
                    return true;
                }
                return false;
            });
            
        }
        
        public void Register(Transform transform)
        {
            _canvasDict.Add(transform.name, transform);
        }

        public void Unregister(Transform transform)
        {
            _canvasDict.Remove(transform.name);
        }
    }
}