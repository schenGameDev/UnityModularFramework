using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModularFramework {
    using Commons;
    using ModularFramework.Utility;
    /// <summary>
    /// Marker is in-game object that auto-registers itself to GameRunner's resgistry at Start, and is monitored by Game Module
    /// </summary>
    public abstract class Marker : MonoBehaviour {
        /// <summary>
        /// (type : group_id). If one type in the same group is found, no warning will be logged
        /// </summary>
        protected (Type,int)[] registryTypes;
        private bool started = false;
        private List<Type> _registeredTypes = new();

        protected virtual void Start() {
            started = true;
            OnEnable();
        }

        protected virtual void OnEnable() {
            if(started) RegisterAll();
        }

        protected virtual void OnDisable() {
            UnregisterAll();
        }

        protected virtual void OnDestroy() {
            UnregisterAll();
        }

        protected Optional<T> GetRegistry<T>() where T : ScriptableObject,IRegistrySO 
            => typeof(T) == typeof(GameSystem)? GameRunner.GetSystemRegistry<T>() : GameRunner.Instance.GetRegistry<T>();

        protected virtual void RegisterAll()
        {
            HashSet<int> foundGroups = new();
            List<int> unfoundTypeIndex = new();
            int count = 0;
            foreach (var t in registryTypes) {
                bool found = t.Item1 == typeof(GameSystem)? GameRunner.RegisterSystem(t.Item1, transform) : GameRunner.Instance.Register(t.Item1, transform);
                if (found) {
                    foundGroups.Add(t.Item2);
                    _registeredTypes.Add(t.Item1);
                } else {
                    unfoundTypeIndex.Add(count);
                }
                count += 1;
            }
            unfoundTypeIndex.Select(i => registryTypes[i])
                            .Where(t => !foundGroups.Contains(t.Item2))
                            .ForEach(t => DebugUtil.Warn("Registry of type " + t.Item1 + " not found"));
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