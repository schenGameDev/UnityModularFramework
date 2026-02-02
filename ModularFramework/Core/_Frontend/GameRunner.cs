using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using EditorAttributes;
using ModularFramework.Utility;
using UnityEditor;
using UnityEngine;
using UnityTimer;

namespace ModularFramework {
    public class GameRunner : MonoBehaviour
    {
        [Header("Game Modules")]
        [SerializeField,HideLabel,HelpBox("In boot-up order", MessageMode.None)]
        [OnValueChanged(nameof(AddBootUpParameter))] private GameModule[] modules;
        [SerializeField,SerializedDictionary("Name","Value")] private SerializedDictionary<string,string> flags = new();
        [SerializeField,SerializedDictionary("Name","Ref Object")] private SerializedDictionary<string,GameObject> references = new();

        [Header("Event System")]
        [SerializeField,SerializedDictionary("Channel","Live"),HideLabel,ReadOnly]
        private SerializedDictionary<ScriptableObject,bool> eventChannels = new();
        [Header("Runtime")]
        public bool IsPause = false;
        public Transform Player => references["PLAYER"].transform;

        readonly List<GameModule> _framelyUpdatedModules = new();
        private Autowire<GameBuilder> _builder = new();

    #region Runtime
        protected void Awake() {
            SingletonRegistry<GameRunner>.TryRegister(this);
            LoadSystemsForDev();
            foreach(var module in modules) {
                module.SceneAwake();
                if(!module.CentrallyManaged && module.OperateEveryFrame) {
                    _framelyUpdatedModules.Add(module);
                }
                InjectModule(module);
            }

            foreach (var sys in Registry<GameSystem>.All)
            {
                sys.SceneAwake();
            }

            foreach (var m in Registry<PersistentBehaviour>.All)
            {
                m.LoadScene(_builder.Get().NextScene);
            }
            
            Time.timeScale = 1;
        }

        private void Start()
        {
            RegistryBuffer.InjectAll();
            if (modules != null)
            {
                foreach (var module in modules)
                {
                    module.Start();
                }
            }

            _timer = new FrameCountdownTimer(10);
            _timer.OnTimerStop += SceneReady;
            _timer.Start();
   
        }
        
        private Timer _timer;

        private void SceneReady()
        {
            _timer.Dispose();
            GameBuilder.SceneTransitionCompleteCallback?.Invoke();
            GameBuilder.SceneTransitionCompleteCallback = null;
        }

        private void Update()
        {
            if (modules == null) return;
            UpdateModuleFrame();
            foreach(var module in _framelyUpdatedModules) {
                module.Tick(Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            LateUpdateModuleFrame();
            foreach(var module in _framelyUpdatedModules) {
                module.LateTick();
            }
        }

        private void OnDestroy()
        {
            DestroySystemsForDev();
            if (modules != null)
            {
                foreach (var module in modules)
                {
                    module.Destroy();
                    ClearSystem(module);
                }
            }

            foreach (var m in Registry<PersistentBehaviour>.All)
            {
                m.DestroyScene(_builder.Get().NextScene);
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || modules == null) return;
            foreach(var module in modules) {
                module.Draw();
            }
        }
        
        public void EndGame() {
            #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
            #elif UNITY_WEBPLAYER
            Application.OpenURL("http://google.com");
            #else
            Application.Quit();
            #endif
        }
    #endregion

    #region In-Scene Reference
    public string GetInSceneFlag(string keyword) => flags[keyword];
    
    public GameObject GetInSceneGameObject(string keyword) => references[keyword];
    
    #endregion
    
    #region System
        public static void InjectSystem<T>(T sys, bool cleanExisting = false) where T : GameSystem
        {
            if(cleanExisting) SingletonRegistry<T>.Replace(sys);
            else if (!SingletonRegistry<T>.TryRegister(sys))
            {
                Debug.LogWarning("System of type " + sys.GetType().Name + " already exists");
            } 
            
            sys.Start();
            Registry<GameSystem>.TryAdd(sys);
        }

        public static void InjectModule<T>(T module) where T : GameSystem // System can be used as a module too
        {
            SingletonRegistry<T>.Replace(module);
        }

        public static void ClearSystem<T>(T module) where T : GameSystem
        {
            SingletonRegistry<T>.Clear();
        }
    #endregion
    
    #region Event Channel
        public void RegisterEventChannel(IEventChannel channel) {
            if(eventChannels.AddIfAbsent(channel as ScriptableObject, channel.Live)) {
                (channel as IResetable)?.ResetState();
            }
        }

        public void UnregisterEventChannel(IEventChannel channel) {
            eventChannels.Remove(channel as ScriptableObject);
        }

        public void PauseEventChannel(IEventChannel channel) {
            channel.Live = false;
            if(Application.isEditor) eventChannels.TrySetValue(channel as ScriptableObject, false);
        }

        public void ResumeEventChannel(IEventChannel channel) {
            channel.Live = true;
            if(Application.isEditor) eventChannels.TrySetValue(channel as ScriptableObject, true);
        }

        public bool IsEventChannelRegistered(IEventChannel channel) => channel is ScriptableObject so && eventChannels.ContainsKey(so);
    #endregion

    #region Exec Management
        private readonly List<ExecQueueMember> _pendingToAddQueue = new();
        private readonly Queue<ExecQueueMember> _execSignalQueue = new();
        private readonly List<ExecQueueMember> _execQueue = new();

        class ExecQueueMember {
            public GameModule Module;
            public float DeltaTime;

            public ExecQueueMember(GameModule module, float deltaTime) {
                Module = module;
                DeltaTime = deltaTime;
            }
        }

        public void AddToExecQueue(GameModule module, float deltaTime) {
            if(_execSignalQueue.All(m => m.Module != module)) {
                var member = new ExecQueueMember(module, deltaTime);
                if(_execSignalQueue.Count > 0 || _pendingToAddQueue.Count > 0) {
                    module.Timer.Pause();
                }
                _pendingToAddQueue.Add(member);
            }
        }

        private void UpdateModuleFrame() {
            _execSignalQueue.ForEach(m => m.DeltaTime += Time.deltaTime);

            ExecQueueMember toExec = null;
            if(_execSignalQueue.TryDequeue(out toExec)) {
                toExec.Module.Timer.Resume();
            } else if(_pendingToAddQueue.NonEmpty()) {
                toExec = _pendingToAddQueue.RemoveAtAndReturn(0);
            } else {
                return;
            }

            toExec.Module.Tick(toExec.DeltaTime);
            _execQueue.Add(toExec);

            _pendingToAddQueue.ForEach(m=>_execSignalQueue.Enqueue(m));
            _pendingToAddQueue.Clear();
        }

        private void LateUpdateModuleFrame()
        {
            foreach (var toExec in _execQueue)
            {
                toExec.Module.LateTick();
            }
            _execQueue.Clear();
        }
    #endregion

    #region Editor
        [Button("Refresh Modules")]
        private void AddBootUpParameter() {
            HashSet<string> kw = new(), kw2 = new();
            references = new();
            foreach(var m in modules) {
                if(m==null) continue;
                
                foreach (var k in SceneFlag.GetAllSceneFlagKeywords(m)) {
                    kw.Add(k);
                    flags.AddIfAbsent(k,"");
                }
                    
                foreach (var k in SceneRef.GetAllSceneReferenceKeywords(m)) {
                    kw2.Add(k);
                    references.AddIfAbsent(k,null);
                }
            }

            AddDevSystemBootUpParameter(kw, kw2);
            flags.RemoveWhere(k => !kw.Contains(k));

            references.RemoveWhere(k => !kw2.Contains(k));

            DebugUtil.DebugLog("Module parameters refreshed");
        }

    #endregion

    #region Dev
    [Header("Game Systems (Dev Only)")]
    [SerializeField,HideLabel]
    private GameSystem[] systems;

    private void LoadSystemsForDev()
    {
        if(GameBuilder.GameStartFromBuilder) return;
        if (systems == null) return;
            
        Registry<GameSystem>.Clear();
            
        foreach (var sys in systems)
        {
            InjectSystem(sys,true);
        }
    }

    private void DestroySystemsForDev()
    {
        if(GameBuilder.GameStartFromBuilder) return;
        if (systems == null) return;
        
        Registry<GameSystem>.Clear();
        foreach(var sys in systems) {
            sys.Destroy();
        }
    }
    
    private void AddDevSystemBootUpParameter(HashSet<string> kw, HashSet<string> kw2)
    {
        if (systems == null) return;
        foreach(var sys in systems) {
            if(sys==null) continue;
                
            foreach (var k in SceneFlag.GetAllSceneFlagKeywords(sys)) {
                kw.Add(k);
                flags.AddIfAbsent(k,"");
            }
                    
            foreach (var k in SceneRef.GetAllSceneReferenceKeywords(sys)) {
                kw2.Add(k);
                references.AddIfAbsent(k,null);
            }
        }
    }
    #endregion
    }
}