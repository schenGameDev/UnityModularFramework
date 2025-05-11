using UnityEngine;
using UnityTimer;

namespace ModularFramework
{
    public class WindowRatioManager : MonoBehaviour
    {
        [SerializeField] private Vector2Int targetRatio = new(16,9);

        private float _desiredRatio;
        private int _screenWidth,_screenHeight;
        
        private void Start()
        {
            _desiredRatio = targetRatio.x / (float)targetRatio.y;
            SetWindowRatio();
            
            Timer timer = new RepeatFrameCountdownTimer(60,1);
            timer.OnTick += CheckWindowRatio;
            timer.Start();
        }

        private void CheckWindowRatio()
        {
            if(Screen.width==_screenWidth && Screen.height==_screenHeight) return;
            
            SetWindowRatio();
        }

        private void SetWindowRatio()
        {
            
            var ratio = Screen.width / (float)Screen.height;
            if (ratio - _desiredRatio > 0.01f)
            {
                _screenHeight = Screen.height;
                _screenWidth = (int) (Screen.height * _desiredRatio);
            } else if (ratio - _desiredRatio < -0.01f)
            {
                _screenHeight =  (int) (Screen.width / _desiredRatio);
                _screenWidth = Screen.width;
            }
            else
            {
                _screenHeight = Screen.height;
                _screenWidth = Screen.width;
            }
            
            Screen.SetResolution(_screenWidth, _screenHeight, Screen.fullScreen);
        }
    }
}