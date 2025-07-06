using System;
using System.Collections.Generic;
using ModularFramework.Commons;
using ModularFramework.Utility;
using UnityEngine;

namespace ModularFramework {
    /// <summary>
    /// Marker is in-game object that auto-registers itself to GameRunner's resgistry at Start, and is monitored by Game Module
    /// </summary>
    public class Marker : MonoBehaviour
    {
        /// <summary>
        /// This mark will be bufferred and re-injected at each scene
        /// </summary>
        [SerializeField] private bool buffered;

        /// <summary>
        /// Array of type groups. If one type in the same group is found, no warning will be logged
        /// </summary>
        protected Type[][] RegistryTypes;
        private readonly List<Type> _registeredTypes = new();
        
        /// <summary>
        /// override don't call base.Awake()
        /// </summary>
        protected virtual void Awake() 
        {
            if (RegistryTypes != null) return;
            List<Type[]> typeList = new();
            foreach (var imark in GetComponents<IMark>())
            {
               typeList.AddRange(imark.RegistryTypes);
            }
            RegistryTypes = typeList.ToArray();
        }

        protected virtual void Start() {
            if(buffered) RegistryBuffer.Register(this);
            else RegisterAll();
        }
        
        protected virtual void OnDestroy()
        {
            if(buffered) RegistryBuffer.Unregister(this);
            else UnregisterAll();
        }

        public Optional<T> GetRegistry<T>() where T : ScriptableObject,IRegistrySO 
            => typeof(T).Inherits(typeof(GameSystem))? GameRunner.GetSystemRegistry<T>() : GameRunner.Instance.GetRegistry<T>();

        public virtual void RegisterAll()
        {
            _registeredTypes.Clear();
            foreach (var types in RegistryTypes)
            {
                bool found = false;
                List<string> unfoundTypes = new();
                foreach (var t in types)
                {
                    if (_registeredTypes.Contains(t))
                    {
                        found = true;
                        break;
                    }
                    
                    found =t.Inherits(typeof(GameSystem))? GameRunner.RegisterSystem(t, transform) : GameRunner.Instance.Register(t, transform);
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
                bool found = t.IsSubclassOf(typeof(GameSystem))? GameRunner.UnregisterSystem(t, transform) : GameRunner.Instance.Unregister(t, transform);
                if(!found) DebugUtil.Warn("Registry of type " + t + " not found");
            }

            _registeredTypes.Clear();
        }
    }
}