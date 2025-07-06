using System.Collections.Generic;
using UnityEngine;

namespace ModularFramework
{
    public class CanvasController : MonoBehaviour
    {
        private Autowire<UISystem> _uiSystem = new();
        
        public void OpenClose(string canvasName)
        {
            Control(canvasName, !_uiSystem.Get().IsCanvasActive(canvasName));
        }
        
        public void Close(string canvasName) => Control(canvasName, false);
        
        public void Open(string canvasName) => Control(canvasName, true);
        
        private void Control(string canvasName, bool visible)
        {
            if (visible) _uiSystem.Get().ActivateCanvas(canvasName);
            else _uiSystem.Get().DeactivateCanvas(canvasName);
        }
    }
}