using System;
using System.Threading;
using EditorAttributes;
using ModularFramework.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ModularFramework
{
    /// <summary>
    /// can only have ONE in all scenes, usually in the always present UI scene <br/>
    /// load scenes async and initialize Systems
    /// </summary>
    public class GameBuilder : Singleton<GameBuilder>
    {
        [Header("Scene Manager")]
        
        [SerializeField] private string startingScene;
        public string CurrentScene {get; private set;}
        public RawImage transitionImage;
        [SerializeField] private SceneTransitionSO defaultTransition;

        [Header("Game Systems")]
        [SerializeField,HideLabel]
        private GameSystem[] systems;
        
        private CancellationTokenSource _cts;

        protected override void Awake()
        {
            base.Awake();
            if (systems == null) return;
            
            GameRunner.SYSTEMS.Clear();
            GameRunner.STATIC_REGISTRY_DICT.Clear();
            
            foreach (var sys in systems)
            {
                GameRunner.InjectSystem(sys);
            }
        }

        void Start()
        {
            LoadStartScene();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            
            foreach(var sys in systems) {
                sys.OnDestroy();
            }
        }

        #region Scene
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
        #endregion
        #region Language
        public void ChangeLanguage(Language language)
        {
            TranslationUtil.Load(language);
            TranslationUtil.SaveLanguagePref(language);
            LoadStartScene();
        }
        
        #endregion
    }
}