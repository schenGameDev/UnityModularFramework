using UnityEngine;

namespace ModularFramework
{
    [RequireComponent(typeof(Camera))]
    public class CanvasCameraSetter : MonoBehaviour
    {
        [SerializeField] private Canvas[] canvases;
        private GameBuilder _gameBuilder = new Autowire<GameBuilder>();
        
        private void Awake()
        {
            gameObject.SetActive(false);
            
            if(canvases == null) return;
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _gameBuilder?.MainCamera;
            }
            
        }
    }
}