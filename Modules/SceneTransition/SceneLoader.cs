using System;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ModularFramework;
using System.Threading;

public class SceneLoader : Singleton<SceneLoader> {
    [SerializeField] private string startingScene;
    public string CurrentScene {get; private set;}
    public RawImage transitionImage;
    [SerializeField] private SceneTransitionSO defaultTransition;
    private CancellationTokenSource _cts;

    void Start()
    {
        LoadStartScene();
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private void LoadStartScene() => LoadScene(startingScene);

    public void LoadScene(string sceneName, SceneTransitionSO transitionProfile = null, Action<string> callback = null) {
        if(sceneName == CurrentScene || sceneName.IsEmpty()) return;

        if(CurrentScene != null && CurrentScene.NonEmpty()) {
            Scene s = SceneManager.GetSceneByName(CurrentScene);
            if(s.IsValid()) {
                transitionImage.texture = GetCameraScreenshot();
                SceneManager.UnloadSceneAsync(s);

                if(_cts!=null) {
                    _cts.Cancel();
                    _cts.Dispose();
                }
                _cts = new CancellationTokenSource();
                SceneTransitionSO transition = transitionProfile ?? defaultTransition;
                transition.Transition(_cts.Token, transitionImage); 
            }
        }
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        op.allowSceneActivation = true;
        op.completed += (_) => {
            Scene newScene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(newScene);
            CurrentScene = sceneName;
            callback?.Invoke("changeScene");
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
}