using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModularFramework {
    using Commons;
    using Utility;
    /// <summary>
    /// Marker is in-game object that auto-registers itself to GameRunner's resgistry at Start, and is monitored by Game Module
    /// </summary>
    public class Marker : MonoBehaviour {
        /// <summary>
        /// Array of type groups. If one type in the same group is found, no warning will be logged
        /// </summary>
        protected Type[][] RegistryTypes;
        private bool _started = false;
        private readonly List<Type> _registeredTypes = new();
        
        /// <summary>
        /// override don't call base.Awake()
        /// </summary>
        protected virtual void Awake() 
        {
            if (RegistryTypes != null) return;
            foreach (var imark in GetComponents<IMark>())
            {
                RegistryTypes.AddRange(imark.RegistryTypes);
            }
        }

        protected virtual void Start() {
            _started = true;
            OnEnable();
        }

        protected virtual void OnEnable() {
            if(_started) RegisterAll();
        }

        protected virtual void OnDisable() {
            UnregisterAll();
        }

        public Optional<T> GetRegistry<T>() where T : ScriptableObject,IRegistrySO 
            => typeof(T) == typeof(GameSystem)? GameRunner.GetSystemRegistry<T>() : GameRunner.Instance.GetRegistry<T>();

        protected virtual void RegisterAll()
        {
            foreach (var types in RegistryTypes)
            {
                bool found = false;
                List<string> unfoundTypes = new();
                foreach (var t in types)
                {
                    found = t == typeof(GameSystem)? GameRunner.RegisterSystem(t, transform) : GameRunner.Instance.Register(t, transform);
                    if (found)
                    {
                        _registeredTypes.Add(t);
                        break;
                    }
                    unfoundTypes.Add(t.ToString());
                }

                if (!found) DebugUtil.Warn("Registry of type " + string.Join("/", unfoundTypes) + " not found");
            }
            
        }

        private void UnregisterAll() {
            foreach (Type t in _registeredTypes) {
                bool found = t == typeof(GameSystem)? GameRunner.UnregisterSystem(t, transform) : GameRunner.Instance.Unregister(t, transform);
                if(!found) DebugUtil.Warn("Registry of type " + t + " not found");
            }

            _registeredTypes.Clear();
        }
    }
}