using System;
using System.Threading;
using AYellowpaper.SerializedCollections;
using EditorAttributes;
using ModularFramework.Commons;
using ModularFramework.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityTimer;
using Timer = UnityTimer.Timer;

namespace ModularFramework
{
    /// <summary>
    /// can only have ONE in all scenes, usually in the always present UI scene <br/>
    /// load scenes async and initialize Systems
    /// </summary>
    public class GameBuilder : Singleton<GameBuilder>
    {
        public Action SceneTransitionCompleteCallback;
        [RuntimeObject] public static bool GameStartFromBuilder {get; private set;}
        
        [Header("Scene Manager")]
        
        [SerializeField] private string startingScene;
        public string CurrentScene {get; private set;}
        public string NextScene {get; private set;}
        
        public RawImage transitionImage;
        [SerializeField] private SceneTransitionSO defaultTransition;
        [SerializeField,SerializedDictionary("from-to scenes","transition")] 
        private SerializedDictionary<Vector<string>, SceneTransitionSO> customTransitions = new();

        [Header("Game Systems")]
        [SerializeField,HideLabel]
        private GameSystem[] systems;
        
        private CancellationTokenSource _cts;
        
        public Camera MainCamera { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            GameStartFromBuilder = true;
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
            MainCamera = Camera.main;
            LoadStartScene();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            
            foreach(var sys in systems) {
                sys.Destroy();
            }
        }

        #region Scene

        private Timer _sceneLoadingTimer;
        
        private void LoadStartScene() => LoadScene(startingScene);

        public void LoadScene(string sceneName, Action callback = null) {
            if(sceneName == CurrentScene || sceneName.IsEmpty()) return;
            NextScene = sceneName;
            bool currentSceneExists = !string.IsNullOrEmpty(CurrentScene);
            Scene currentScene = default(Scene);
            if (currentSceneExists)
            {
                currentScene = SceneManager.GetSceneByName(CurrentScene);
                currentSceneExists = currentScene.IsValid();
            }

            if (currentSceneExists)
            {
                transitionImage.gameObject.SetActive(true);
                transitionImage.texture = GetCameraScreenshot();
            }

            if(_sceneLoadingTimer==null) _sceneLoadingTimer = new FrameCountdownTimer(2);
            else
            {
                _sceneLoadingTimer.Reset();
                _sceneLoadingTimer.OnTimerStop = null;
            }
            
            var transitionProfile = currentSceneExists? 
                customTransitions[new Vector<string>(CurrentScene, sceneName)] ?? defaultTransition 
                : defaultTransition;
            _sceneLoadingTimer.OnTimerStop += () => UnloadLoadScene(currentSceneExists, currentScene,transitionProfile, callback);
            _sceneLoadingTimer.Start();

        }

        private void UnloadLoadScene(bool unloadScene, Scene currentScene, SceneTransitionSO transitionProfile, Action callback)
        {
            if(unloadScene) {
                SceneManager.UnloadSceneAsync(currentScene);

                if(_cts!=null) {
                    _cts.Cancel();
                    _cts.Dispose();
                }
                _cts = new CancellationTokenSource();
                transitionProfile.Transition(_cts.Token, transitionImage); 
            }
            AsyncOperation op = SceneManager.LoadSceneAsync(NextScene, LoadSceneMode.Additive);
            op.allowSceneActivation = true;
            op.completed += (_) => {
                Scene newScene = SceneManager.GetSceneByName(NextScene);
                SceneManager.SetActiveScene(newScene);
                CurrentScene = NextScene;
            };
            SceneTransitionCompleteCallback = callback;
        }

        private Texture2D GetCameraScreenshot()
        {
            Camera cam = MainCamera;
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