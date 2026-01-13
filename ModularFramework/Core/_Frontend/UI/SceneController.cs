using UnityEngine;

namespace ModularFramework
{
    public class SceneController : MonoBehaviour
    {
        private Autowire<GameBuilder> _builder = new();
        
        public void GoToScene(string sceneName)
        {
            _builder.Get().LoadScene(sceneName);
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }   
}