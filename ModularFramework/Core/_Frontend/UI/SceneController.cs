using ModularFramework;
using UnityEngine;

namespace ModularFramework
{
    public class SceneController : MonoBehaviour
    {
        public void GoToScene(string sceneName)
        {
            GameBuilder.Instance.LoadScene(sceneName);
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }   
}