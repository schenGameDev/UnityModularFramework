using UnityEngine;

namespace ModularFramework
{
    [RequireComponent(typeof(Camera))]
    public class CanvasCameraSetter : MonoBehaviour
    {
        [SerializeField] private Canvas[] canvases;
        private Autowire<GameBuilder> _gameBuilder = new();
        
        private void Awake()
        {
            gameObject.SetActive(false);
            
            if(canvases == null) return;
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _gameBuilder.Get()?.MainCamera;
            }
            
        }
    }
}