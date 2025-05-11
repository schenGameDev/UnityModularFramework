using System;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using EditorAttributes;
using ModularFramework.Commons;
using ModularFramework.Utility;
using UnityEditor;
using UnityEngine;
using UnityTimer;

namespace ModularFramework {
    public class GameRunner : Singleton<GameRunner>
    {
        public static readonly List<GameSystem> SYSTEMS = new();

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

    #region Runtime
        protected override void Awake() {
            base.Awake();
            foreach(var module in modules) {
                if(module is IRegistrySO so) {
                    _registryDict.Add(module.GetType(), so);
                }
                module.OnAwake(flags,references);
                if(!module.CentrallyManaged && module.OperateEveryFrame) {
                    _framelyUpdatedModules.Add(module);
                }
            }

            foreach (var m in PersistentBehaviour.INSTANCES)
            {
                m.OnSceneLoad(GameBuilder.Instance.NextScene);
            }
        }

        private void Start()
        {
            if (modules == null) return;
            RegistryBuffer.InjectAll();
            foreach(var module in modules) {
                module.OnStart();
            }

            _timer = new FrameCountdownTimer(10);
            _timer.OnTimerStop += SceneReady;
            _timer.Start();
   
        }
        
        private Timer _timer;

        private void SceneReady()
        {
            _timer.Dispose();
            if (GameBuilder.Instance)
            {
                GameBuilder.Instance.SceneTransitionCompleteCallback?.Invoke();
                GameBuilder.Instance.SceneTransitionCompleteCallback = null;
            }
        }

        private void Update()
        {
            if (modules == null) return;
            UpdateModuleFrame();
            foreach(var module in _framelyUpdatedModules) {
                module.OnUpdate(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (modules == null) return;
            foreach(var module in modules) {
                module.OnDestroy();
            }
            foreach (var m in PersistentBehaviour.INSTANCES)
            {
                m.OnSceneDestroy(GameBuilder.Instance.NextScene);
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || modules == null) return;
            foreach(var module in modules) {
                module.OnGizmos();
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

    #region Module
        public Optional<T> GetModule<T>() where T : GameModule {
            GameModule module = modules.First(m => (m as T) != null);
            if(module != null) {
                return (T) module;
            }
            DebugUtil.Error("Module of type " + typeof(T) + " not found");
            return null;
        }
    #endregion
    
    #region System
        public static Optional<T> GetSystem<T>() where T : GameSystem {
            GameSystem sys = SYSTEMS.FirstOrDefault(m => m.GetType() == typeof(T));
            if(sys) {
                return (T) sys;
            }
            Debug.LogWarning("System of type " + typeof(T) + " not found");
            return null;
        }

        public static void InjectSystem<T>(T sys) where T : GameSystem
        {
            if (SYSTEMS.Any(s=> s.GetType() == sys.GetType()))
            {
                Debug.LogWarning("System of type " + sys.GetType().Name + " already exists");
                return;
            }
            if(sys is IRegistrySO so) 
            {
                STATIC_REGISTRY_DICT.Add(sys.GetType(), so);
            }
            
            sys.OnStart();
            SYSTEMS.Add(sys);
        }
    #endregion

    #region Registry
        private readonly Dictionary<Type,IRegistrySO> _registryDict = new();
        public static readonly Dictionary<Type,IRegistrySO> STATIC_REGISTRY_DICT = new();
        public bool Register(Type registryType, Transform marker) {
            if(_registryDict.TryGetValue(registryType, out var registry)) {
                registry.Register(marker);
                return true;
            }
            return false;
        }
        public bool Unregister(Type registryType, Transform marker) {
            if(_registryDict.TryGetValue(registryType, out var registry)) {
                registry.Unregister(marker);
                return true;
            }
            return false;
        }
        public Optional<T> GetRegistry<T>() where T : ScriptableObject,IRegistrySO {
            if(_registryDict.TryGetValue(typeof(T), out var registry)) {
                return (T) registry;
            }
            DebugUtil.Error("Registry of type " + typeof(T) + " not found");
            return null;
        }
        
        public static Optional<T> GetSystemRegistry<T>() where T : ScriptableObject,IRegistrySO
        {
            if(STATIC_REGISTRY_DICT.TryGetValue(typeof(T), out var registry)) {
                return (T) registry;
            }
            DebugUtil.Error("Registry of type " + typeof(T) + " not found");
            return null;
        }
        public static bool RegisterSystem(Type registryType, Transform marker) {
            if(STATIC_REGISTRY_DICT.TryGetValue(registryType, out var registry)) {
                registry.Register(marker);
                return true;
            }
            return false;
        }
        public static bool UnregisterSystem(Type registryType, Transform marker) {
            if(STATIC_REGISTRY_DICT.TryGetValue(registryType, out var registry)) {
                registry.Unregister(marker);
                return true;
            }
            return false;
        }

    #endregion

    #region Event Channel
        public void RegisterEventChannel(IEventChannel channel) {
            if(eventChannels.AddIfAbsent(channel as ScriptableObject, channel.Live)) {
                (channel as IResetable)?.Reset();
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

        class ExecQueueMember {
            public GameModule Module;
            public float DeltaTime;

            public ExecQueueMember(GameModule module, float deltaTime) {
                Module = module;
                DeltaTime = deltaTime;
            }
        }

        public void AddToExecQueue(GameModule module, float deltaTime) {
            if(!_execSignalQueue.Any(m=>m.Module == module)) {
                var member = new ExecQueueMember(module, deltaTime);
                if(_execSignalQueue.Count > 0 || _pendingToAddQueue.Count > 0) {
                    module.Timer.Pause();
                }
                _pendingToAddQueue.Add(member);
            }
        }

        void UpdateModuleFrame() {
            _execSignalQueue.ForEach(m => m.DeltaTime += Time.deltaTime);

            ExecQueueMember toExec = null;
            if(_execSignalQueue.TryDequeue(out toExec)) {
                toExec.Module.Timer.Resume();
            } else if(_pendingToAddQueue.NonEmpty()) {
                toExec = _pendingToAddQueue.RemoveAtAndReturn(0);
            } else {
                return;
            }

            toExec.Module.OnUpdate(toExec.DeltaTime);

            _pendingToAddQueue.ForEach(m=>_execSignalQueue.Enqueue(m));
            _pendingToAddQueue.Clear();
        }
    #endregion

    #region Editor
        [Button("Refresh Modules")]
        private void AddBootUpParameter() {
            HashSet<string> kw = new(), kw2 = new();
            foreach(var m in modules) {
                if(m==null) continue;

                if(m.Keywords != null) {
                    foreach (var k in m.Keywords) {
                        kw.Add(k);
                        flags.AddIfAbsent(k,"");
                    }
                }
                if(m.RefKeywords != null) {
                    foreach (var k in m.RefKeywords) {
                        kw2.Add(k);
                        references.AddIfAbsent(k,null);
                    }
                }
            }
            flags.RemoveWhere(k => !kw.Contains(k));

            references.RemoveWhere(k => !kw2.Contains(k));

            DebugUtil.DebugLog("Module parameters refreshed");
        }

    #endregion
    }
}