using System;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using EditorAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace ModularFramework {
    using Commons;
    using Utility;

    public class GameRunner : Singleton<GameRunner>
    {
        static readonly List<GameSystem> Systems = new();
        /// <summary>
        /// each system only needs to register in the first scene it is used
        /// </summary>
        [Header("Game Systems")]
        [SerializeField,HideLabel]
        private GameSystem[] systems;

        [Header("Game Modules")]
        [SerializeField,HideLabel,HelpBox("In boot-up order", MessageMode.None)]
        [OnValueChanged(nameof(AddBootupParameter))] private GameModule[] modules;
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

            if (systems != null)
            {
                foreach (var sys in systems)
                {
                    if (Systems.Contains(sys)) continue;
                    sys.OnStart();
                    Systems.Add(sys);
                }
            }
            _registrySODict = new();
            foreach(var module in modules) {
                if(module is IRegistrySO) {
                    _registrySODict.Add(module.GetType(), module as IRegistrySO);
                }
                module.OnAwake(flags,references);
                if(!module.CentrallyManaged && module.OperateEveryFrame) {
                    _framelyUpdatedModules.Add(module);
                }
            }
        }

        private void Start()
        {
            if(modules!=null) {
                foreach(var module in modules) {
                    module.OnStart();
                }
            }
        }

        private void Update() {
            if(modules!=null) {
                UpdateModuleFrame();
                foreach(var module in _framelyUpdatedModules) {
                    module.OnUpdate(Time.deltaTime);
                }
            }
        }

        private void OnDestroy() {
            if(modules!=null) {
                foreach(var module in modules) {
                    module.OnDestroy();
                }
            }
        }

        private void OnDrawGizmos() {
            if(Application.isPlaying && modules!=null) {
                foreach(var module in modules) {
                    module.OnGizmos();
                }
            }
        }

        [Button]
        public void EndGame() {
            OnDestroy();
            foreach(var sys in Systems) {
                sys.OnDestroy();
            }
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #elif UNITY_WEBPLAYER
            Application.OpenURL("http://google.com");
            #else
            Application.Quit();
            #endif
        }
    #endregion

    #region Module
        public Optional<T> GetModule<T>() where T : GameModule {
            GameModule module = modules.Where(m=>(m as T) != null).First();
            if(module != null) {
                return (T) module;
            }
            DebugUtil.Error("Module of type " + typeof(T).ToString() + " not found");
            return null;
        }

    #endregion

    #region Registry
        private Dictionary<Type,IRegistrySO> _registrySODict;
        public bool Register(Type registryType, Transform marker) {
            if(_registrySODict.TryGetValue(registryType, out var registry)) {
                registry.Register(marker);
                return true;
            }
            return false;
        }
        public bool Unregister(Type registryType, Transform marker) {
            if(_registrySODict.TryGetValue(registryType, out var registry)) {
                registry.Unregister(marker);
                return true;
            }
            return false;
        }
        public IRegistrySO GetRegistry(Type registryType) {
            if(_registrySODict.TryGetValue(registryType, out var registry)) {
                return registry;
            }
            DebugUtil.Error("Registry of type " + registryType.ToString() + " not found");
            return null;
        }

        public Optional<T> GetRegistry<T>() where T : ScriptableObject,IRegistrySO {
            if(_registrySODict.TryGetValue(typeof(T), out var registry)) {
                return (T) registry;
            }
            DebugUtil.Error("Registry of type " + typeof(T).ToString() + " not found");
            return null;
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

        public bool IsEventChannelRegistered(IEventChannel channel) => eventChannels.ContainsKey(channel as ScriptableObject);
    #endregion

    #region Exec Management
        List<ExecQueueMember> _pendingToAddQueue = new();
        Queue<ExecQueueMember> _execSignalQueue = new();

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
        void AddBootupParameter() {
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