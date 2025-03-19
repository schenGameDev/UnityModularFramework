using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ModularFramework;
using Cysharp.Threading.Tasks;
using System.Threading;

public class SceneLoader : Singleton<SceneLoader> {
    [SerializeField] private string _startingScene;
    private string _currentScene;
    public RawImage transitionImage;
    [SerializeField] float _fadeDuration = 1;

    void Start()
    {
        LoadStartScene();
    }

    public void LoadStartScene() => LoadScene(_startingScene);

    public void LoadScene(string sceneName) {
        if(sceneName == _currentScene || sceneName.IsEmpty()) return;

        if(_currentScene != null && _currentScene.NonEmpty()) {
            Scene s = SceneManager.GetSceneByName(_currentScene);
            if(s!=null && s.IsValid()) {
                transitionImage.texture = GetCameraScreenshot();
                SceneManager.UnloadSceneAsync(s);

                if(_cts!=null) {
                    _cts.Cancel();
                    _cts.Dispose();
                }
                _cts = new CancellationTokenSource();
                FadeOut(_cts.Token).Forget();
            }
        }
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        op.allowSceneActivation = true;
        op.completed += (_) => {
            Scene newScene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(newScene);
            _currentScene = sceneName;
        };

    }

    private Texture2D GetCameraScreenshot() {
        Camera cam = Camera.main;
        int w = cam.pixelWidth;
        int h = cam.pixelHeight;

        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 2;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D output = new Texture2D(w, h, TextureFormat.RGB24, false);
        output.ReadPixels(new Rect(0,0,w,h), 0,0, false);
        output.Apply();

        RenderTexture.active = null;
        cam.targetTexture = null;
        rt.DiscardContents();
        rt.Release();

        return output;
    }

    private CancellationTokenSource _cts;
    async UniTaskVoid FadeOut(CancellationToken token) {
        Color from = Color.white;
        Color to = new Color(1,1,1,0);
        float t = 0;
        while(t<=_fadeDuration) {
            transitionImage.color = Color.Lerp(from, to, t / _fadeDuration);
            t += Time.deltaTime;
            await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
        }
        transitionImage.color = to;
    }
}