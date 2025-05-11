using System.Collections.Generic;
using UnityEngine;

namespace ModularFramework
{
    public class CanvasController : MonoBehaviour
    {
        public void OpenClose(string canvasName)
        {
            GameRunner.GetSystem<UISystem>().Do(sys=>Control(canvasName, !sys.IsCanvasActive(canvasName)));
        }
        
        public void Close(string canvasName) => Control(canvasName, false);
        
        public void Open(string canvasName) => Control(canvasName, true);
        
        private void Control(string canvasName, bool visible)
        {
            GameRunner.GetSystem<UISystem>()
                .Do(sys =>
                {
                    if (visible) sys.ActivateCanvas(canvasName);
                    else sys.DeactivateCanvas(canvasName);
                })
                .OrElseThrow(new KeyNotFoundException("Canvas not found."));
        }
    }
}