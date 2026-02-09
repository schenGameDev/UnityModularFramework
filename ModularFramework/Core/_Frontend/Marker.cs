using System;
using System.Collections.Generic;
using KBCore.Refs;
using UnityEngine;

namespace ModularFramework {
    /// <summary>
    /// Marker is in-game object that auto-registers itself to GameRunner's registry at Start, and is monitored by Game Module
    /// </summary>
    [DisallowMultipleComponent]
    public class Marker : MonoBehaviour
    {
        /// <summary>
        /// This mark will be bufferred and re-injected at each scene
        /// </summary>
        [SerializeField] private bool buffered;
        
        [SerializeField,Self(Flag.Optional)] private InterfaceRef<IMark>[] marks;
        
#if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
#endif

        protected virtual void Start() {
            if(buffered) RegistryBuffer.Register(this);
            else RegisterAll();
        }
        
        protected virtual void OnDestroy()
        {
            if(buffered) RegistryBuffer.Unregister(this);
            else UnregisterAll();
        }
        
        public virtual void RegisterAll()
        {
            HashSet<Type> registeredTypes = new();
            foreach (var imarkRef in marks)
            {
                registeredTypes.AddRange(imarkRef.Value.RegisterSelf(registeredTypes));
            }
        }
        
        protected virtual void UnregisterAll() {

            foreach (var imarkRef in marks)
            {
                imarkRef.Value.UnregisterSelf();
            }
        }
    }
    
}