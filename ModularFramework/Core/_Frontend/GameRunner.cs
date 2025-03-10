using System;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using EditorAttributes;
using UnityEngine;

namespace ModularFramework {
    using Commons;
    public class GameRunner : Singleton<GameRunner>
    {
        [Header("Game Modules")]
        [SerializeField,HideLabel,HelpBox("In boot-up order", MessageMode.None)]
        [OnValueChanged(nameof(AddBootupParameter))] private GameModule[] _modules;
        [SerializeField,SerializedDictionary("Name","Value")] private SerializedDictionary<string,string> _flags = new();
        [SerializeField,SerializedDictionary("Name","Ref Object")] private SerializedDictionary<string,GameObject> _references = new();
        [Header("Event System")]
        [SerializeField,SerializedDictionary("Channel","Live"),HideLabel,ReadOnly]
        private SerializedDictionary<ScriptableObject,bool> _eventChannels = new();
        [Header("Runtime")]
        public bool IsPause = true;
        public Transform Player => _references["PLAYER"].transform;

        List<GameModule> _framelyUpdatedModules = new();

    #region Runtime
        protected override void Awake() {
            base.Awake();
            _registrySODict = new();
            foreach(var module in _modules) {
                if(module is IRegistrySO) {
                    _registrySODict.Add(module.GetType(), module as IRegistrySO);
                }
                module.OnAwake(_flags,_references);
                if(!module.CentrallyManaged && module.OperateEveryFrame) {
                    _framelyUpdatedModules.Add(module);
                }
            }
        }

        private void Start()
        {
            if(_modules!=null) {
                foreach(var module in _modules) {
                    module.OnStart();
                }
            }
        }

        private void Update() {
            if(_modules!=null) {
                UpdateModuleFrame();
                foreach(var module in _framelyUpdatedModules) {
                    module.OnUpdate(Time.deltaTime);
                }
            }
        }

        private void OnDestroy() {
            if(_modules!=null) {
                foreach(var module in _modules) {
                    module.OnDestroy();
                }
            }
        }

        private void OnDrawGizmos() {
            if(Application.isPlaying && _modules!=null) {
                foreach(var module in _modules) {
                    module.OnGizmos();
                }
            }
        }
    #endregion

    #region Module
        public Optional<T> GetModule<T>() where T : GameModule {
            GameModule module = _modules.Where(m=>(m as T) != null).First();
            if(module != null) {
                return (T) module;
            }
            Debug.LogError("Module of type " + typeof(T).ToString() + " not found");
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
            Debug.LogError("Registry of type " + registryType.ToString() + " not found");
            return null;
        }

        public Optional<T> GetRegistry<T>() where T : ScriptableObject,IRegistrySO {
            if(_registrySODict.TryGetValue(typeof(T), out var registry)) {
                return (T) registry;
            }
            Debug.LogError("Registry of type " + typeof(T).ToString() + " not found");
            return null;
        }

    #endregion

    #region Event Channel
        public void RegisterEventChannel(IEventChannel channel) {
            if(_eventChannels.AddIfAbsent(channel as ScriptableObject, channel.Live)) {
                (channel as IResetable)?.Reset();
            }
        }

        public void UnregisterEventChannel(IEventChannel channel) {
            _eventChannels.Remove(channel as ScriptableObject);
        }

        public void PauseEventChannel(IEventChannel channel) {
            channel.Live = false;
            if(Application.isEditor) _eventChannels.TrySetValue(channel as ScriptableObject, false);
        }

        public void ResumeEventChannel(IEventChannel channel) {
            channel.Live = true;
            if(Application.isEditor) _eventChannels.TrySetValue(channel as ScriptableObject, true);
        }

        public bool IsEventChannelRegistered(IEventChannel channel) => _eventChannels.ContainsKey(channel as ScriptableObject);
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
            foreach(var m in _modules) {
                if(m==null) continue;

                if(m.Keywords != null) {
                    foreach (var k in m.Keywords) {
                        kw.Add(k);
                        _flags.AddIfAbsent(k,"");
                    }
                }
                if(m.RefKeywords != null) {
                    foreach (var k in m.RefKeywords) {
                        kw2.Add(k);
                        _references.AddIfAbsent(k,null);
                    }
                }
            }
            _flags.RemoveWhere(k => !kw.Contains(k));

            _references.RemoveWhere(k => !kw2.Contains(k));

            Debug.Log("Module parameters refreshed");
        }

    #endregion
    }
}